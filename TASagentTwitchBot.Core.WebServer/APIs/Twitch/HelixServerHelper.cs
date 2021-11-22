using System.Text.Json;
using RestSharp;

using TASagentTwitchBot.Core.API.OAuth;

namespace TASagentTwitchBot.Core.WebServer.API.Twitch;

public class HelixServerHelper
{
    private readonly Config.WebServerConfig webServerConfig;

    /// <summary>
    /// Constructor for the Twitch_Helix api helper
    /// </summary>
    public HelixServerHelper(
        Config.WebServerConfig webServerConfig)
    {
        this.webServerConfig = webServerConfig;
    }

    #region Authentication

    /// <summary>
    /// Gets new OAuth App Access token
    /// </summary>
    public async Task<TokenRequest?> GetAppAccessToken()
    {
        RestClient restClient = new RestClient("https://id.twitch.tv/oauth2/token");
        RestRequest request = new RestRequest(Method.POST);
        request.AddParameter("client_id", webServerConfig.TwitchClientId);
        request.AddParameter("client_secret", webServerConfig.TwitchClientSecret);
        request.AddParameter("grant_type", "client_credentials");

        IRestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        return JsonSerializer.Deserialize<TokenRequest>(response.Content);
    }

    /// <summary>
    /// Expire old OAuth Access token
    /// </summary>
    public async Task<bool> ExpireToken(
        string token)
    {
        RestClient restClient = new RestClient("https://id.twitch.tv/oauth2/revoke");
        RestRequest request = new RestRequest(Method.POST);
        request.AddParameter("client_id", webServerConfig.TwitchClientId);
        request.AddParameter("token", token);

        IRestResponse response = await restClient.ExecuteAsync(request);

        return response.StatusCode == HttpStatusCode.OK;
    }

    /// <summary>
    /// Validate an OAuth Access token
    /// </summary>
    public async Task<TokenValidationRequest?> ValidateToken(
        string accessToken)
    {
        RestClient restClient = new RestClient("https://id.twitch.tv/oauth2/validate");
        RestRequest request = new RestRequest(Method.GET);
        request.AddHeader("Authorization", $"OAuth {accessToken}");

        IRestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        return JsonSerializer.Deserialize<TokenValidationRequest>(response.Content);
    }

    #endregion Authentication

}
