using BGC.Scripting;
using System.Text.Json;
using TASagentTwitchBot.Core.Scripting;

namespace TASagentTwitchBot.Core.Notifications;

public partial class ScriptedActivityProvider
{
    public class ScriptedNotificationConfig
    {
        private static string ConfigFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "ScriptedNotificationConfig.json");
        private static readonly object _lock = new object();

        public bool SubNotificationEnabled { get; set; } = true;
        public bool CheerNotificationEnabled { get; set; } = true;
        public bool RaidNotificationEnabled { get; set; } = true;
        public bool GiftSubNotificationEnabled { get; set; } = true;
        public bool AnonGiftSubNotificationEnabled { get; set; } = true;
        public bool FollowNotificationEnabled { get; set; } = true;
        public bool TTSNotificationEnabled { get; set; } = true;


        public string SubNotificationScript { get; set; } = DEFAULT_SUB_SCRIPT;
        public string CheerNotificationScript { get; set; } = DEFAULT_CHEER_SCRIPT;
        public string RaidNotificationScript { get; set; } = DEFAULT_RAID_SCRIPT;
        public string GiftSubNotificationScript { get; set; } = DEFAULT_GIFTSUB_SCRIPT;
        public string FollowNotificationScript { get; set; } = DEFAULT_FOLLOW_SCRIPT;

        public static ScriptedNotificationConfig GetConfig(ScriptedNotificationConfig? defaultConfig = null)
        {
            ScriptedNotificationConfig config;
            if (File.Exists(ConfigFilePath))
            {
                //Load existing config
                config = JsonSerializer.Deserialize<ScriptedNotificationConfig>(File.ReadAllText(ConfigFilePath))!;
            }
            else
            {
                config = defaultConfig ?? new ScriptedNotificationConfig();
            }

            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config));

            return config;
        }

        public void Serialize()
        {
            lock (_lock)
            {
                File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(this));
            }
        }

        public void UpdateScript(ScriptType scriptType, string script)
        {
            switch (scriptType)
            {
                case ScriptType.Subscription:
                    SubNotificationScript = script;
                    break;

                case ScriptType.Cheer:
                    CheerNotificationScript = script;
                    break;

                case ScriptType.Raid:
                    RaidNotificationScript = script;
                    break;

                case ScriptType.GiftSub:
                    GiftSubNotificationScript = script;
                    break;

                case ScriptType.Follow:
                    FollowNotificationScript = script;
                    break;

                default:
                    throw new NotImplementedException($"ScriptType not implemented: {scriptType}");
            }
        }

        public string GetScript(ScriptType scriptType) => scriptType switch
        {
            ScriptType.Subscription => SubNotificationScript,
            ScriptType.Cheer => CheerNotificationScript,
            ScriptType.Raid => RaidNotificationScript,
            ScriptType.GiftSub => GiftSubNotificationScript,
            ScriptType.Follow => FollowNotificationScript,
            _ => throw new NotImplementedException($"ScriptType not implemented: {scriptType}")
        };

        public static string GetDefaultScript(ScriptType scriptType) => scriptType switch
        {
            ScriptType.Subscription => DEFAULT_SUB_SCRIPT,
            ScriptType.Cheer => DEFAULT_CHEER_SCRIPT,
            ScriptType.Raid => DEFAULT_RAID_SCRIPT,
            ScriptType.GiftSub => DEFAULT_GIFTSUB_SCRIPT,
            ScriptType.Follow => DEFAULT_FOLLOW_SCRIPT,
            _ => throw new NotImplementedException($"ScriptType not implemented: {scriptType}")
        };

        public static FunctionSignature[] GetRequiredFunctions(ScriptType scriptType) => scriptType switch
        {
            ScriptType.Subscription => subscriptionFunctions,
            ScriptType.Cheer => cheerFunctions,
            ScriptType.Raid => raidFunctions,
            ScriptType.GiftSub => giftSubFunctions,
            ScriptType.Follow => followFunctions,
            _ => throw new NotImplementedException($"ScriptType not implemented: {scriptType}"),
        };

        private static readonly FunctionSignature[] subscriptionFunctions = new FunctionSignature[] {
            new FunctionSignature("GetAlertData", typeof(IAlert),
                new ArgumentData("user", typeof(ScriptingUser)),
                new ArgumentData("sub", typeof(NotificationSub)))};

        private static readonly FunctionSignature[] cheerFunctions = new FunctionSignature[] {
            new FunctionSignature("GetAlertData", typeof(IAlert),
                new ArgumentData("user", typeof(ScriptingUser)),
                new ArgumentData("cheer", typeof(NotificationCheer)))};

        private static readonly FunctionSignature[] raidFunctions = new FunctionSignature[] {
            new FunctionSignature("GetAlertData", typeof(IAlert),
                new ArgumentData("user", typeof(ScriptingUser)),
                new ArgumentData("raiders", typeof(int)))};

        private static readonly FunctionSignature[] giftSubFunctions = new FunctionSignature[] {
            new FunctionSignature("GetAlertData", typeof(IAlert),
                new ArgumentData("sender", typeof(ScriptingUser)),
                new ArgumentData("recipient", typeof(ScriptingUser)),
                new ArgumentData("sub", typeof(NotificationGiftSub))),
            new FunctionSignature("GetAnonAlertData", typeof(IAlert),
                new ArgumentData("recipient", typeof(ScriptingUser)),
                new ArgumentData("sub", typeof(NotificationGiftSub)))};

        private static readonly FunctionSignature[] followFunctions = new FunctionSignature[] {
            new FunctionSignature("GetAlertData", typeof(IAlert),
                new ArgumentData("follower", typeof(ScriptingUser)))};

        private const string DEFAULT_SUB_SCRIPT =
