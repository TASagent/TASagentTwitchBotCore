using System.Web;

using TASagentTwitchBot.Core.API.OAuth;

namespace TASagentTwitchBot.Core.API.Twitch;

[AutoRegister]
public interface IBotTokenValidator : ITokenValidator { }

public class BotTokenValidator : TokenValidator, IBotTokenValidator, IStartupListener
{
    private readonly Config.BotConfiguration botConfig;

    private readonly IReadOnlySet<string> scopes = new HashSet<string>
    {
            //"analytics:read:extensions",
            //"analytics:read:games",
            "bits:read",
            "channel:edit:commercial",
            "channel:manage:broadcast",
            //"channel:manage:extensions",
            "channel:moderate",
            //"channel:read:hype_train",
            //"channel:read:stream_key",
            "channel:read:subscriptions",
            "chat:edit",
            "chat:read",
            "clips:edit",
            "moderator:manage:shoutouts",
            "moderator:manage:announcements",
            "user:edit",
            "user:edit:follows",
            "user:read:broadcast",
            "moderator:read:followers",
            //"user:read:email",
            //"whispers:read",
            //"whispers:edit"
    };

    protected override string AccessToken
    {
        get => botConfig.BotAccessToken;
        set => botConfig.BotAccessToken = value;
    }

    protected override string RefreshToken
    {
        get => botConfig.BotRefreshToken;
        set => botConfig.BotRefreshToken = value;
    }

    protected override string RedirectURI => $"http://localhost:5000/TASagentBotAPI/OAuth/BotCode";

    public BotTokenValidator(
        Config.BotConfiguration botConfig,
        ApplicationManagement applicationManagement,
        ICommunication communication,
        HelixHelper helixHelper,
        ErrorHandler errorHandler)
        : base(applicationManagement, communication, helixHelper, errorHandler)
    {
        this.botConfig = botConfig;
    }

    protected override void SaveChanges() =>
        botConfig.Serialize();

    protected override void SendCodeRequest(string stateString)
    {

        string url = $"https://id.twitch.tv/oauth2/authorize" +
            $"?client_id={botConfig.TwitchClientId}" +
            $"&client_secret={botConfig.TwitchClientSecret}" +
            $"&redirect_uri={RedirectURI}" +
            $"&response_type=code" +
            $"&scope={string.Join('+', scopes)}" +
            $"&state={HttpUtility.UrlEncode(stateString)}";

        communication.SendDebugMessage($"Go to this url logged into Twitch as the Bot:\n\n{url}\n\n");
    }

    protected override bool ValidateScopes(List<string> receivedScopes) => scopes.IsSubsetOf(receivedScopes);

    void IStartupListener.NotifyStartup() => RunValidator();
}
