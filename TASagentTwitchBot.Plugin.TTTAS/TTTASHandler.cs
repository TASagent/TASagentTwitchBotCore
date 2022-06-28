using System.Web;

namespace TASagentTwitchBot.Plugin.TTTAS;

[Core.AutoRegister]
public interface ITTTASHandler
{
    void HandleTTTAS(Core.Database.User user, string message, bool approved);
}

public class TTTASHandler : ITTTASHandler
{
    private readonly Core.ICommunication communication;
    private readonly Core.Notifications.IActivityDispatcher activityDispatcher;
    private readonly Core.Notifications.IActivityHandler activityHandler;
    private readonly Core.Audio.ISoundEffectSystem soundEffectSystem;
    private readonly ITTTASRenderer tttasRenderer;

    private readonly TTTASConfiguration tttasConfig;

    public TTTASHandler(
        Core.ICommunication communication,
        Core.Notifications.IActivityDispatcher activityDispatcher,
        Core.Notifications.IActivityHandler activityHandler,
        Core.Audio.ISoundEffectSystem soundEffectSystem,
        ITTTASRenderer tttasRenderer,
        TTTASConfiguration tttasConfig)
    {
        this.communication = communication;
        this.activityDispatcher = activityDispatcher;
        this.activityHandler = activityHandler;
        this.soundEffectSystem = soundEffectSystem;
        this.tttasRenderer = tttasRenderer;
        this.tttasConfig = tttasConfig;
    }

    public async void HandleTTTAS(
        Core.Database.User user,
        string message,
        bool approved)
    {
        Core.Audio.AudioRequest? tttasAudio = await GetTTTASAudioRequest(user, message);

        if (tttasAudio is not null)
        {
            activityDispatcher.QueueActivity(
                activity: new TTTASActivityRequest(
                    activityHandler: activityHandler,
                    description: $"{tttasConfig.FeatureNameBrief} {user.TwitchUserName}: {message}",
                    requesterId: user.TwitchUserId,
                    audioRequest: tttasAudio,
                    marqueeMessage: GetStandardMarqueeMessage(user, message)),
                approved: approved);
        }
    }

    private async Task<Core.Audio.AudioRequest?> GetTTTASAudioRequest(Core.Database.User _, string message)
    {
        Core.Audio.AudioRequest? soundEffectRequest = null;
        Core.Audio.AudioRequest? tttasRequest = null;

        if (!string.IsNullOrEmpty(tttasConfig.SoundEffect) && soundEffectSystem.HasSoundEffects())
        {
            Core.Audio.SoundEffect? tttasSoundEffect = soundEffectSystem.GetSoundEffectByName(tttasConfig.SoundEffect);
            if (tttasSoundEffect is null)
            {
                communication.SendWarningMessage($"Expected {tttasConfig.FeatureNameBrief} SoundEffect \"{tttasConfig.SoundEffect}\" not found.  Defaulting to first sound effect.");
                tttasSoundEffect = soundEffectSystem.GetAnySoundEffect();
            }

            if (tttasSoundEffect is not null)
            {
                soundEffectRequest = new Core.Audio.SoundEffectRequest(tttasSoundEffect);
            }
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            tttasRequest = await tttasRenderer.TTTASRequest(message);
        }

        if (tttasRequest is null)
        {
            return null;
        }

        return Core.Audio.AudioTools.JoinRequests(300, soundEffectRequest, tttasRequest);
    }

    public class TTTASActivityRequest : Core.Notifications.ActivityRequest, Core.Notifications.IAudioActivity, Core.Notifications.IMarqueeMessageActivity
    {
        public Core.Audio.AudioRequest? AudioRequest { get; }
        public string? MarqueeMessage { get; }

        public TTTASActivityRequest(
            Core.Notifications.IActivityHandler activityHandler,
            string description,
            string requesterId,
            Core.Audio.AudioRequest? audioRequest = null,
            string? marqueeMessage = null)
            : base(activityHandler, description, requesterId)
        {
            AudioRequest = audioRequest;
            MarqueeMessage = marqueeMessage;
        }
    }

    private string? GetStandardMarqueeMessage(Core.Database.User user, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        return $"<h1><span style=\"color: {(string.IsNullOrWhiteSpace(user.Color) ? "#0000FF" : user.Color)}\" >{HttpUtility.HtmlEncode(user.TwitchUserName)}</span>: {HttpUtility.HtmlEncode(message)}</h1>";
    }
}
