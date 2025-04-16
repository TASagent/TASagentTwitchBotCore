using System.Web;

using TASagentTwitchBot.Core.API.OAuth;

namespace TASagentTwitchBot.Core.API.Twitch;

[AutoRegister]
public interface IBroadcasterTokenValidator : ITokenValidator { }

public class BroadcasterTokenValidator : TokenValidator, IBroadcasterTokenValidator, IStartupListener
{
    private readonly Config.BotConfiguration botConfig;

    private readonly IReadOnlySet<string> scopes = new HashSet<string>
    {
        "channel:manage:broadcast",
        "channel:manage:extensions",
        "channel:manage:redemptions",
        "channel:moderate",
        "channel:read:redemptions",
        "channel:read:subscriptions",
        "moderator:read:followers"
    };

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

    protected override string RedirectURI => $"http://localhost:5000/TASagentBotAPI/OAuth/BroadcasterCode";

    public BroadcasterTokenValidator(
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

        communication.SendDebugMessage($"Go to this url logged into Twitch as the Broadcaster:\n\n{url}\n\n");
    }

    protected override bool ValidateScopes(List<string> receivedScopes) => scopes.IsSubsetOf(receivedScopes);

    void IStartupListener.NotifyStartup() => RunValidator();
}
