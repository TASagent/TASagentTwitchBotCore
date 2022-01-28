namespace TASagentTwitchBot.Core.Notifications;


public class ActivityProviderStubs :
    IRaidHandler,
    IGiftSubHandler,
    IFollowerHandler,
    ICheerHandler,
    ISubscriptionHandler,
    ITTSHandler
{

    public virtual void HandleCheer(Database.User cheerer, string message, int quantity, bool approved) { }

    public virtual void HandleFollower(Database.User follower, bool approved) { }

    public virtual void HandleGiftSub(string senderId, string recipientId, int tier, int months, bool approved) { }
    public virtual void HandleAnonGiftSub(string recipientId, int tier, int months, bool approved) { }

    public virtual void HandleRaid(string raiderId, int count, bool approved) { }

    public virtual void HandleSubscription(string userId, string message, int monthCount, int tier, bool approved) { }

    public virtual void HandleTTS(Database.User user, string message, bool approved) { }
}

public sealed class RaidHandlerStub : IRaidHandler
{
    public void HandleRaid(string raiderId, int count, bool approved) { }
}

public sealed class GiftSubHandlerStub : IGiftSubHandler
{
    public void HandleGiftSub(string senderId, string recipientId, int tier, int months, bool approved) { }
    public void HandleAnonGiftSub(string recipientId, int tier, int months, bool approved) { }
}

public sealed class FollowerHandlerStub : IFollowerHandler
{
    public void HandleFollower(Database.User follower, bool approved) { }
}

public sealed class CheerHandlerStub : ICheerHandler
{
    public void HandleCheer(Database.User cheerer, string message, int quantity, bool approved) { }
}

public sealed class SubscriptionHandlerStub : ISubscriptionHandler
{
    public void HandleSubscription(string userId, string message, int monthCount, int tier, bool approved) { }
}

public sealed class TTSHandlerStub : ITTSHandler
{
    public void HandleTTS(Database.User user, string message, bool approved) { }
}
