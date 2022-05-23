using TASagentTwitchBot.Core.Database;

namespace TASagentTwitchBot.Core.Notifications;


public class ActivityProviderStubs :
    IActivityHandler,
    IRaidHandler,
    IGiftSubHandler,
    IFollowerHandler,
    ICheerHandler,
    ISubscriptionHandler,
    ITTSHandler
{
    public virtual Task Execute(ActivityRequest activityRequest) => Task.CompletedTask;

    public virtual void HandleCheer(User cheerer, string message, int quantity, bool approved) { }

    public virtual void HandleFollower(User follower, bool approved) { }

    public virtual void HandleGiftSub(string senderId, string recipientId, int tier, int months, bool approved) { }
    public virtual void HandleAnonGiftSub(string recipientId, int tier, int months, bool approved) { }

    public virtual void HandleRaid(string raiderId, int count, bool approved) { }

    public virtual void HandleSubscription(string userId, string message, int monthCount, int tier, bool approved) { }

    public virtual void HandleTTS(User user, string message, bool approved) { }

    //Always return Success for disabling, and Failure from enabling
    Task<bool> ITTSHandler.SetTTSEnabled(bool enabled) => Task.FromResult(!enabled);
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
    public void HandleFollower(User follower, bool approved) { }
}

public sealed class CheerHandlerStub : ICheerHandler
{
    public void HandleCheer(User cheerer, string message, int quantity, bool approved) { }
}

public sealed class SubscriptionHandlerStub : ISubscriptionHandler
{
    public void HandleSubscription(string userId, string message, int monthCount, int tier, bool approved) { }
}

public sealed class TTSHandlerStub : ITTSHandler
{
    public void HandleTTS(User user, string message, bool approved) { }

    //Always return Success for disabling, and Failure from enabling
    Task<bool> ITTSHandler.SetTTSEnabled(bool enabled) => Task.FromResult(!enabled);
}
