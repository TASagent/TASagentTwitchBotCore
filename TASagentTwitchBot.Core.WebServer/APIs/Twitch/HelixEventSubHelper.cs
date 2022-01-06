using System.Text.Json;
using RestSharp;

using TASagentTwitchBot.Core.API.Twitch;
using TASagentTwitchBot.Core.Web.Extensions;

namespace TASagentTwitchBot.Core.WebServer.API.Twitch;

public class HelixEventSubHelper
{
    private readonly AppAccessTokenManager appAccessTokenManager;
    private readonly Config.WebServerConfig webServerConfig;
    private readonly ILogger<HelixEventSubHelper> logger;

    private static readonly Uri HelixURI = new Uri("https://api.twitch.tv/helix");

    public HelixEventSubHelper(
        Config.WebServerConfig webServerConfig,
        AppAccessTokenManager appAccessTokenManager,
        ILogger<HelixEventSubHelper> logger)
    {
        this.webServerConfig = webServerConfig;
        this.appAccessTokenManager = appAccessTokenManager;
        this.logger = logger;
    }

    /// <summary>
    /// Subscribe to EventSub Webhook
    /// </summary>
    public async Task<TwitchSubscribeResponse?> Subscribe(
        string subscriptionType,
        Condition condition,
        Transport transport)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("eventsub/subscriptions", Method.Post);
        request.AddHeader("Client-ID", webServerConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {await appAccessTokenManager.GetAppAccessToken()}");

        request.AddJsonBody(new TwitchSubscribeRequest(
            SubscriptionType: subscriptionType,
            Version: "1",
            Condition: condition,
            Transport: transport));

        RestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.Accepted)
        {
            logger.LogWarning("Bad response to Subscribe request: {StatusCode} - {Content}", response.StatusCode, response.Content);
            return null;
        }

        return JsonSerializer.Deserialize<TwitchSubscribeResponse>(response.Content!);
    }

    /// <summary>
    /// Delete the EventSub Webhook
    /// </summary>
    public async Task<TwitchDeleteSubscriptionResponse> DeleteSubscription(
        string Id)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("eventsub/subscriptions", Method.Delete);
        request.AddHeader("Client-ID", webServerConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {await appAccessTokenManager.GetAppAccessToken()}");
        request.AddQueryParameter("id", Id);

        RestResponse response = await restClient.ExecuteAsync(request);

        return response.StatusCode switch
        {
            HttpStatusCode.NoContent => TwitchDeleteSubscriptionResponse.Success,
            HttpStatusCode.NotFound => TwitchDeleteSubscriptionResponse.NotFound,
            HttpStatusCode.Unauthorized => TwitchDeleteSubscriptionResponse.AuthFailed,
            _ => TwitchDeleteSubscriptionResponse.MAX
        };
    }

    /// <summary>
    /// Get EventSub Webhooks
    /// </summary>
    public async Task<TwitchGetSubscriptionsResponse?> GetSubscriptions(
        string? status = null,
        string? subscriptionType = null,
        string? after = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("eventsub/subscriptions", Method.Get);
        request.AddHeader("Client-ID", webServerConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {await appAccessTokenManager.GetAppAccessToken()}");

        request.AddOptionalParameter("status", status);
        request.AddOptionalParameter("type", subscriptionType);
        request.AddOptionalParameter("after", after);

        RestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            logger.LogWarning("Bad response to GetSubscriptions request: {StatusCode} - {Content}", response.StatusCode, response.Content);
            return null;
        }

        return JsonSerializer.Deserialize<TwitchGetSubscriptionsResponse>(response.Content!);
    }
}
