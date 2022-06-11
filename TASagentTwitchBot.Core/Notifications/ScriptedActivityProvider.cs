using System.Text;
using System.Web;

using BGC.Scripting;
using BGC.Scripting.Parsing;
using TASagentTwitchBot.Core.TTS;
using TASagentTwitchBot.Core.Scripting;

namespace TASagentTwitchBot.Core.Notifications;

public partial class ScriptedActivityProvider :
    IActivityHandler,
    ISubscriptionHandler,
    ICheerHandler,
    IRaidHandler,
    IGiftSubHandler,
    IFollowerHandler,
    ITTSHandler,
    IScriptedComponent,
    IDisposable
{
    private readonly ICommunication communication;
    private readonly IActivityDispatcher activityDispatcher;
    private readonly Audio.ISoundEffectSystem soundEffectSystem;
    private readonly Audio.IAudioPlayer audioPlayer;
    private readonly Audio.Effects.IAudioEffectSystem audioEffectSystem;
    private readonly ITTSRenderer ttsRenderer;
    private readonly NotificationServer notificationServer;
    private readonly Bits.CheerHelper cheerHelper;
    private readonly IScriptHelper scriptHelper;

    private readonly ScriptedNotificationConfig notificationConfig;

    private readonly Database.IUserHelper userHelper;

    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();

    private GlobalRuntimeContext? globalRuntimeContext;

    private ScriptRuntimeContext? subRuntimeContext;
    private ScriptRuntimeContext? cheerRuntimeContext;
    private ScriptRuntimeContext? raidRuntimeContext;
    private ScriptRuntimeContext? giftSubRuntimeContext;
    private ScriptRuntimeContext? followRuntimeContext;

    private Script? subScript = null;
    private Script? cheerScript = null;
    private Script? raidScript = null;
    private Script? giftSubScript = null;
    private Script? followScript = null;

    public enum ScriptType
    {
        Subscription = 0,
        Cheer,
        Raid,
        GiftSub,
        Follow,
        MAX
    }

    private bool disposedValue;

    public ScriptedActivityProvider(
        ScriptedNotificationConfig notificationConfig,
        ICommunication communication,
        Audio.ISoundEffectSystem soundEffectSystem,
        Audio.IAudioPlayer audioPlayer,
        Audio.Effects.IAudioEffectSystem audioEffectSystem,
        Bits.CheerHelper cheerHelper,
        IActivityDispatcher activityDispatcher,
        ITTSRenderer ttsRenderer,
        NotificationServer notificationServer,
        Database.IUserHelper userHelper,
        IScriptHelper scriptHelper)
    {
        this.notificationConfig = notificationConfig;

        this.communication = communication;

        this.soundEffectSystem = soundEffectSystem;
        this.audioEffectSystem = audioEffectSystem;
        this.audioPlayer = audioPlayer;
        this.cheerHelper = cheerHelper;

        this.activityDispatcher = activityDispatcher;
        this.ttsRenderer = ttsRenderer;
        this.notificationServer = notificationServer;

        this.userHelper = userHelper;
        this.scriptHelper = scriptHelper;
    }

    #region IScriptedComponent

    void IScriptedComponent.Initialize(IScriptRegistrar scriptRegistrar)
    {
        globalRuntimeContext = scriptRegistrar.GlobalSharedRuntimeContext;

        ClassRegistrar.TryRegisterClass<ScriptingUser>("User");
        ClassRegistrar.TryRegisterClass<NotificationSub>("Sub");
        ClassRegistrar.TryRegisterClass<NotificationCheer>("Cheer");
        ClassRegistrar.TryRegisterClass<NotificationGiftSub>("GiftSub");
        ClassRegistrar.TryRegisterClass<NotificationAudio>("Audio");
        ClassRegistrar.TryRegisterClass<NotificationSoundEffect>("SoundEffect");
        ClassRegistrar.TryRegisterClass<NotificationPause>("Pause");
        ClassRegistrar.TryRegisterClass<NotificationTTS>("TTS");
        ClassRegistrar.TryRegisterClass<NotificationText>("Text");

        ClassRegistrar.TryRegisterClass<IAlert>();
        ClassRegistrar.TryRegisterClass<StandardAlert>();
        ClassRegistrar.TryRegisterClass<NoAlert>();

        //Prepare Scripts
        SetScript(ScriptType.Subscription, notificationConfig.SubNotificationScript, true);
        SetScript(ScriptType.Cheer, notificationConfig.CheerNotificationScript, true);
        SetScript(ScriptType.Raid, notificationConfig.RaidNotificationScript, true);
        SetScript(ScriptType.GiftSub, notificationConfig.GiftSubNotificationScript, true);
        SetScript(ScriptType.Follow, notificationConfig.FollowNotificationScript, true);
    }

    public IEnumerable<string> GetScriptNames()
    {
        string[] scriptNames = new string[(int)ScriptType.MAX];
        for (ScriptType scriptType = 0; scriptType < ScriptType.MAX; scriptType++)
        {
            scriptNames[(int)scriptType] = ToName(scriptType);
        }

        return scriptNames;
    }

    public string? GetScript(string scriptName)
    {
        ScriptType scriptType = ToScriptType(scriptName);

        if (scriptType == ScriptType.MAX)
        {
            communication.SendErrorMessage($"Unexpected ScriptName: {scriptName}.");
            return null;
        }

        return notificationConfig.GetScript(scriptType);
    }

    public string? GetDefaultScript(string scriptName)
    {
        ScriptType scriptType = ToScriptType(scriptName);

        if (scriptType == ScriptType.MAX)
        {
            communication.SendErrorMessage($"Unexpected ScriptName: {scriptName}.");
            return null;
        }

        return ScriptedNotificationConfig.GetDefaultScript(scriptType);
    }

    public bool SetScript(string scriptName, string script)
    {
        ScriptType scriptType = ToScriptType(scriptName);

        if (scriptType == ScriptType.MAX)
        {
            communication.SendErrorMessage($"Unexpected ScriptName: {scriptName}.");
            return false;
        }

        return SetScript(scriptType, script);
    }

    private bool SetScript(
        ScriptType scriptType,
        string script,
        bool suppressUpdate = false)
    {
        try
        {
            Script newScript = ScriptParser.LexAndParseScript(script, ScriptedNotificationConfig.GetRequiredFunctions(scriptType));

            switch (scriptType)
            {
                case ScriptType.Subscription:
                    subScript = newScript;
                    subRuntimeContext = newScript.PrepareScript(globalRuntimeContext!);
                    break;

                case ScriptType.Cheer:
                    cheerScript = newScript;
                    cheerRuntimeContext = newScript.PrepareScript(globalRuntimeContext!);
                    break;

                case ScriptType.Raid:
                    raidScript = newScript;
                    raidRuntimeContext = newScript.PrepareScript(globalRuntimeContext!);
                    break;

                case ScriptType.GiftSub:
                    giftSubScript = newScript;
                    giftSubRuntimeContext = newScript.PrepareScript(globalRuntimeContext!);
                    break;

                case ScriptType.Follow:
                    followScript = newScript;
                    followRuntimeContext = newScript.PrepareScript(globalRuntimeContext!);
                    break;

                default:
                    communication.SendErrorMessage($"ScriptType not implemented: {scriptType}");
                    return false;
            }

            if (!suppressUpdate)
            {
                notificationConfig.UpdateScript(scriptType, script);
                notificationConfig.Serialize();
            }

            return true;
        }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"Failed to parse {ToName(scriptType)} script: {ex.Message}");
            return false;
        }
    }

    #endregion IScriptedComponent

    public Task Execute(ActivityRequest activityRequest)
    {
        List<Task> taskList = new List<Task>();

        if (activityRequest is IOverlayActivity overlayActivity && overlayActivity.NotificationMessage is not null)
        {
            taskList.Add(notificationServer.ShowNotificationAsync(overlayActivity.NotificationMessage));
        }

        if (activityRequest is IAudioActivity audioActivity && audioActivity.AudioRequest is not null)
        {
            taskList.Add(audioPlayer.PlayAudioRequest(audioActivity.AudioRequest));
        }

        if (activityRequest is IMarqueeMessageActivity marqueeMessageActivity && !string.IsNullOrEmpty(marqueeMessageActivity.MarqueeMessage))
        {
            //Don't bother waiting on this one to complete
            taskList.Add(notificationServer.ShowTTSMessageAsync(marqueeMessageActivity.MarqueeMessage));
        }

        return Task.WhenAll(taskList).WithCancellation(generalTokenSource.Token);
    }

    protected delegate Task<string> GetDefaultImageAsync();
    protected delegate string GetDefaultImage();

    protected async Task HandleAlertData(
        IAlert alertData,
        string description,
        GetDefaultImageAsync getDefaultImage,
        bool approved)
    {
        switch (alertData)
        {
            case StandardAlert standardAlertData:
                activityDispatcher.QueueActivity(
                    activity: new ScriptedActivityRequest(
                        activityHandler: this,
                        description: description,
                        notificationMessage: new ImageNotificationMessage(
                            image: string.IsNullOrEmpty(standardAlertData.ImageOverride) ? (await getDefaultImage()) : standardAlertData.ImageOverride,
                            duration: 1_000 * standardAlertData.Duration,
                            message: TransformImageText(standardAlertData.ImageText)),
                        audioRequest: await TransformNotificationAudio(standardAlertData.Audio),
                        marqueeMessage: TransformMarqueeText(standardAlertData.MarqueText)),
                    approved: approved);
                break;

            case NoAlert _:
                //Do nothing
                break;

            default:
                communication.SendErrorMessage($"Unhandled alertData type: {alertData}");
                break;
        }
    }

    protected async Task HandleAlertData(
        IAlert alertData,
        string description,
        GetDefaultImage getDefaultImage,
        bool approved)
    {
        switch (alertData)
        {
            case StandardAlert standardAlertData:
                activityDispatcher.QueueActivity(
                    activity: new ScriptedActivityRequest(
                        activityHandler: this,
                        description: description,
                        notificationMessage: new ImageNotificationMessage(
                            image: string.IsNullOrEmpty(standardAlertData.ImageOverride) ? getDefaultImage() : standardAlertData.ImageOverride,
                            duration: 1_000 * standardAlertData.Duration,
                            message: TransformImageText(standardAlertData.ImageText)),
                        audioRequest: await TransformNotificationAudio(standardAlertData.Audio),
                        marqueeMessage: TransformMarqueeText(standardAlertData.MarqueText)),
                    approved: approved);
                break;

            case NoAlert _:
                //Do nothing
                break;

            default:
                communication.SendErrorMessage($"Unhandled alertData type: {alertData}");
                break;
        }
    }

    #region ISubscriptionHandler

    public virtual async void HandleSubscription(
        string userId,
        string message,
        int monthCount,
        int tier,
        bool approved)
    {
        Database.User? subscriber = await userHelper.GetUserByTwitchId(userId);

        if (subscriber is null)
        {
            communication.SendErrorMessage($"Unable to find user {userId} for Subscription handling");
            return;
        }

        communication.NotifyEvent($"Tier {tier} Sub: {subscriber.TwitchUserName}");

        if (!notificationConfig.SubNotificationEnabled)
        {
            return;
        }

        if (subScript is not null && subRuntimeContext is not null)
        {
            try
            {
                ScriptingUser user = scriptHelper.GetScriptingUser(subscriber);
                NotificationSub sub = new NotificationSub(tier, monthCount, message);

                IAlert alertData = await subScript.ExecuteFunctionAsync<IAlert>(
                    "GetAlertData", 2_000, subRuntimeContext, user, sub);

                await HandleAlertData(
                    alertData: alertData,
                    description: $"Sub: {subscriber.TwitchUserName} \"{message}\"",
                    getDefaultImage: notificationServer.GetNextImageURL,
                    approved: approved);
            }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"Failed to run Sub notification script: {ex.Message}");
            }
        }
        else
        {
            communication.SendErrorMessage($"No sub script defined. Skipping.");
        }
    }

    #endregion ISubscriptionHandler
    #region ICheerHandler

    public virtual async void HandleCheer(
        Database.User cheerer,
        string message,
        int quantity,
        bool approved)
    {
        communication.NotifyEvent($"Cheer {quantity}: {cheerer.TwitchUserName}");

        if (!notificationConfig.CheerNotificationEnabled)
        {
            return;
        }

        if (cheerScript is not null && cheerRuntimeContext is not null)
        {
            try
            {
                ScriptingUser user = scriptHelper.GetScriptingUser(cheerer);
                NotificationCheer cheer = new NotificationCheer(quantity, message);

                IAlert alertData = await cheerScript.ExecuteFunctionAsync<IAlert>(
                    "GetAlertData", 2_000, cheerRuntimeContext, user, cheer);

                await HandleAlertData(
                    alertData: alertData,
                    description: $"Cheer {quantity}: {cheerer.TwitchUserName} \"{message}\"",
                    getDefaultImage: () => cheerHelper.GetCheerImageURL(message, quantity)!,
                    approved: approved);
            }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"Failed to run Cheer notification script: {ex.Message}");
            }
        }
        else
        {
            communication.SendErrorMessage($"No cheer script defined. Skipping.");
        }
    }


    #endregion ICheerHandler
    #region IRaidHandler

    public virtual async void HandleRaid(
        string raiderId,
        int count,
        bool approved)
    {
        Database.User? raider = await userHelper.GetUserByTwitchId(raiderId);

        if (raider is null)
        {
            communication.SendErrorMessage($"Unable to find user {raiderId} for Raid handling");
            return;
        }

        communication.NotifyEvent($"{count} Raid: {raider.TwitchUserName}");

        if (!notificationConfig.RaidNotificationEnabled)
        {
            return;
        }

        if (raidScript is not null && raidRuntimeContext is not null)
        {
            try
            {
                ScriptingUser user = scriptHelper.GetScriptingUser(raider);

                IAlert alertData = await raidScript.ExecuteFunctionAsync<IAlert>(
                    "GetAlertData", 2_000, raidRuntimeContext, user, count);

                await HandleAlertData(
                    alertData: alertData,
                    description: $"Raid: {raider.TwitchUserName} with {count} viewers",
                    getDefaultImage: notificationServer.GetNextImageURL,
                    approved: approved);
            }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"Failed to run Raid notification script: {ex.Message}");
            }
        }
        else
        {
            communication.SendErrorMessage($"No raid script defined. Skipping.");
        }
    }

    #endregion IRaidHandler
    #region IGiftSubHandler

    public virtual async void HandleGiftSub(
        string senderId,
        string recipientId,
        int tier,
        int months,
        bool approved)
    {
        Database.User? sender = await userHelper.GetUserByTwitchId(senderId);
        Database.User? recipient = await userHelper.GetUserByTwitchId(recipientId);

        if (sender is null)
        {
            communication.SendErrorMessage($"Unable to find sender {senderId} for Gift Sub handling");
            return;
        }

        if (recipient is null)
        {
            communication.SendErrorMessage($"Unable to find reciever {recipientId} for Gift Sub handling");
            return;
        }

        communication.NotifyEvent($"Gift Sub from {sender.TwitchUserName} to {recipient.TwitchUserName}");

        if (!notificationConfig.GiftSubNotificationEnabled)
        {
            return;
        }


        if (giftSubScript is not null && giftSubRuntimeContext is not null)
        {
            try
            {
                ScriptingUser notificationSender = scriptHelper.GetScriptingUser(sender);
                ScriptingUser notificationRecipient = scriptHelper.GetScriptingUser(recipient);

                NotificationGiftSub notificationGiftSub = new NotificationGiftSub(tier, months);

                IAlert alertData = await giftSubScript.ExecuteFunctionAsync<IAlert>(
                    "GetAlertData", 2_000, giftSubRuntimeContext, notificationSender, notificationRecipient, notificationGiftSub);

                await HandleAlertData(
                    alertData: alertData,
                    description: $"Gift Sub From {sender.TwitchUserName} To {recipient.TwitchUserName}",
                    getDefaultImage: notificationServer.GetNextImageURL,
                    approved: approved);
            }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"Failed to run GiftSub notification script: {ex.Message}");
            }
        }
        else
        {
            communication.SendErrorMessage($"No GiftSub script defined. Skipping.");
        }
    }

    public virtual async void HandleAnonGiftSub(
        string recipientId,
        int tier,
        int months,
        bool approved)
    {
        Database.User? recipient = await userHelper.GetUserByTwitchId(recipientId);

        if (recipient is null)
        {
            communication.SendErrorMessage($"Unable to find reciever {recipientId} for Gift Sub handling");
            return;
        }

        communication.NotifyEvent($"Gift Sub from Anon to {recipient.TwitchUserName}");

        if (!notificationConfig.AnonGiftSubNotificationEnabled)
        {
            return;
        }

        if (giftSubScript is not null && giftSubRuntimeContext is not null)
        {
            try
            {
                ScriptingUser notificationRecipient = scriptHelper.GetScriptingUser(recipient);

                NotificationGiftSub notificationGiftSub = new NotificationGiftSub(tier, months);

                IAlert alertData = await giftSubScript.ExecuteFunctionAsync<IAlert>(
                    "GetAnonAlertData", 2_000, giftSubRuntimeContext, notificationRecipient, notificationGiftSub);

                await HandleAlertData(
                    alertData: alertData,
                    description: $"Gift Sub From Anon To {recipient.TwitchUserName}",
                    getDefaultImage: notificationServer.GetNextImageURL,
                    approved: approved);
            }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"Failed to run GiftSub notification script: {ex.Message}");
            }
        }
        else
        {
            communication.SendErrorMessage($"No GiftSub script defined. Skipping.");
        }
    }

    #endregion IGiftSubHandler
    #region IFollowerHandler

    public virtual async void HandleFollower(
        Database.User follower,
        bool approved)
    {
        communication.NotifyEvent($"Follow: {follower.TwitchUserName}");

        if (!notificationConfig.FollowNotificationEnabled)
        {
            return;
        }

        if (followScript is not null && followRuntimeContext is not null)
        {
            try
            {
                ScriptingUser notificationFollower = scriptHelper.GetScriptingUser(follower);

                IAlert alertData = await followScript.ExecuteFunctionAsync<IAlert>(
                    "GetAlertData", 2_000, followRuntimeContext, notificationFollower);

                await HandleAlertData(
                    alertData: alertData,
                    description: $"Follower: {follower.TwitchUserName}",
                    getDefaultImage: notificationServer.GetNextImageURL,
                    approved: approved);
            }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"Failed to run Follow notification script: {ex.Message}");
            }
        }
        else
        {
            communication.SendErrorMessage($"No Follow script defined. Skipping.");
        }
    }

    #endregion IFollowerHandler
    #region ITTSHandler

    Task<bool> ITTSHandler.SetTTSEnabled(bool enabled) => ttsRenderer.SetTTSEnabled(enabled);

    public virtual async void HandleTTS(
        Database.User user,
        string message,
        bool approved)
    {
        if (!notificationConfig.TTSNotificationEnabled)
        {
            return;
        }

        activityDispatcher.QueueActivity(
            activity: new ScriptedActivityRequest(
                activityHandler: this,
                description: $"TTS {user.TwitchUserName} : {message}",
                notificationMessage: null,
                audioRequest: await ttsRenderer.TTSRequest(
                    authorizationLevel: user.AuthorizationLevel,
                    voicePreference: user.TTSVoicePreference,
                    pitchPreference: user.TTSPitchPreference,
                    speedPreference: user.TTSSpeedPreference,
                    effectsChain: audioEffectSystem.SafeParse(user.TTSEffectsChain),
                    ttsText: message),
                marqueeMessage: TransformMarqueeText(new List<NotificationText>() {
                    new NotificationText(user.TwitchUserName, string.IsNullOrWhiteSpace(user.Color) ? "#0000FF" : user.Color),
                    new NotificationText($": {message}", "#FFFFFF")})),
            approved: approved);
    }

    #endregion ITTSHandler

    private static string TransformImageText(List<NotificationText> imageText)
    {
        StringBuilder sb = new StringBuilder();

        foreach (NotificationText text in imageText)
        {
            sb.Append($"<span style=\"color: {text.Color}\">{HttpUtility.HtmlEncode(text.Text)}</span>");
        }

        return sb.ToString();
    }

    private static string TransformMarqueeText(List<NotificationText> marqueeText)
    {
        if (marqueeText.Count == 0)
        {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("<h1>");

        foreach (NotificationText text in marqueeText)
        {
            sb.Append($"<span style=\"color: {text.Color}\">{HttpUtility.HtmlEncode(text.Text)}</span>");
        }

        sb.Append("</h1>");
        return sb.ToString();
    }

    private async Task<Audio.AudioRequest?> TransformNotificationAudio(List<NotificationAudio> audioRequest)
    {
        List<Audio.AudioRequest?> transformedAudioRequests = new List<Audio.AudioRequest?>();

        foreach (NotificationAudio audio in audioRequest)
        {
            if (audio is null)
            {
                continue;
            }

            switch (audio)
            {
                case NotificationSoundEffect soundEffect:
                    if (soundEffectSystem.HasSoundEffects())
                    {
                        Audio.SoundEffect? subSoundEffect = soundEffectSystem.GetSoundEffectByName(soundEffect.SoundEffectName);
                        if (subSoundEffect is null)
                        {
                            communication.SendWarningMessage($"Expected SoundEffect {soundEffect.SoundEffectName} not found. Defaulting to first sound effect.");
                            subSoundEffect = soundEffectSystem.GetAnySoundEffect();
                        }

                        if (subSoundEffect is not null)
                        {
                            transformedAudioRequests.Add(new Audio.SoundEffectRequest(subSoundEffect));
                        }
                    }
                    break;

                case NotificationPause pause:
                    transformedAudioRequests.Add(new Audio.AudioDelay(pause.PauseDurationMS));
                    break;

                case NotificationTTS tts:
                    transformedAudioRequests.Add(await ttsRenderer.TTSRequest(
                        authorizationLevel: Commands.AuthorizationLevel.Moderator,
                        voicePreference: tts.TTSVoice,
                        pitchPreference: tts.TTSPitch,
                        speedPreference: tts.TTSSpeed,
                        effectsChain: audioEffectSystem.SafeParse(tts.TTSEffect),
                        ttsText: tts.Message));
                    break;

                default:
                    communication.SendErrorMessage($"Unexpected NotificationAudio type {audio.GetType().Name}.");
                    break;
            }
        }

        return Audio.AudioTools.JoinRequests(0, transformedAudioRequests);
    }

    private readonly Dictionary<string, ScriptType> scriptLookup = new Dictionary<string, ScriptType>();

    private static string ToName(ScriptType scriptType) => scriptType switch
    {
        ScriptType.Subscription => "Subscription",
        ScriptType.Cheer => "Cheer",
        ScriptType.Raid => "Raid",
        ScriptType.GiftSub => "GiftSub",
        ScriptType.Follow => "Follow",
        _ => ""
    };

    private ScriptType ToScriptType(string scriptName)
    {
        if (scriptLookup.Count == 0)
        {
            for (ScriptType scriptType = 0; scriptType < ScriptType.MAX; scriptType++)
            {
                scriptLookup.Add(ToName(scriptType), scriptType);
            }
        }

        if (!scriptLookup.TryGetValue(scriptName, out ScriptType returnValue))
        {
            return ScriptType.MAX;
        }

        return returnValue;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                generalTokenSource.Cancel();
                generalTokenSource.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public class ScriptedActivityRequest : ActivityRequest, IAudioActivity, IOverlayActivity, IMarqueeMessageActivity
    {
        public NotificationMessage? NotificationMessage { get; }
        public Audio.AudioRequest? AudioRequest { get; }
        public string? MarqueeMessage { get; }

        public ScriptedActivityRequest(
            IActivityHandler activityHandler,
            string description,
            NotificationMessage? notificationMessage = null,
            Audio.AudioRequest? audioRequest = null,
            string? marqueeMessage = null)
            : base(activityHandler, description)
        {
            NotificationMessage = notificationMessage;
            AudioRequest = audioRequest;
            MarqueeMessage = marqueeMessage;
        }
    }

    public interface IAlert
    {

    }

    public class StandardAlert : IAlert
    {
        /// <summary>
        /// Duration of the alert, in seconds
        /// </summary>
        public double Duration { get; set; } = 5.0;
        public string ImageOverride { get; set; } = "";

        public List<NotificationText> ImageText { get; set; } = new List<NotificationText>();

        public List<NotificationAudio> Audio { get; set; } = new List<NotificationAudio>();

        public List<NotificationText> MarqueText { get; set; } = new List<NotificationText>();

        public StandardAlert() { }

        public StandardAlert(
            string imageOverride,
            List<NotificationText> imageText,
            List<NotificationAudio> audio,
            List<NotificationText> marqueText)
        {
            ImageOverride = imageOverride;
            ImageText = imageText;
            Audio = audio;
            MarqueText = marqueText;
        }
    }

    public class NoAlert : IAlert
    {

    }

    public class NotificationSub
    {
        public int Tier { get; set; } = 0;
        public int CumulativeMonths { get; set; } = 0;
        public string Message { get; set; } = "";

        public NotificationSub() { }

        public NotificationSub(int tier, int months, string message)
        {
            Tier = tier;
            CumulativeMonths = months;
            Message = message;
        }
    }

    public class NotificationCheer
    {
        public int Quantity { get; set; } = 0;
        public string Message { get; set; } = "";

        public NotificationCheer() { }

        public NotificationCheer(int quantity, string message)
        {
            Quantity = quantity;
            Message = message;
        }
    }

    public class NotificationGiftSub
    {
        public int Tier { get; set; } = 0;
        public int Months { get; set; } = 0;

        public NotificationGiftSub() { }

        public NotificationGiftSub(int tier, int months)
        {
            Tier = tier;
            Months = months;
        }
    }

    public class NotificationAudio
    {

    }

    public class NotificationSoundEffect : NotificationAudio
    {
        public string SoundEffectName { get; set; } = "";

        public NotificationSoundEffect() { }

        public NotificationSoundEffect(string soundEffectName)
        {
            SoundEffectName = soundEffectName;
        }
    }

    public class NotificationPause : NotificationAudio
    {
        public int PauseDurationMS { get; set; } = 1000;

        public NotificationPause() { }

        public NotificationPause(int pauseDurationMS)
        {
            PauseDurationMS = pauseDurationMS;
        }
    }


    public class NotificationTTS : NotificationAudio
    {
        public TTSVoice TTSVoice { get; set; } = TTSVoice.Unassigned;
        public TTSPitch TTSPitch { get; set; } = TTSPitch.Unassigned;
        public TTSSpeed TTSSpeed { get; set; } = TTSSpeed.Unassigned;
        public string TTSEffect { get; set; } = "";
        public string Message { get; set; } = "";

        public NotificationTTS() { }

        public NotificationTTS(ScriptingUser user, string message)
        {
            TTSVoice = user.TTSVoice;
            TTSPitch = user.TTSPitch;
            TTSSpeed = user.TTSSpeed;
            TTSEffect = user.TTSEffect;

            Message = message;
        }

        public NotificationTTS(
            string ttsVoice,
            string ttsPitch,
            string ttsSpeed,
            string ttsEffect,
            string message)
        {
            TTSVoice = ttsVoice.TranslateTTSVoice();
            TTSPitch = ttsPitch.TranslateTTSPitch();
            TTSSpeed = ttsSpeed.TranslateTTSSpeed();
            TTSEffect = ttsEffect;

            Message = message;
        }

        public NotificationTTS(
            TTSVoice ttsVoice,
            TTSPitch ttsPitch,
            TTSSpeed ttsSpeed,
            string ttsEffect,
            string message)
        { 
            TTSVoice = ttsVoice;
            TTSPitch = ttsPitch;
            TTSSpeed = ttsSpeed;
            TTSEffect = ttsEffect;

            Message = message;
        }
    }

    public class NotificationText
    {
        public string Text { get; set; } = "";
        public string Color { get; set; } = "";

        public NotificationText() { }

        public NotificationText(string text, string color)
        {
            Text = text;
            Color = color;
        }

        public NotificationText(ScriptingUser user)
        {
            Text = user.TwitchUserName;
            Color = user.Color;
        }
    }
}
