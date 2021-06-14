using System;
using System.Text.Json;
using System.IO;
using TASagentTwitchBot.Core.Core;
using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Config
{
    public class BotConfiguration
    {
        private static string ConfigFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "Config.json");
        private static object _lock = new object();

        public string BotName { get; set; } = "";
        public string Broadcaster { get; set; } = "";
        public string BroadcasterId { get; set; } = "";

        public string TwitchClientId { get; set; } = "";
        public string TwitchClientSecret { get; set; } = "";

        public string BotAccessToken { get; set; } = "";
        public string BotRefreshToken { get; set; } = "";


        public string BroadcasterAccessToken { get; set; } = "";
        public string BroadcasterRefreshToken { get; set; } = "";

        public int TTSTimeoutTime { get; set; } = 20;

        public bool LogAllErrors { get; set; } = true;
        public bool ExhaustiveIRCLogging { get; set; } = true;

        public int BitTTSThreshold { get; set; } = 0;
        public bool EnableErrorHandling { get; set; } = true;

        //Output configuration
        public string EffectOutputDevice { get; set; } = "";
        public string VoiceOutputDevice { get; set; } = "";
        public string VoiceInputDevice { get; set; } = "";

        public MicConfiguration MicConfiguration { get; set; } = new MicConfiguration();
        public AuthConfiguration AuthConfiguration { get; set; } = new AuthConfiguration();

        public static BotConfiguration GetConfig()
        {
            BotConfiguration config;
            if (File.Exists(ConfigFilePath))
            {
                //Load existing config
                config = JsonSerializer.Deserialize<BotConfiguration>(File.ReadAllText(ConfigFilePath));
            }
            else
            {
                config = new BotConfiguration();
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

        public AuthDegree TryCredentials(string password, out string authString)
        {
            byte[] adminHashBytes = Cryptography.GetSaltFromPasswordString(Admin.Password);
            byte[] privHashBytes = Cryptography.GetSaltFromPasswordString(Privileged.Password);
            byte[] userHashBytes = Cryptography.GetSaltFromPasswordString(User.Password);

            var adminHash = Cryptography.HashPassword(password, adminHashBytes);
            var privHash = Cryptography.HashPassword(password, privHashBytes);
            var userHash = Cryptography.HashPassword(password, userHashBytes);


            if (adminHash == Admin.Password)
            {
                authString = Admin.AuthString;
                return AuthDegree.Admin;
            }
            else if (privHash == Privileged.Password)
            {
                authString = Privileged.AuthString;
                return AuthDegree.Privileged;
            }
            else if (userHash == User.Password)
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

        private static string GenerateAuthString()
        {
            using var provider = new System.Security.Cryptography.RNGCryptoServiceProvider();
            byte[] bytes = new byte[16];

            provider.GetBytes(bytes);
            return new Guid(bytes).ToString();
        }
    }

    public class CredentialSet
    {
        public string Password { get; set; } = "";
        public string AuthString { get; set; } = "";
    }

}
