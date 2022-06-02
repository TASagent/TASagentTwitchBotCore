using System.Text.Json;
using System.Security.Cryptography;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Config;

public class BotConfiguration
{
    private static string ConfigFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "Config.json");
    private static readonly object _lock = new object();

    public string BotName { get; set; } = "";
    public string Broadcaster { get; set; } = "";
    public string BroadcasterId { get; set; } = "";

    public string TwitchClientId { get; set; } = "";
    public string TwitchClientSecret { get; set; } = "";

    public string BotAccessToken { get; set; } = "";
    public string BotRefreshToken { get; set; } = "";
    public string BroadcasterAccessToken { get; set; } = "";
    public string BroadcasterRefreshToken { get; set; } = "";

    public bool LogChat { get; set; } = true;
    public bool LogAllErrors { get; set; } = true;
    public bool ExhaustiveIRCLogging { get; set; } = true;
    public bool ExhaustiveRedemptionLogging { get; set; } = true;

    //Output configuration
    public string EffectOutputDevice { get; set; } = "";
    public string VoiceOutputDevice { get; set; } = "";
    public string VoiceInputDevice { get; set; } = "";

    public bool UseThreadedMonitors { get; init; } = true;

    public MicConfiguration MicConfiguration { get; set; } = new MicConfiguration();
    public AuthConfiguration AuthConfiguration { get; set; } = new AuthConfiguration();

    public CommandConfiguration CommandConfiguration { get; set; } = new CommandConfiguration();

    public static BotConfiguration GetConfig(BotConfiguration? defaultConfig = null)
    {
        BotConfiguration config;
        if (File.Exists(ConfigFilePath))
        {
            //Load existing config
            config = JsonSerializer.Deserialize<BotConfiguration>(File.ReadAllText(ConfigFilePath))!;
        }
        else
        {
            config = defaultConfig ?? new BotConfiguration();
        }

        config.AuthConfiguration.RegenerateAuthStrings();

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
}

public class AuthConfiguration
{
    public bool PublicAuthAllowed { get; set; } = true;

    public CredentialSet Admin { get; set; } = new CredentialSet();
    public CredentialSet Privileged { get; set; } = new CredentialSet();
    public CredentialSet User { get; set; } = new CredentialSet();

    /// <summary>
    /// Checks the submitted password against the stored passwords to look for a match.
    /// </summary>
    /// <exception cref="FormatException"> Throws <see cref="FormatException"/> if the passwordHash or password is invalid </exception>
    /// <exception cref="Exception"> Throws <see cref="Exception"/> if an exception is encountered in the password validation process </exception>
    public AuthDegree TryCredentials(string password, out string authString)
    {
        if (password is null)
        {
            //At least sanitize against null passwords
            password = "";
        }

        if (Cryptography.ComparePassword(password, Admin.PasswordHash))
        {
            authString = Admin.AuthString;
            return AuthDegree.Admin;
        }
        else if (Cryptography.ComparePassword(password, Privileged.PasswordHash))
        {
            authString = Privileged.AuthString;
            return AuthDegree.Privileged;
        }
        else if (Cryptography.ComparePassword(password, User.PasswordHash))
        {
            authString = User.AuthString;
            return AuthDegree.User;
        }

        authString = "";
        return AuthDegree.None;
    }

    public AuthDegree CheckAuthString(string authString)
    {
        if (authString == Admin.AuthString)
        {
            return AuthDegree.Admin;
        }
        else if (authString == Privileged.AuthString)
        {
            return AuthDegree.Privileged;
        }
        else if (authString == User.AuthString)
        {
            return AuthDegree.User;
        }

        return AuthDegree.None;
    }

    public void RegenerateAuthStrings()
    {
        Admin.AuthString = GenerateAuthString();
        Privileged.AuthString = GenerateAuthString();
        User.AuthString = GenerateAuthString();
    }

    private static string GenerateAuthString() =>
        new Guid(RandomNumberGenerator.GetBytes(16)).ToString();
}

public class CredentialSet
{
    public string PasswordHash { get; set; } = "";
    public string AuthString { get; set; } = "";
}

public class CommandConfiguration
{
    public bool HelpEnabled { get; set; } = true;
    public bool ScopedEnabled { get; set; } = true;

    public bool GlobalErrorHandlingEnabled { get; set; } = true;

    public string GenericHelpMessage { get; set; } = "For more information, visit https://tas.wtf/info";
    public string UnknownCommandResponse { get; set; } = "You wot m8‽";
}
