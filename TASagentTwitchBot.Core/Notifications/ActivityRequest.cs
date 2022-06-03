namespace TASagentTwitchBot.Core.Notifications;

public abstract class ActivityRequest
{
    public int Id { get; set; } = 0;

    public bool Played { get; set; } = false;

    private readonly IActivityHandler activityHandler;
    private readonly string description;

    public ActivityRequest(
        IActivityHandler activityHandler,
        string description)
    {
        this.description = description;
        this.activityHandler = activityHandler;
    }

    public override string ToString() => description;

    public virtual Task Execute() => activityHandler.Execute(this);
}

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
