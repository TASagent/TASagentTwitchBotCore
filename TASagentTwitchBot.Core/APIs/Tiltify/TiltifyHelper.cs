using System.Text.Json;
using RestSharp;

using TASagentTwitchBot.Core.API.OAuth;
using TASagentTwitchBot.Core.Web.Extensions;

namespace TASagentTwitchBot.Core.API.Tiltify;

public class TiltifyHelper : IOAuthHandler
{
    private readonly TiltifyConfiguration tiltifyConfig;
    private readonly ICommunication communication;

    public TiltifyHelper(
        TiltifyConfiguration tiltifyConfig,
        ICommunication communication)
    {
        this.tiltifyConfig = tiltifyConfig;
        this.communication = communication;
    }

    #region OAuth

    public async Task<TokenRequest?> GetToken(
        string authCode,
        string redirectURI)
    {
        RestClient restClient = new RestClient("https://tiltify.com/oauth/token");
        RestRequest request = new RestRequest(Method.POST);
        request.AddParameter("client_id", tiltifyConfig.ApplicationId);
        request.AddParameter("client_secret", tiltifyConfig.ApplicationSecret);
        request.AddParameter("code", authCode);
        request.AddParameter("grant_type", "authorization_code");
        request.AddParameter("redirect_uri", redirectURI);

        IRestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        return JsonSerializer.Deserialize<TokenRequest>(response.Content);
    }

    public async Task<TokenRefreshRequest?> RefreshToken(
        string refreshToken)
    {
        RestClient restClient = new RestClient("https://tiltify.com/oauth/token");
        RestRequest request = new RestRequest(Method.POST);
        request.AddParameter("grant_type", "refresh_token");
        request.AddParameter("refresh_token", refreshToken);
        request.AddParameter("client_id", tiltifyConfig.ApplicationId);
        request.AddParameter("client_secret", tiltifyConfig.ApplicationSecret);

        IRestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            communication.SendErrorMessage($"Failed Tiltify OAuth Refresh response:");
            communication.SendErrorMessage($"  {response.Content}");
            return null;
        }

        return JsonSerializer.Deserialize<TokenRefreshRequest>(response.Content);
    }

    public async Task<TokenValidationRequest?> ValidateToken(
        string accessToken)
    {
        RestClient restClient = new RestClient("https://tiltify.com/oauth/validate");
        RestRequest request = new RestRequest(Method.GET);
        request.AddHeader("Authorization", $"OAuth {accessToken}");

        IRestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            communication.SendWarningMessage($"Failed Tiltify OAuth AccessToken validation response: {response.Content}");
            return null;
        }

        return JsonSerializer.Deserialize<TokenValidationRequest>(response.Content);
    }

    public async Task<bool> ExpireToken(
        string token)
    {
        RestClient restClient = new RestClient("https://tiltify.com/oauth/revoke");
        RestRequest request = new RestRequest(Method.POST);
        request.AddParameter("client_id", tiltifyConfig.ApplicationId);
        request.AddParameter("token", token);

        IRestResponse response = await restClient.ExecuteAsync(request);

        return response.StatusCode == HttpStatusCode.OK;
    }

    #endregion OAuth
    #region Campaigns

    public async Task<CampaignRequest?> GetCampaign(
        int campaignId)
    {
        RestClient restClient = new RestClient($"https://tiltify.com/api/v3/campaigns/{campaignId}");
        RestRequest request = new RestRequest(Method.GET);
        request.AddHeader("Authorization", $"Bearer {tiltifyConfig.ApplicationAccessToken}");

        IRestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            communication.SendWarningMessage($"Failed Tiltify GetCampaign: {response.Content}");
            return null;
        }

        return JsonSerializer.Deserialize<CampaignRequest>(response.Content);
    }

    public async Task<CampaignDonationRequest?> GetCampaignDonations(
        int campaignId,
        int? afterIndex = null)
    {
        RestClient restClient = new RestClient($"https://tiltify.com/api/v3/campaigns/{campaignId}/donations");
        RestRequest request = new RestRequest(Method.GET);
        request.AddHeader("Authorization", $"Bearer {tiltifyConfig.ApplicationAccessToken}");

        request.AddOptionalParameter("after", afterIndex);

        IRestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            communication.SendWarningMessage($"Failed Tiltify GetCampaignDonations: {response.Content}");
            return null;
        }

        return JsonSerializer.Deserialize<CampaignDonationRequest>(response.Content);
    }

    #endregion Campaigns
}