@"//Default Sub Script
extern ICommunication communication;

IAlert GetAlertData(User user, Sub sub)
{
    StandardAlert alertData = new StandardAlert();

    communication.SendPublicChatMessage($""How Cow! Thanks for the {GetTierText(sub.Tier)} sub, @{user.TwitchUserName}{GetChatMonthText(sub.CumulativeMonths)}!"");

    //Show image for 5 seconds
    alertData.Duration = 5.0;

    alertData.ImageText.Add(new Text(GetImageTextIntro(sub), ""#FFFFFF""));
    alertData.ImageText.Add(new Text(user));
    alertData.ImageText.Add(new Text(""!"", ""#FFFFFF""));


    alertData.Audio.Add(new SoundEffect(""SMW PowerUp""));
    alertData.Audio.Add(new Pause(300));
    alertData.Audio.Add(new TTS(""Brian"", ""Medium"", ""Medium"", """", $""{user.TwitchUserName} has subbed {GetTTSMonthText(sub.CumulativeMonths)}""));
    alertData.Audio.Add(new Pause(300));
    
    if (!string.IsNullOrEmpty(sub.Message))
    {
        alertData.Audio.Add(new TTS(user, sub.Message));

        alertData.MarqueText.Add(new Text(user));
        alertData.MarqueText.Add(new Text($"": {sub.Message}"", ""#FFFFFF""));
    }

    return alertData;
}


//Helper Functions

string GetTierText(int tier)
{
    switch (tier)
    {
        case 0: return ""Twitch Prime "";
        case 1: return """";
        case 2: return ""Tier 2 "";
        case 3: return ""Tier 3 "";
    }
}

string GetImageTextIntro(Sub sub)
{
    if (sub.CumulativeMonths <= 1)
    {
        switch (sub.Tier)
        {
            case 0: return ""Thank you for the brand new Prime Gaming Sub, "";
            case 1: return ""Thank you for the brand new Sub, "";
            case 2: return ""Thank you for the brand new Tier 2 Sub, "";
            case 3: return ""Thank you for the brand new Tier 3 Sub, "";
        }
    }
    else
    {
        switch (sub.Tier)
        {
            case 0: return $""Thank you for subscribing for {sub.CumulativeMonths} months with Prime Gaming, "";
            case 1: return $""Thank you for subscribing for {sub.CumulativeMonths} months, "";
            case 2: return $""Thank you for subscribing at Tier 2 for {sub.CumulativeMonths} months, "";
            case 3: return $""Thank you for subscribing at Tier 3 for {sub.CumulativeMonths} months, "";
        }
    }

    return ""Thank you, "";
}

string GetChatMonthText(int cumulativeMonths) => (cumulativeMonths <= 1) ? """" : ($"", and for {cumulativeMonths} months"");

string GetTTSMonthText(int cumulativeMonths) => (cumulativeMonths <= 1) ? """" : ($"" for {cumulativeMonths} months"");";


        private const string DEFAULT_CHEER_SCRIPT =
@"//Default Cheer Script
extern ICommunication communication;

IAlert GetAlertData(User user, Cheer cheer)
{
    StandardAlert alertData = new StandardAlert();

    //Show image for 10 seconds
    alertData.Duration = 10.0;

    alertData.ImageText.Add(new Text(user));
    alertData.ImageText.Add(new Text($"" has cheered {cheer.Quantity} {(cheer.Quantity == 1 ? "" bit: "" : "" bits: "")} {cheer.Message}"", ""#FFFFFF""));

    alertData.Audio.Add(new SoundEffect(""FF7 Purchase""));
    alertData.Audio.Add(new Pause(500));
    
    if (!string.IsNullOrEmpty(cheer.Message) && cheer.MeetsTTSThreshold)
    {
        alertData.Audio.Add(new TTS(user, cheer.Message));

        alertData.MarqueText.Add(new Text(user));
        alertData.MarqueText.Add(new Text($"": {cheer.Message}"", ""#FFFFFF""));
    }

    return alertData;
}";



        private const string DEFAULT_RAID_SCRIPT =
@"//Default Raid Script
extern ICommunication communication;

IAlert GetAlertData(User user, int raiders)
{
    StandardAlert alertData = new StandardAlert();

    //Send Chat Message
    communication.SendPublicChatMessage($""Wow! {user.TwitchUserName} has Raided with {raiders} viewers! PogChamp"");

    //Show image for 10 seconds
    alertData.Duration = 10.0;

    alertData.ImageText.Add(new Text($""WOW! {(raiders >= 5 ? ($""{raiders} raiders"") : (""raiders""))} incoming from "", ""#FFFFFF""));
    alertData.ImageText.Add(new Text(user));
    alertData.ImageText.Add(new Text(""!"", ""#FFFFFF""));

    alertData.Audio.Add(new SoundEffect(""SMW CastleClear""));

    return alertData;
}";

        private const string DEFAULT_GIFTSUB_SCRIPT =
@"//Default GiftSub Script
extern ICommunication communication;

IAlert GetAlertData(User sender, User recipient, GiftSub sub)
{
    StandardAlert alertData = new StandardAlert();

    //Show image for 5 seconds
    alertData.Duration = 5.0;

    alertData.ImageText.Add(new Text(""Thank you, "", ""#FFFFFF""));
    alertData.ImageText.Add(new Text(sender));
    alertData.ImageText.Add(new Text($"" for gifting {GetMessageMiddleSegment(sub)} to "", ""#FFFFFF""));
    alertData.ImageText.Add(new Text(recipient));
    alertData.ImageText.Add(new Text(""!"", ""#FFFFFF""));

    alertData.Audio.Add(new SoundEffect(""SMW PowerUp""));
    alertData.Audio.Add(new Pause(500));

    return alertData;
}

IAlert GetAnonAlertData(User recipient, GiftSub sub)
{
    StandardAlert alertData = new StandardAlert();

    //Show image for 5 seconds
    alertData.Duration = 5.0;

    alertData.ImageText.Add(new Text(""Thank you, "", ""#FFFFFF""));
    alertData.ImageText.Add(new Text(""Anonymous"", ""#0000FF""));
    alertData.ImageText.Add(new Text($"" for gifting {GetMessageMiddleSegment(sub)} to "", ""#FFFFFF""));
    alertData.ImageText.Add(new Text(recipient));
    alertData.ImageText.Add(new Text(""!"", ""#FFFFFF""));

    alertData.Audio.Add(new SoundEffect(""SMW PowerUp""));
    alertData.Audio.Add(new Pause(500));
    
    return alertData;
}


string GetMessageMiddleSegment(GiftSub sub)
{
    if (sub.Months <= 1)
    {
        switch (sub.Tier)
        {
            case 0: return ""a sub"";
            case 1: return ""a sub"";
            case 2: return ""a tier 2 sub"";
            case 3: return ""a tier 3 sub"";
            default: return ""a sub"";
        }
    }
    else
    {
        switch (sub.Tier)
        {
            case 0: return $""{sub.Months} months"";
            case 1: return $""{sub.Months} months"";
            case 2: return $""{sub.Months} months of tier 2"";
            case 3: return $""{sub.Months} months of tier 3"";
            default: return $""{sub.Months} months"";
        }
    }
}";

        private const string DEFAULT_FOLLOW_SCRIPT =
@"//Default Follow Script
extern ICommunication communication;

//Used for preventing notifying on double-follow
HashSet<string> followedUserIds = new HashSet<string>();

IAlert GetAlertData(User follower)
{
    if (followedUserIds.Add(follower.TwitchUserId))
    {
        //Remove the references to the user below to make it anonymous
        communication.SendPublicChatMessage($""Thanks for following, @{follower.TwitchUserName}"");

        StandardAlert alertData = new StandardAlert();

        //Show image for 4 seconds
        alertData.Duration = 4.0;

        //Remove the reference to the user to make it anonymous
        alertData.ImageText.Add(new Text(""Thanks for following, "", ""#FFFFFF""));
        alertData.ImageText.Add(new Text(follower));
        alertData.ImageText.Add(new Text(""!"", ""#FFFFFF""));

        alertData.Audio.Add(new SoundEffect(""SMW MessageBlock""));

        return alertData;
    }

    return new NoAlert();
}";
    }
}
