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
        Database.IUserHelper userHelper)
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
    }

    public static void RegisterRequiredScriptingClasses()
    {
        ClassRegistrar.TryRegisterClass<ScriptingUser>("User");
        ClassRegistrar.TryRegisterClass<NotificationSub>("Sub");
        ClassRegistrar.TryRegisterClass<NotificationCheer>("Cheer");
        ClassRegistrar.TryRegisterClass<NotificationGiftSub>("GiftSub");
        ClassRegistrar.TryRegisterClass<NotificationAudio>("Audio");
        ClassRegistrar.TryRegisterClass<NotificationSoundEffect>("SoundEffect");
        ClassRegistrar.TryRegisterClass<NotificationPause>("Pause");
        ClassRegistrar.TryRegisterClass<NotificationTTS>("TTS");
        ClassRegistrar.TryRegisterClass<NotificationText>("Text");
        ClassRegistrar.TryRegisterClass<NotificationData>("NotificationData");
    }

    #region IScriptedComponent

    void IScriptedComponent.Initialize(IScriptRegistrar scriptRegistrar)
    {
        globalRuntimeContext = scriptRegistrar.GlobalSharedRuntimeContext;

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

        if (activityRequest is IMarqueeMessageActivity marqueeMessageActivity && marqueeMessageActivity.MarqueeMessage is not null)
        {
            //Don't bother waiting on this one to complete
            taskList.Add(notificationServer.ShowTTSMessageAsync(marqueeMessageActivity.MarqueeMessage));
        }

        return Task.WhenAll(taskList).WithCancellation(generalTokenSource.Token);
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
                ScriptingUser user = ScriptingUser.FromDB(subscriber);
                NotificationSub sub = new NotificationSub(tier, monthCount, message);

                NotificationData notificationData = await subScript.ExecuteFunctionAsync<NotificationData>(
                    "GetNotificationData", 2_000, subRuntimeContext, user, sub);

                if (!string.IsNullOrWhiteSpace(notificationData.ChatMessage))
                {
                    communication.SendPublicChatMessage(notificationData.ChatMessage);
                }

                if (!notificationData.ShowNotification)
                {
                    return;
                }

                if (string.IsNullOrEmpty(notificationData.Image))
                {
                    notificationData.Image = notificationServer.GetNextImageURL();
                }

                activityDispatcher.QueueActivity(
                    activity: new ScriptedActivityRequest(
                        activityHandler: this,
                        description: $"Sub: {subscriber.TwitchUserName}: {message}",
                        notificationMessage: new ImageNotificationMessage(
                            image: notificationData.Image,
                            duration: 5000,
                            message: TransformImageText(notificationData.ImageText)),
                        audioRequest: await TransformNotificationAudio(notificationData.Audio),
                        marqueeMessage: notificationData.ShowMarqueeMessage ? new MarqueeMessage(user.TwitchUserName, message, user.Color) : null),
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
                ScriptingUser user = ScriptingUser.FromDB(cheerer);
                NotificationCheer cheer = new NotificationCheer(quantity, message);

                NotificationData notificationData = await cheerScript.ExecuteFunctionAsync<NotificationData>(
                    "GetNotificationData", 2_000, cheerRuntimeContext, user, cheer);

                if (!string.IsNullOrWhiteSpace(notificationData.ChatMessage))
                {
                    communication.SendPublicChatMessage(notificationData.ChatMessage);
                }

                if (!notificationData.ShowNotification)
                {
                    return;
                }

                if (string.IsNullOrEmpty(notificationData.Image))
                {
                    notificationData.Image = (await cheerHelper.GetCheerImageURL(message, quantity))!;
                }

                activityDispatcher.QueueActivity(
                    activity: new ScriptedActivityRequest(
                        activityHandler: this,
                        description: $"Cheer {quantity}: {cheerer.TwitchUserName}: {message}",
                        notificationMessage: new ImageNotificationMessage(
                            image: notificationData.Image,
                            duration: 10_000,
                            message: TransformImageText(notificationData.ImageText)),
                        audioRequest: await TransformNotificationAudio(notificationData.Audio),
                        marqueeMessage: notificationData.ShowMarqueeMessage ? new MarqueeMessage(user.TwitchUserName, message, user.Color) : null),
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
                ScriptingUser user = ScriptingUser.FromDB(raider);

                NotificationData notificationData = await raidScript.ExecuteFunctionAsync<NotificationData>(
                    "GetNotificationData", 2_000, raidRuntimeContext, user, count);

                if (!string.IsNullOrWhiteSpace(notificationData.ChatMessage))
                {
                    communication.SendPublicChatMessage(notificationData.ChatMessage);
                }

                if (!notificationData.ShowNotification)
                {
                    return;
                }

                if (string.IsNullOrEmpty(notificationData.Image))
                {
                    notificationData.Image = notificationServer.GetNextImageURL()!;
                }

                activityDispatcher.QueueActivity(
                    activity: new ScriptedActivityRequest(
                        activityHandler: this,
                        description: $"Raid: {raider.TwitchUserName} with {count} viewers",
                        notificationMessage: new ImageNotificationMessage(
                            image: notificationData.Image,
                            duration: 10_000,
                            message: TransformImageText(notificationData.ImageText)),
                        audioRequest: await TransformNotificationAudio(notificationData.Audio),
                        marqueeMessage: null),
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
                ScriptingUser notificationSender = ScriptingUser.FromDB(sender);
                ScriptingUser notificationRecipient = ScriptingUser.FromDB(recipient);

                NotificationGiftSub notificationGiftSub = new NotificationGiftSub(tier, months);

                NotificationData notificationData = await giftSubScript.ExecuteFunctionAsync<NotificationData>(
                    "GetNotificationData", 2_000, giftSubRuntimeContext, sender, recipient, notificationGiftSub);

                if (!string.IsNullOrWhiteSpace(notificationData.ChatMessage))
                {
                    communication.SendPublicChatMessage(notificationData.ChatMessage);
                }

                if (!notificationData.ShowNotification)
                {
                    return;
                }

                if (string.IsNullOrEmpty(notificationData.Image))
                {
                    notificationData.Image = notificationServer.GetNextImageURL()!;
                }

                activityDispatcher.QueueActivity(
                    activity: new ScriptedActivityRequest(
                        activityHandler: this,
                        description: $"Gift Sub From {sender.TwitchUserName} To {recipient.TwitchUserName}",
                        notificationMessage: new ImageNotificationMessage(
                            image: notificationData.Image,
                            duration: 5_000,
                            message: TransformImageText(notificationData.ImageText)),
                        audioRequest: await TransformNotificationAudio(notificationData.Audio),
                        marqueeMessage: null),
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
                ScriptingUser notificationRecipient = ScriptingUser.FromDB(recipient);

                NotificationGiftSub notificationGiftSub = new NotificationGiftSub(tier, months);

                NotificationData notificationData = await giftSubScript.ExecuteFunctionAsync<NotificationData>(
                    "GetAnonNotificationData", 2_000, giftSubRuntimeContext, recipient, notificationGiftSub);

                if (!string.IsNullOrWhiteSpace(notificationData.ChatMessage))
                {
                    communication.SendPublicChatMessage(notificationData.ChatMessage);
                }

                if (!notificationData.ShowNotification)
                {
                    return;
                }

                if (string.IsNullOrEmpty(notificationData.Image))
                {
                    notificationData.Image = notificationServer.GetNextImageURL()!;
                }

                activityDispatcher.QueueActivity(
                    activity: new ScriptedActivityRequest(
                        activityHandler: this,
                        description: $"Gift Sub From Anon To {recipient.TwitchUserName}",
                        notificationMessage: new ImageNotificationMessage(
                            image: notificationData.Image,
                            duration: 5_000,
                            message: TransformImageText(notificationData.ImageText)),
                        audioRequest: await TransformNotificationAudio(notificationData.Audio),
                        marqueeMessage: null),
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
                ScriptingUser notificationFollower = ScriptingUser.FromDB(follower);

                NotificationData notificationData = await followScript.ExecuteFunctionAsync<NotificationData>(
                    "GetNotificationData", 2_000, followRuntimeContext, notificationFollower);

                if (!string.IsNullOrWhiteSpace(notificationData.ChatMessage))
                {
                    communication.SendPublicChatMessage(notificationData.ChatMessage);
                }

                if (!notificationData.ShowNotification)
                {
                    return;
                }

                if (string.IsNullOrEmpty(notificationData.Image))
                {
                    notificationData.Image = notificationServer.GetNextImageURL()!;
                }

                activityDispatcher.QueueActivity(
                    activity: new ScriptedActivityRequest(
                        activityHandler: this,
                        description: $"Follower: {follower.TwitchUserName}",
                        notificationMessage: new ImageNotificationMessage(
                            image: notificationData.Image,
                            duration: 4_000,
                            message: TransformImageText(notificationData.ImageText)),
                        audioRequest: await TransformNotificationAudio(notificationData.Audio),
                        marqueeMessage: null),
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
                marqueeMessage: new MarqueeMessage(user.TwitchUserName, message, user.Color)),
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
                        voicePreference: tts.TTSVoice.TranslateTTSVoice(),
                        pitchPreference: tts.TTSPitch.TranslateTTSPitch(),
                        speedPreference: tts.TTSSpeed.TranslateTTSSpeed(),
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

    public class ScriptedActivityRequest : ActivityRequest
    {
        public NotificationMessage? NotificationMessage { get; }
        public Audio.AudioRequest? AudioRequest { get; }
        public MarqueeMessage? MarqueeMessage { get; }

        public ScriptedActivityRequest(
            IActivityHandler activityHandler,
            string description,
            NotificationMessage? notificationMessage = null,
            Audio.AudioRequest? audioRequest = null,
            MarqueeMessage? marqueeMessage = null)
            : base(activityHandler, description)
        {
            NotificationMessage = notificationMessage;
            AudioRequest = audioRequest;
            MarqueeMessage = marqueeMessage;
        }
    }

    public class NotificationSub
    {
        [ScriptingAccess]
        public int Tier { get; set; } = 0;
        [ScriptingAccess]
        public int CumulativeMonths { get; set; } = 0;
        [ScriptingAccess]
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
        [ScriptingAccess]
        public int Quantity { get; set; } = 0;
        [ScriptingAccess]
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
        [ScriptingAccess]
        public int Tier { get; set; } = 0;
        [ScriptingAccess]
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
        [ScriptingAccess]
        public string SoundEffectName { get; set; } = "";

        public NotificationSoundEffect() { }

        public NotificationSoundEffect(string soundEffectName)
        {
            SoundEffectName = soundEffectName;
        }
    }

    public class NotificationPause : NotificationAudio
    {
        [ScriptingAccess]
        public int PauseDurationMS { get; set; } = 1000;

        public NotificationPause() { }

        public NotificationPause(int pauseDurationMS)
        {
            PauseDurationMS = pauseDurationMS;
        }
    }


    public class NotificationTTS : NotificationAudio
    {
        [ScriptingAccess]
        public string TTSVoice { get; set; } = "";

        [ScriptingAccess]
        public string TTSPitch { get; set; } = "";

        [ScriptingAccess]
        public string TTSSpeed { get; set; } = "";

        [ScriptingAccess]
        public string TTSEffect { get; set; } = "";

        [ScriptingAccess]
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
            TTSVoice = ttsVoice;
            TTSPitch = ttsPitch;
            TTSSpeed = ttsSpeed;
            TTSEffect = ttsEffect;

            Message = message;
        }

    }

    public class NotificationText
    {
        [ScriptingAccess]
        public string Text { get; set; } = "";
        [ScriptingAccess]
        public string Color { get; set; } = "";

        public NotificationText() { }

        public NotificationText(string text, string color)
        {
            Text = text;
            Color = color;
        }
    }

    public class NotificationData
    {
        [ScriptingAccess]
        public bool ShowNotification { get; set; } = true;

        [ScriptingAccess]
        public string ChatMessage { get; set; } = "";

        [ScriptingAccess]
        public string Image { get; set; } = "";

        [ScriptingAccess]
        public List<NotificationText> ImageText { get; set; } = new List<NotificationText>();

        [ScriptingAccess]
        public List<NotificationAudio> Audio { get; set; } = new List<NotificationAudio>();

        [ScriptingAccess]
        public bool ShowMarqueeMessage { get; set; } = false;

        public NotificationData() { }

        public NotificationData(
            string chatMessage,
            bool showNotification,
            string image,
            List<NotificationText> imageText,
            List<NotificationAudio> audio,
            bool showMarqueeMessage)
        {
            ChatMessage = chatMessage;
            ShowNotification = showNotification;
            Image = image;
            ImageText = imageText;
            Audio = audio;
            ShowMarqueeMessage = showMarqueeMessage;
        }
    }
}
