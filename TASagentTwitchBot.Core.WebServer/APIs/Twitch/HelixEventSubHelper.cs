using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using Microsoft.Extensions.Logging;
using RestSharp;

using TASagentTwitchBot.Core.API.Twitch;
using TASagentTwitchBot.Core.Web.Extensions;

namespace TASagentTwitchBot.Core.WebServer.API.Twitch
{
    public class HelixEventSubHelper
    {
        private readonly AppAccessTokenManager appAccessTokenManager;
        private readonly Config.WebServerConfig webServerConfig;
        private readonly ILogger<HelixEventSubHelper> logger;

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
        public async Task<TwitchSubscribeResponse> Subscribe(
            string subscriptionType,
            Condition condition,
            Transport transport)
        {
            RestClient restClient = new RestClient("https://api.twitch.tv/helix/eventsub/subscriptions");
            RestRequest request = new RestRequest(Method.POST);
            request.AddHeader("Client-ID", webServerConfig.TwitchClientId);
            request.AddHeader("Authorization", $"Bearer {await appAccessTokenManager.GetAppAccessToken()}");

            request.AddJsonBody(JsonSerializer.Serialize(new TwitchSubscribeRequest(
                SubscriptionType: subscriptionType,
                Version: "1",
                Condition: condition,
                Transport: transport)));

            IRestResponse response = await restClient.ExecuteAsync(request);

            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                logger.LogWarning($"Bad response to Subscribe request: {response.StatusCode} - {response.Content}");
                return null;
            }

            return JsonSerializer.Deserialize<TwitchSubscribeResponse>(response.Content);
        }

        /// <summary>
        /// Delete the EventSub Webhook
        /// </summary>
        public async Task<TwitchDeleteSubscriptionResponse> DeleteSubscription(
            string Id)
        {
            RestClient restClient = new RestClient("https://api.twitch.tv/helix/eventsub/subscriptions");
            RestRequest request = new RestRequest(Method.DELETE);
            request.AddHeader("Client-ID", webServerConfig.TwitchClientId);
            request.AddHeader("Authorization", $"Bearer {await appAccessTokenManager.GetAppAccessToken()}");
            request.AddQueryParameter("id", Id);

            IRestResponse response = await restClient.ExecuteAsync(request);

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
        public async Task<TwitchGetSubscriptionsResponse> GetSubscriptions(
            string status = null,
            string subscriptionType = null,
            string after = null)
        {
            RestClient restClient = new RestClient("https://api.twitch.tv/helix/eventsub/subscriptions");
            RestRequest request = new RestRequest(Method.GET);
            request.AddHeader("Client-ID", webServerConfig.TwitchClientId);
            request.AddHeader("Authorization", $"Bearer {await appAccessTokenManager.GetAppAccessToken()}");

            request.AddOptionalParameter("status", status);
            request.AddOptionalParameter("type", subscriptionType);
            request.AddOptionalParameter("after", after);

            IRestResponse response = await restClient.ExecuteAsync(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogWarning($"Bad response to GetSubscriptions request: {response.Content}");
                return null;
            }

            return JsonSerializer.Deserialize<TwitchGetSubscriptionsResponse>(response.Content);
        }
    }
}
