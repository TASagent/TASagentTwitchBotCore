using System.Text.Json;
using System.Text.Json.Serialization;

namespace TASagentTwitchBot.Core.TTS;

public class TTSConfiguration
{
    private static string ConfigFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "TTSConfig.json");
    private static readonly object _lock = new object();

    public string FeatureName { get; init; } = "Text-To-Speech";
    public string FeatureNameBrief { get; init; } = "TTS";

    public bool Enabled { get; set; } = true;

    public bool AllowNeuralVoices { get; set; } = false;
    public bool UseAWSPolly { get; set; } = true;
    public bool UseGoogleCloudTTS { get; set; } = true;
    public bool UseAzureSpeechSynthesis { get; set; } = false;

    public CommandConfiguration Command { get; init; } = new CommandConfiguration();
    public RedemptionConfiguration Redemption { get; init; } = new RedemptionConfiguration();

    public int BitThreshold { get; set; } = 0;

    public CommandRolePermissions ModPermissions { get; init; } = new CommandRolePermissions(Permission.Always, Permission.Always, true, true);
    public CommandRolePermissions ElevatedPermissions { get; init; } = new CommandRolePermissions(Permission.Always, Permission.Always, true, true);
    public CommandRolePermissions RiffRaffPermissions { get; init; } = new CommandRolePermissions(Permission.WithApproval, Permission.Always, true, false);

    public enum Permission
    {
        Never = 0,
        WithApproval,
        Always,
        MAX
    }

    public static TTSConfiguration GetConfig()
    {
        if (File.Exists(ConfigFilePath))
        {
            //Load existing config

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            return JsonSerializer.Deserialize<TTSConfiguration>(File.ReadAllText(ConfigFilePath), options) ?? new TTSConfiguration();
        }
        else
        {
            TTSConfiguration config = new TTSConfiguration();
            config.Serialize();
            return config;
        }
    }

    public void Serialize()
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        lock (_lock)
        {
            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(this, options));
        }
    }

    public void UpdateRedemptionID(string newID)
    {
        Redemption.RedemptionID = newID;
        Serialize();
    }

    public bool CanUseNeuralVoice(Commands.AuthorizationLevel authorizationLevel)
    {
        if (!AllowNeuralVoices)
        {
            return false;
        }

        switch (authorizationLevel)
        {
            case Commands.AuthorizationLevel.Restricted: return false;
            case Commands.AuthorizationLevel.None: return RiffRaffPermissions.AllowNeuralVoices;
            case Commands.AuthorizationLevel.Elevated: return ElevatedPermissions.AllowNeuralVoices;
            case Commands.AuthorizationLevel.Moderator: return ModPermissions.AllowNeuralVoices;
            case Commands.AuthorizationLevel.Admin: return true;

            default:
                BGC.Debug.LogError($"Unsupported AuthorizationLevel: {authorizationLevel}");
                return false;
        }
    }

    public bool CanCustomize(Commands.AuthorizationLevel authorizationLevel)
    {
        switch (authorizationLevel)
        {
            case Commands.AuthorizationLevel.Restricted: return false;
            case Commands.AuthorizationLevel.None: return RiffRaffPermissions.CanCustomize;
            case Commands.AuthorizationLevel.Elevated: return ElevatedPermissions.CanCustomize;
            case Commands.AuthorizationLevel.Moderator: return ModPermissions.CanCustomize;
            case Commands.AuthorizationLevel.Admin: return true;

            default:
                BGC.Debug.LogError($"Unsupported AuthorizationLevel: {authorizationLevel}");
                return false;
        }
    }

    public bool CanUseCommand(Commands.AuthorizationLevel authorizationLevel)
    {
        switch (authorizationLevel)
        {
            case Commands.AuthorizationLevel.Restricted: return false;
            case Commands.AuthorizationLevel.None: return RiffRaffPermissions.CanUseCommand == Permission.Always || RiffRaffPermissions.CanUseCommand == Permission.WithApproval;
            case Commands.AuthorizationLevel.Elevated: return ElevatedPermissions.CanUseCommand == Permission.Always || ElevatedPermissions.CanUseCommand == Permission.WithApproval;
            case Commands.AuthorizationLevel.Moderator: return ModPermissions.CanUseCommand == Permission.Always || ModPermissions.CanUseCommand == Permission.WithApproval;
            case Commands.AuthorizationLevel.Admin: return true;

            default:
                BGC.Debug.LogError($"Unsupported AuthorizationLevel: {authorizationLevel}");
                return false;
        }
    }

    public bool HasCommandApproval(Commands.AuthorizationLevel authorizationLevel)
    {
        switch (authorizationLevel)
        {
            case Commands.AuthorizationLevel.Restricted: return false;
            case Commands.AuthorizationLevel.None: return RiffRaffPermissions.CanUseCommand == Permission.Always;
            case Commands.AuthorizationLevel.Elevated: return ElevatedPermissions.CanUseCommand == Permission.Always;
            case Commands.AuthorizationLevel.Moderator: return ModPermissions.CanUseCommand == Permission.Always;
            case Commands.AuthorizationLevel.Admin: return true;

            default:
                BGC.Debug.LogError($"Unsupported AuthorizationLevel: {authorizationLevel}");
                return false;
        }
    }

    public bool IsServiceSupported(TTSService service)
    {
        switch (service)
        {
            case TTSService.Amazon: return UseAWSPolly;
            case TTSService.Google: return UseGoogleCloudTTS;
            case TTSService.Azure: return UseAzureSpeechSynthesis;

            default:
                BGC.Debug.LogError($"Unsupported TTSService: {service}");
                return false;
        }
    }

    public TTSService GetASupportedService()
    {
        if (UseAWSPolly)
        {
            return TTSService.Amazon;
        }

        if (UseGoogleCloudTTS)
        {
            return TTSService.Google;
        }

        if (UseAzureSpeechSynthesis)
        {
            return TTSService.Azure;
        }

        return TTSService.MAX;
    }

    public class CommandRolePermissions
    {
        public Permission CanUseCommand { get; set; } = Permission.Never;
        public Permission CanUseRedemption { get; set; } = Permission.Never;
        public bool CanCustomize { get; set; } = true;
        public bool AllowNeuralVoices { get; set; } = false;

        public CommandRolePermissions() { }

        public CommandRolePermissions(
            Permission canUseCommand,
            Permission canUseRedemption,
            bool canCustomize,
            bool allowNeuralVoices)
        {
            CanUseCommand = canUseCommand;
            CanUseRedemption = canUseRedemption;
            CanCustomize = canCustomize;
            AllowNeuralVoices = allowNeuralVoices;
        }
    }

    public class CommandConfiguration
    {
        public bool Enabled { get; set; } = true;
        public string CommandName { get; init; } = "tts";
        public int CooldownTime { get; set; } = 20;
        public bool ModsIgnoreCooldown { get; set; } = true;
    }

    public class RedemptionConfiguration
    {
        public bool Enabled { get; set; } = false;
        public string RedemptionID { get; set; } = "";
        public string Name { get; init; } = "TTS";
        public string Description { get; init; } = "Send a message using the TTS System.";
        public string BackgroundColor { get; init; } = "#56BDE6";
        public int Cost { get; init; } = 500;
    }
}
