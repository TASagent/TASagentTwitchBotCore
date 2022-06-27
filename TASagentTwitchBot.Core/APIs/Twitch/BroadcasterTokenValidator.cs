using System.Web;

using TASagentTwitchBot.Core.API.OAuth;

namespace TASagentTwitchBot.Core.API.Twitch;

[AutoRegister]
public interface IBroadcasterTokenValidator : ITokenValidator { }

public class BroadcasterTokenValidator : TokenValidator, IBroadcasterTokenValidator, IStartupListener
{
    private readonly Config.BotConfiguration botConfig;

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

    void IStartupListener.NotifyStartup() => RunValidator();
}
