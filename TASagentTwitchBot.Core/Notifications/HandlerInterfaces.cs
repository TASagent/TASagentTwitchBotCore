namespace TASagentTwitchBot.Core.Notifications;

[AutoRegister]
public interface IActivityHandler
{
    Task Execute(ActivityRequest activityRequest);
}

[AutoRegister]
public interface ISubscriptionHandler
{
    void HandleSubscription(string userId, string message, int monthCount, int tier, bool approved);
}

[AutoRegister]
public interface ICheerHandler
{
    void HandleCheer(Database.User cheerer, string message, int quantity, bool approved);
}

[AutoRegister]
public interface IRaidHandler
{
    void HandleRaid(string raiderId, int count, bool approved);
}

[AutoRegister]
public interface IGiftSubHandler
{
    void HandleGiftSub(string senderId, string recipientId, int tier, int months, bool approved);
    void HandleAnonGiftSub(string recipientId, int tier, int months, bool approved);
}

[AutoRegister]
public interface IFollowerHandler
{
    void HandleFollower(Database.User follower, bool approved);
}

[AutoRegister]
public interface ITTSHandler
{
    bool IsTTSVoiceValid(string voice);
    TTS.TTSVoiceInfo? GetTTSVoiceInfo(string voice);
    Task<bool> SetTTSEnabled(bool enabled); 
    void HandleTTS(Database.User user, string message, bool approved);
}
