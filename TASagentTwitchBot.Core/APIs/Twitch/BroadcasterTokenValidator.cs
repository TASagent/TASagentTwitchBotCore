using System.Threading.Tasks;
using System.Web;

namespace TASagentTwitchBot.Core.API.Twitch
{
    public interface IBroadcasterTokenValidator : ITokenValidator { }

    public class BroadcasterTokenValidator : TokenValidator, IBroadcasterTokenValidator
    {
        private readonly Config.IBotConfigContainer botConfigContainer;
        private readonly Config.IExternalWebAccessConfiguration webAccessConfig;

        protected override string AccessToken
        {
            get => botConfigContainer.BotConfig.BroadcasterAccessToken;
            set => botConfigContainer.BotConfig.BroadcasterAccessToken = value;
        }

        protected override string RefreshToken
        {
            get => botConfigContainer.BotConfig.BroadcasterRefreshToken;
            set => botConfigContainer.BotConfig.BroadcasterRefreshToken = value;
        }

        protected override string RedirectURI => $"{webAccessConfig.GetLocalAddress()}/TASagentBotAPI/OAuth/BroadcasterCode";

        public BroadcasterTokenValidator(
            ICommunication communication,
            HelixHelper helixHelper,
            Config.IBotConfigContainer botConfigContainer,
            Config.IExternalWebAccessConfiguration webAccessConfig)
            : base(communication, helixHelper)
        {
            this.botConfigContainer = botConfigContainer;
            this.webAccessConfig = webAccessConfig;
        }

        protected override void SaveChanges() =>
            botConfigContainer.SerializeData();

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
                $"?client_id={botConfigContainer.BotConfig.TwitchClientId}" +
                $"&client_secret={botConfigContainer.BotConfig.TwitchClientSecret}" +
                $"&redirect_uri={RedirectURI}" +
                $"&response_type=code" +
                $"&scope={scopes.Replace(" ", "+")}" +
                $"&state={HttpUtility.UrlEncode(stateString)}";

            communication.SendDebugMessage($"Go to this url logged into Twitch as the Broadcaster:\n\n{url}\n\n");
        }
    }
}
