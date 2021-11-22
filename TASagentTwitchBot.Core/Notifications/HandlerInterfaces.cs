namespace TASagentTwitchBot.Core.Notifications;

public interface ISubscriptionHandler
{
    void HandleSubscription(string userId, string message, int monthCount, int tier, bool approved);
}

public interface ICheerHandler
{
    void HandleCheer(Database.User cheerer, string message, int quantity, bool approved);
}

public interface IRaidHandler
{
    void HandleRaid(string raiderId, int count, bool approved);
}

public interface IGiftSubHandler
{
    void HandleGiftSub(string senderId, string recipientId, int tier, int months, bool approved);
    void HandleAnonGiftSub(string recipientId, int tier, int months, bool approved);
}

public interface IFollowerHandler
{
    void HandleFollower(Database.User follower, bool approved);
}

public interface ITTSHandler
{
    void HandleTTS(Database.User user, string message, bool approved);
}
