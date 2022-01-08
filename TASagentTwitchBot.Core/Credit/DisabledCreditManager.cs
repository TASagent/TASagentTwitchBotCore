namespace TASagentTwitchBot.Core.Credit;

public class DisabledCreditManager : ICreditManager
{
    public bool IsEnabled => false;

    public DisabledCreditManager() { }

    public Task<bool> HasCredits(Database.User user, string creditType) => Task.FromResult(false);
    public Task<long> GetCredits(Database.User user, string creditType) => Task.FromResult(0L);

    public Task AdjustCredits(Database.User user, string creditType, long delta) => Task.CompletedTask;

    public Task SetCredits(Database.User user, string creditType, long value) => Task.CompletedTask;

    public Task<bool> TryDebit(Database.User user, string creditType, long cost) => Task.FromResult(false);

    public Task<IEnumerable<(string creditType, long value)>> GetAllCredits(Database.User user) =>
        Task.FromResult(Enumerable.Empty<(string creditType, long value)>());
}