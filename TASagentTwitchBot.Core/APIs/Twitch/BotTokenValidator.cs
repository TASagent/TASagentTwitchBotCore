using System.Threading.Tasks;
using System.Web;

namespace TASagentTwitchBot.Core.API.Twitch
{
    public interface IBotTokenValidator : ITokenValidator { }

    public class BotTokenValidator : TokenValidator, IBotTokenValidator
    {
        private readonly Config.IBotConfigContainer botConfigContainer;
        private readonly Config.IExternalWebAccessConfiguration webAccessConfig;

        protected override string AccessToken
        {
            get => botConfigContainer.BotConfig.BotAccessToken;
            set => botConfigContainer.BotConfig.BotAccessToken = value;
        }

        protected override string RefreshToken
        {
            get => botConfigContainer.BotConfig.BotRefreshToken;
            set => botConfigContainer.BotConfig.BotRefreshToken = value;
        }

        protected override string RedirectURI => $"{webAccessConfig.GetLocalAddress()}/TASagentBotAPI/OAuth/BotCode";

        public BotTokenValidator(
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
                "analytics:read:extensions " +
                "analytics:read:games " +
                "bits:read " +
                "channel:edit:commercial " +
                "channel:manage:broadcast " +
                "channel:manage:extensions " +
                "channel:moderate " +
                "channel:read:hype_train " +
                "channel:read:stream_key " +
                "channel:read:subscriptions " +
                "chat:edit " +
                "chat:read " +
                "clips:edit " +
                "user:edit " +
                "user:edit:follows " +
                "user:read:broadcast " +
                "user:read:email " +
                "whispers:read " +
                "whispers:edit";

            string url = $"https://id.twitch.tv/oauth2/authorize" +
                $"?client_id={botConfigContainer.BotConfig.TwitchClientId}" +
                $"&client_secret={botConfigContainer.BotConfig.TwitchClientSecret}" +
                $"&redirect_uri={RedirectURI}" +
                $"&response_type=code" +
                $"&scope={scopes.Replace(" ", "+")}" +
                $"&state={HttpUtility.UrlEncode(stateString)}";

            communication.SendDebugMessage($"Go to this url logged into Twitch as the Bot:\n\n{url}\n\n");
        }
    }
}
