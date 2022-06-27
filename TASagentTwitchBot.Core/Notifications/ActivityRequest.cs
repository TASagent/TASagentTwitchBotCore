namespace TASagentTwitchBot.Core.Notifications;

public abstract class ActivityRequest
{
    public int Id { get; set; } = 0;

    public string RequesterId { get; }

    public bool Played { get; set; } = false;

    private readonly IActivityHandler activityHandler;
    private readonly string description;

    public ActivityRequest(
        IActivityHandler activityHandler,
        string description,
        string requesterId)
    {
        this.description = description;
        this.activityHandler = activityHandler;
        RequesterId = requesterId;
    }

    public override string ToString() => description;

    public virtual Task Execute() => activityHandler.Execute(this);
}

//These interfaces are used to extract features from Activity Requests

public interface IAudioActivity
{
    Audio.AudioRequest? AudioRequest { get; }
}

public interface IOverlayActivity
{
    NotificationMessage? NotificationMessage { get; }
}

public interface IMarqueeMessageActivity
{
    string? MarqueeMessage { get; }
}
