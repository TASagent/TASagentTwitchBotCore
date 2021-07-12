using System;
using System.Web;

using TASagentTwitchBot.Core.API.OAuth;

namespace TASagentTwitchBot.Core.API.Twitch
{
    public interface IBroadcasterTokenValidator : ITokenValidator { }

    public class BroadcasterTokenValidator : TokenValidator, IBroadcasterTokenValidator
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly Config.IExternalWebAccessConfiguration webAccessConfig;

        protected override string AccessToken
        {
            get => botConfig.BroadcasterAccessToken;
            set => botConfig.BroadcasterAccessToken = value;
        }

        protected override string RefreshToken
        {
            get => botConfig.BroadcasterRefreshToken;
            set => botConfig.BroadcasterRefreshToken = value;
        }

        protected override string RedirectURI => $"{webAccessConfig.GetLocalAddress()}/TASagentBotAPI/OAuth/BroadcasterCode";

        public BroadcasterTokenValidator(
            Config.BotConfiguration botConfig,
            ICommunication communication,
            HelixHelper helixHelper,
            Config.IExternalWebAccessConfiguration webAccessConfig)
            : base(communication, helixHelper)
        {
            this.botConfig = botConfig;
            this.webAccessConfig = webAccessConfig;
        }

        protected override void SaveChanges() =>
            botConfig.Serialize();

        protected override void SendCodeRequest(string stateString)
        {
            const string scopes =
                "channel:manage:broadcast " +
                "channel:manage:extensions " +
                "channel:manage:redemptions " +
                "channel:moderate " +
                "channel:read:redemptions " +
                "channel:read:subscriptions";

            string url = $"https://id.twitch.tv/oauth2/authorize" +
                $"?client_id={botConfig.TwitchClientId}" +
                $"&client_secret={botConfig.TwitchClientSecret}" +
                $"&redirect_uri={RedirectURI}" +
                $"&response_type=code" +
                $"&scope={scopes.Replace(" ", "+")}" +
                $"&state={HttpUtility.UrlEncode(stateString)}";

            communication.SendDebugMessage($"Go to this url logged into Twitch as the Broadcaster:\n\n{url}\n\n");
        }
    }
}
