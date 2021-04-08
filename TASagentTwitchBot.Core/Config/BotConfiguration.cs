using System;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Config
{
    public class BotConfiguration
    {
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

        public bool ExhaustiveIRCLogging { get; set; } = true;

        public int BitTTSThreshold { get; set; } = 0;

        //Output configuration
        public string EffectOutputDevice { get; set; } = "";
        public string VoiceOutputDevice { get; set; } = "";
        public string VoiceInputDevice { get; set; } = "";

        public MicConfiguration MicConfiguration { get; set; } = new MicConfiguration();
        public AuthConfiguration AuthConfiguration { get; set; } = new AuthConfiguration();
    }

    public class AuthConfiguration
    {
        public bool PublicAuthAllowed { get; set; } = true;

        public CredentialSet Admin { get; set; } = new CredentialSet();
        public CredentialSet Privileged { get; set; } = new CredentialSet();
        public CredentialSet User { get; set; } = new CredentialSet();

        public AuthDegree TryCredentials(string password, out string authString)
        {
            if (password == Admin.Password)
            {
                authString = Admin.AuthString;
                return AuthDegree.Admin;
            }
            else if (password == Privileged.Password)
            {
                authString = Privileged.AuthString;
                return AuthDegree.Privileged;
            }
            else if (password == User.Password)
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
