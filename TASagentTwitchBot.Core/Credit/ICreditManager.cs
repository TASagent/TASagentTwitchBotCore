namespace TASagentTwitchBot.Core.Credit;

public interface ICreditManager
{
    bool IsEnabled { get; }

    Task<bool> HasCredits(Database.User user, string creditType);
    Task<long> GetCredits(Database.User user, string creditType);

    Task AdjustCredits(Database.User user, string creditType, long delta);
    Task SetCredits(Database.User user, string creditType, long value);

    Task<bool> TryDebit(Database.User user, string creditType, long cost);

    Task<IEnumerable<(string creditType, long value)>> GetAllCredits(Database.User user);
}
