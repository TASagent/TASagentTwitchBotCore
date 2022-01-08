using System.Text.Json;
using System.Text.Json.Serialization;

namespace TASagentTwitchBot.Core.Credit;

public class SimpleCreditManager : ICreditManager
{
    private readonly CreditSystemData creditSystemData;

    public SimpleCreditManager()
    {
        creditSystemData = CreditSystemData.GetCreditData();
    }

    public bool IsEnabled => true;

    public Task<bool> HasCredits(Database.User user, string creditType) => Task.FromResult(creditSystemData.HasCredits(user.TwitchUserId, creditType));
    public Task<long> GetCredits(Database.User user, string creditType) => Task.FromResult(creditSystemData.GetCredits(user.TwitchUserId, creditType));

    public Task AdjustCredits(Database.User user, string creditType, long delta)
    {
        creditSystemData.AdjustCredits(user.TwitchUserId, creditType, delta);
        creditSystemData.Serialize();
        return Task.CompletedTask;
    }

    public Task SetCredits(Database.User user, string creditType, long value)
    {
        creditSystemData.SetCredits(user.TwitchUserId, creditType, value);
        creditSystemData.Serialize();
        return Task.CompletedTask;
    }

    public Task<bool> TryDebit(Database.User user, string creditType, long cost)
    {
        if (creditSystemData.TryDebit(user.TwitchUserId, creditType, cost))
        {
            creditSystemData.Serialize();
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<IEnumerable<(string creditType, long value)>> GetAllCredits(Database.User user) =>
        Task.FromResult(creditSystemData.GetAllCredits(user.TwitchUserId));

    private class CreditSystemData
    {
        public Dictionary<string, CreditUser> Users { get; init; } = new Dictionary<string, CreditUser>();

        private static string CreditFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "Credits.json");

        private static readonly object _lock = new object();

        public bool HasCredits(string userId, string creditType) => GetUser(userId).HasCredits(creditType);
        public long GetCredits(string userId, string creditType) => GetUser(userId).GetCredits(creditType);
        public void AdjustCredits(string userId, string creditType, long delta) => GetUser(userId).AdjustCredits(creditType, delta);
        public void SetCredits(string userId, string creditType, long value) => GetUser(userId).SetCredits(creditType, value);
        public bool TryDebit(string userId, string creditType, long cost) => GetUser(userId).TryDebit(creditType, cost);

        public IEnumerable<(string creditType, long value)> GetAllCredits(string userId) => GetUser(userId).GetAllCredits();

        private CreditUser GetUser(string userId)
        {
            if (!Users.TryGetValue(userId, out CreditUser? user))
            {
                user = new CreditUser();
                Users.Add(userId, user);
            }

            return user;
        }

        public static CreditSystemData GetCreditData()
        {
            CreditSystemData creditData;
            if (File.Exists(CreditFilePath))
            {
                //Load existing config
                creditData = JsonSerializer.Deserialize<CreditSystemData>(File.ReadAllText(CreditFilePath))!;
            }
            else
            {
                creditData = new CreditSystemData();
                creditData.Serialize();
            }

            return creditData;
        }

        public void Serialize()
        {
            lock (_lock)
            {
                File.WriteAllText(CreditFilePath, JsonSerializer.Serialize(this));
            }
        }
    }

    private class CreditUser
    {
        public Dictionary<string, long> Credits { get; init; } = new Dictionary<string, long>();

        public bool HasCredits(string creditType) => Credits.GetValueOrDefault(creditType, 0) > 0;
        public long GetCredits(string creditType) => Credits.GetValueOrDefault(creditType, 0);
        public void AdjustCredits(string creditType, long delta) => Credits[creditType] = Credits.GetValueOrDefault(creditType, 0) + delta;
        public void SetCredits(string creditType, long value) => Credits[creditType] = value;
        public bool TryDebit(string creditType, long cost)
        {
            if (GetCredits(creditType) >= cost)
            {
                AdjustCredits(creditType, -cost);
                return true;
            }

            return false;
        }

        public IEnumerable<(string creditType, long value)> GetAllCredits()
        {
            foreach (KeyValuePair<string, long> kvpCredit in Credits.OrderBy(x=>x.Key).Where(x=>x.Value != 0))
            {
                yield return (kvpCredit.Key, kvpCredit.Value);
            }
        }
    }
}
