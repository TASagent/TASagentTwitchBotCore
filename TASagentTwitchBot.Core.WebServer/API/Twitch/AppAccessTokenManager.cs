
using TASagentTwitchBot.Core.API.OAuth;

namespace TASagentTwitchBot.Core.WebServer.API.Twitch;

public class AppAccessTokenManager
{
    private readonly Config.WebServerConfig webServerConfig;
    private readonly HelixServerHelper helixServerHelper;
    private readonly ILogger<AppAccessTokenManager> logger;

    private DateTime nextValidation = DateTime.Now;
    private static readonly SemaphoreSlim validationSemaphore = new SemaphoreSlim(1);

    public AppAccessTokenManager(
        Config.WebServerConfig webServerConfig,
        HelixServerHelper helixServerHelper,
        ILogger<AppAccessTokenManager> logger)
    {
        this.webServerConfig = webServerConfig;
        this.helixServerHelper = helixServerHelper;
        this.logger = logger;
    }

    public async Task<string> GetAppAccessToken()
    {
        //Initial Check
        if (GetNeedsValidation())
        {
            //Threads queue up here
            await validationSemaphore.WaitAsync();
            try
            {
                //Make sure it needs validation
                if (GetNeedsValidation())
                {
                    await ValidateAndUpdate();
                }
            }
            finally
            {
                validationSemaphore.Release();
            }
        }

        return webServerConfig.AppAccessToken;
    }

    public void RequireRevalidation() => nextValidation = DateTime.Now;

    private bool GetNeedsValidation() => DateTime.Now >= nextValidation;

    private async Task ValidateAndUpdate()
    {
        if (!string.IsNullOrEmpty(webServerConfig.AppAccessToken))
        {
            //Have token
            TokenValidationRequest? validationRequest = await helixServerHelper.ValidateToken(webServerConfig.AppAccessToken);

            if (validationRequest is null || validationRequest.ExpiresIn < 60 * 60)
            {
                //App Access Token invalid
                webServerConfig.AppAccessToken = "";
            }
            else
            {
                //App Access Token good
                nextValidation = DateTime.Now + TimeSpan.FromHours(1.0);
                return;
            }
        }

        //Need new token
        TokenRequest? tokenRequest = await helixServerHelper.GetAppAccessToken();

        if (tokenRequest is null)
        {
            logger.LogError("Failed to get App Access token.");
            return;
        }

        nextValidation = DateTime.Now + TimeSpan.FromHours(1.0);
        webServerConfig.AppAccessToken = tokenRequest.AccessToken;
        webServerConfig.Serialize();
    }
}
