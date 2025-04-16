/**
 *    Copyright 2019 Amazon.com, Inc. or its affiliates
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */

using System.Text.Json;
using Microsoft.Extensions.Logging;
using RestSharp;

using TASagentTwitchBot.Core.API.OAuth;
using TASagentTwitchBot.Core.Web.Extensions;

namespace TASagentTwitchBot.Core.API.Twitch;

public class HelixHelper : IOAuthHandler
{
    private readonly Config.BotConfiguration botConfig;
    private readonly ICommunication communication;

    private static readonly Uri OAuthURI = new Uri("https://id.twitch.tv/oauth2");
    private static readonly Uri HelixURI = new Uri("https://api.twitch.tv/helix");

    /// <summary>
    /// Constructor for the Twitch_Helix api helper
    /// </summary>
    public HelixHelper(
        Config.BotConfiguration botConfig,
        ICommunication communication)
    {
        this.botConfig = botConfig;
        this.communication = communication;
    }

    #region Authentication

    /// <summary>
    /// Gets new OAuth Access token
    /// </summary>
    public async Task<TokenRequest?> GetToken(
        string authCode,
        string redirectURI)
    {
        RestClient restClient = new RestClient(OAuthURI);
        RestRequest request = new RestRequest("token", Method.Post);
        request.AddParameter("client_id", botConfig.TwitchClientId);
        request.AddParameter("client_secret", botConfig.TwitchClientSecret);
        request.AddParameter("code", authCode);
        request.AddParameter("grant_type", "authorization_code");
        request.AddParameter("redirect_uri", redirectURI);

        return Deserialize<TokenRequest>(await restClient.ExecuteAsync(request));
    }

    /// <summary>
    /// Gets new OAuth Access token
    /// </summary>
    public async Task<TokenRequest?> GetClientCredentialsToken()
    {

        RestClient restClient = new RestClient(OAuthURI);
        RestRequest request = new RestRequest("token", Method.Post);
        request.AddParameter("client_id", botConfig.TwitchClientId);
        request.AddParameter("client_secret", botConfig.TwitchClientSecret);
        request.AddParameter("grant_type", "client_credentials");

        return Deserialize<TokenRequest>(await restClient.ExecuteAsync(request));
    }

    /// <summary>
    /// Expire old OAuth Access token
    /// </summary>
    public async Task<bool> ExpireToken(
        string token)
    {
        RestClient restClient = new RestClient(OAuthURI);
        RestRequest request = new RestRequest("revoke", Method.Post);
        request.AddParameter("client_id", botConfig.TwitchClientId);
        request.AddParameter("token", token);

        RestResponse response = await restClient.ExecuteAsync(request);

        return response.StatusCode == HttpStatusCode.OK;
    }

    /// <summary>
    /// Refresh an OAuth Access token
    /// </summary>
    public async Task<TokenRefreshRequest?> RefreshToken(
        string refreshToken)
    {
        RestClient restClient = new RestClient(OAuthURI);
        RestRequest request = new RestRequest("token", Method.Post);
        request.AddParameter("grant_type", "refresh_token");
        request.AddParameter("refresh_token", refreshToken);
        request.AddParameter("client_id", botConfig.TwitchClientId);
        request.AddParameter("client_secret", botConfig.TwitchClientSecret);

        RestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            communication.SendErrorMessage($"Failed Twitch OAuth Refresh response:");
            communication.SendErrorMessage($"  {response.Content}");
            return null;
        }

        return JsonSerializer.Deserialize<TokenRefreshRequest>(response.Content!);
    }

    /// <summary>
    /// Validate an OAuth Access token
    /// </summary>
    public async Task<TokenValidationRequest?> ValidateToken(
        string accessToken)
    {
        RestClient restClient = new RestClient(OAuthURI);
        RestRequest request = new RestRequest("validate", Method.Get);
        request.AddHeader("Authorization", $"OAuth {accessToken}");

        return Deserialize<TokenValidationRequest>(await restClient.ExecuteAsync(request));
    }

    #endregion Authentication

    /// <summary>
    /// Gets game information as specified
    /// </summary>
    public async Task<TwitchGames?> GetGames(
        List<string>? gameIDs = null,
        List<string>? gameNames = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("games", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);

        request.AddOptionalParameter("id", gameIDs);
        request.AddOptionalParameter("name", gameNames);

        return Deserialize<TwitchGames>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets Top Games
    /// </summary>
    public async Task<TwitchGames?> GetTopGames(
        string? after = null,
        string? before = null,
        string? first = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("games/top", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);

        request.AddOptionalParameter("first", first);
        request.AddOptionalParameter("before", before);
        request.AddOptionalParameter("after", after);

        return Deserialize<TwitchGames>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets information about one specified Twitch users, identified by user ID.
    /// </summary>
    public async Task<TwitchUsers.Datum?> GetUserById(string id)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("users", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddOptionalParameter("id", new List<string>() { id });

        TwitchUsers? twitchUsers = Deserialize<TwitchUsers>(await restClient.ExecuteAsync(request));

        if ((twitchUsers?.Data?.Count ?? 0) == 0)
        {
            return null;
        }

        return twitchUsers!.Data[0];
    }

    /// <summary>
    /// Gets information about one specified Twitch users, identified by user Login Name.
    /// </summary>
    public async Task<TwitchUsers.Datum?> GetUserByLogin(string login)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("users", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddOptionalParameter("login", new List<string>() { login.ToLower() });

        TwitchUsers? twitchUsers = Deserialize<TwitchUsers>(await restClient.ExecuteAsync(request));

        if ((twitchUsers?.Data?.Count ?? 0) == 0)
        {
            return null;
        }

        return twitchUsers!.Data[0];
    }


    /// <summary>
    /// Gets information about one or more specified Twitch users. Users are identified by optional user IDs and/or login name.
    /// If neither a user ID nor a login name is specified, the user is looked up by Bearer token.
    /// </summary>
    public async Task<TwitchUsers?> GetUsers(
        List<string>? ids = null,
        List<string>? logins = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("users", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddOptionalParameter("id", ids);
        request.AddOptionalParameter("login", logins);

        return Deserialize<TwitchUsers>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets stream tags for a specified broadcaster
    /// </summary>
    public async Task<TwitchStreamTags?> GetStreamTags(string broadcasterId)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("streams/tags", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);

        request.AddParameter("broadcaster_id", broadcasterId);

        return Deserialize<TwitchStreamTags>(await restClient.ExecuteAsync(request), x => x.Replace('-', '_'));
    }


    /// <summary>
    /// Gets all stream tags
    /// </summary>
    public async Task<TwitchStreamTags?> GetAllStreamTags(
        string? first = null,
        string? after = null,
        List<string>? tagIDs = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("tags/streams", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);

        request.AddOptionalParameter("first", first);
        request.AddOptionalParameter("tag_id", tagIDs);
        request.AddOptionalParameter("after", after);

        return Deserialize<TwitchStreamTags>(await restClient.ExecuteAsync(request), x => x.Replace('-', '_'));
    }


    /// <summary>
    /// Gets follows relationship for a specified user
    /// </summary>
    public async Task<TwitchFollows?> GetUserFollows(
        string? fromID = null,
        string? toID = null,
        string? after = null,
        string? first = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("users/follows", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);

        request.AddOptionalParameter("first", first);
        request.AddOptionalParameter("after", after);
        request.AddOptionalParameter("from_id", fromID);
        request.AddOptionalParameter("to_id", toID);

        return Deserialize<TwitchFollows>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets video information by video ID (one or more), user ID (one only), or game ID (one only).
    /// </summary>
    public async Task<TwitchVideos?> GetVideos(
        List<string>? videoID = null,
        string? gameID = null,
        string? userID = null,
        string? after = null,
        string? before = null,
        string? first = null,
        string? language = null,
        string? period = null,
        string? sort = null,
        string? type = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("videos", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);

        request.AddOptionalParameter("video_id", videoID);
        request.AddOptionalParameter("game_id", gameID);
        request.AddOptionalParameter("user_id", userID);
        request.AddOptionalParameter("after", after);
        request.AddOptionalParameter("before", before);
        request.AddOptionalParameter("first", first);
        request.AddOptionalParameter("language", language);
        request.AddOptionalParameter("period", period);
        request.AddOptionalParameter("sort", sort);
        request.AddOptionalParameter("type", type);

        return Deserialize<TwitchVideos>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets clip information by clip ID (one or more), broadcaster ID (one only), or game ID (one only).
    /// </summary>
    public async Task<TwitchClips?> GetClips(
        List<string>? clipID = null,
        string? broadcasterID = null,
        string? gameID = null,
        string? after = null,
        string? before = null,
        string? endedAt = null,
        string? startedAt = null,
        string? first = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("clips", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);

        request.AddOptionalParameter("id", clipID);
        request.AddOptionalParameter("broadcaster_id", broadcasterID);
        request.AddOptionalParameter("game_id", gameID);
        request.AddOptionalParameter("after", after);
        request.AddOptionalParameter("before", before);
        request.AddOptionalParameter("ended_at", endedAt);
        request.AddOptionalParameter("started_at", startedAt);
        request.AddOptionalParameter("first", first);

        return Deserialize<TwitchClips>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets information about active streams. Streams are returned sorted by number of current viewers, in descending order.
    /// </summary>
    public async Task<TwitchStreams?> GetStreams(
        string? after = null,
        string? before = null,
        List<string>? communityIDs = null,
        string? first = null,
        List<string>? gameIDs = null,
        List<string>? languages = null,
        List<string>? userIDs = null,
        List<string>? userLogins = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("streams", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddOptionalParameter("user_login", userLogins);
        request.AddOptionalParameter("user_id", userIDs);
        request.AddOptionalParameter("language", languages);
        request.AddOptionalParameter("game_id", gameIDs);
        request.AddOptionalParameter("first", first);
        request.AddOptionalParameter("community_id", communityIDs);
        request.AddOptionalParameter("before", before);
        request.AddOptionalParameter("after", after);

        return Deserialize<TwitchStreams>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets information about a broadcaster's channel.
    /// </summary>
    public async Task<TwitchChannels?> GetChannels(
        string broadcaseterID)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("channels", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddParameter("broadcaster_id", broadcaseterID);

        return Deserialize<TwitchChannels>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Updates the description of a user specified by a Bearer token.
    /// </summary>
    public async Task<TwitchUsers?> UpdateDescription(
        string token,
        string description)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("users", Method.Put);
        request.AddHeader("Authorization", $"Bearer {token}");

        request.AddParameter("description", description);

        return Deserialize<TwitchUsers>(await restClient.ExecuteAsync(request));
    }

    /// <summary>
    /// Gets the subscribers of a broadcaster as specified by a Bearer token.
    /// </summary>
    public async Task<TwitchSubscriptions?> GetBroadcasterSubscribers(
        string token,
        List<string>? userIDs,
        string broadcasterID)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("subscriptions", Method.Get);
        request.AddHeader("Authorization", $"Bearer {token}");

        request.AddParameter("broadcaster_id", broadcasterID);
        request.AddOptionalParameter("user_id", userIDs);

        return Deserialize<TwitchSubscriptions>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets a user's subscriptions as specified by a Bearer token
    /// </summary>
    public async Task<TwitchSubscriptions?> GetBroadcasterSubscriptions(
        string token,
        string broadcasterID)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("subscriptions", Method.Get);
        request.AddHeader("Authorization", $"Bearer {token}");

        request.AddParameter("broadcaster_id", broadcasterID);

        return Deserialize<TwitchSubscriptions>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Creates a clip programmatically. This returns both an ID and an edit URL for the new clip.
    /// </summary>
    public async Task<TwitchCreatedClip?> CreateClip(
        string broadcasterID,
        string? hlsDelay = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("clips", Method.Post);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddParameter("broadcaster_id", broadcasterID);
        request.AddOptionalParameter("hls_delay", hlsDelay);

        return Deserialize<TwitchCreatedClip>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets a URL that game developers can use to download analytics reports (CSV files) for their games.
    /// The URL is valid for 5 minutes. 
    /// </summary>
    public async Task<TwitchGameAnalytics?> GetGameAnalytics(
        string? after = null,
        string? endedAt = null,
        string? first = null,
        string? gameID = null,
        string? startedAt = null,
        string? type = null)
    {

        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("analytics/games", Method.Get);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddOptionalParameter("after", after);
        request.AddOptionalParameter("ended_at", endedAt);
        request.AddOptionalParameter("first", first);
        request.AddOptionalParameter("game_id", gameID);
        request.AddOptionalParameter("started_at", startedAt);
        request.AddOptionalParameter("type", type);

        return Deserialize<TwitchGameAnalytics>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets a list of all extensions (both active and inactive) for a specified user, identified by a Bearer token.
    /// </summary>
    public async Task<TwitchUserExtensions?> GetUserExtensions()
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("users/extensions/list", Method.Get);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        return Deserialize<TwitchUserExtensions>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets information about active extensions installed by a specified user, identified by a user ID or Bearer token.
    /// </summary>
    public async Task<TwitchActiveUserExtensions?> GetActiveUserExtensions(
        string? userID = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("users/extensions", Method.Get);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddOptionalParameter("user_id", userID);

        return Deserialize<TwitchActiveUserExtensions>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets a URL that extension developers can use to download analytics reports (CSV files) for their extensions. The URL is valid for 5 minutes.
    /// </summary>
    public async Task<TwitchExtensionAnalytics?> GetExtensionAnalytics(
        string? after = null,
        string? endedAt = null,
        string? extensionID = null,
        string? first = null,
        string? startedAt = null,
        string? type = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("analytics/extensions", Method.Get);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddOptionalParameter("after", after);
        request.AddOptionalParameter("ended_at", endedAt);
        request.AddOptionalParameter("extension_id", extensionID);
        request.AddOptionalParameter("first", first);
        request.AddOptionalParameter("started_at", startedAt);
        request.AddOptionalParameter("type", type);

        return Deserialize<TwitchExtensionAnalytics>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets metadata information about active streams playing Overwatch or Hearthstone.
    /// Streams are sorted by number of current viewers, in descending order.
    /// Across multiple pages of results, there may be duplicate or missing streams, as viewers join and leave streams.
    /// </summary>
    public async Task<TwitchStreamsMetadata?> GetStreamsMetadata(
        string? after = null,
        List<string>? communityIDs = null,
        string? before = null,
        string? first = null,
        List<string>? gameIDs = null,
        List<string>? languages = null,
        List<string>? userIDs = null,
        List<string>? userLogins = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("streams/metadata", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);

        request.AddOptionalParameter("after", after);
        request.AddOptionalParameter("community_id", communityIDs);
        request.AddOptionalParameter("before", before);
        request.AddOptionalParameter("first", first);
        request.AddOptionalParameter("game_id", gameIDs);
        request.AddOptionalParameter("language", languages);
        request.AddOptionalParameter("user_id", userIDs);
        request.AddOptionalParameter("user_login", userLogins);

        return Deserialize<TwitchStreamsMetadata>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Updates the activation state, extension ID, and/or version number of installed extensions for a specified user,
    /// identified by a Bearer token. If you try to activate a given extension under multiple extension types,
    /// the last write wins (and there is no guarantee of write order).
    /// </summary>
    /// <returns>Twitch_ActiveUserExtensions</returns>
    public async Task<TwitchActiveUserExtensions?> UpdateUserExtensions(
        TwitchUpdateActiveUserExtensions toUpdate)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("users/extensions", Method.Put);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");
        request.AddHeader("Content-Type", "application/json");

        //Add serialized toUpdate as body
        request.AddJsonBody(toUpdate);

        return Deserialize<TwitchActiveUserExtensions>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Gets a ranked list of Bits leaderboard information for an authorized broadcaster.
    /// </summary>
    /// <returns>Twitch_BitsLeaderboard</returns>
    public async Task<TwitchBitsLeaderboard?> GetBitsLeaderboard(
        string? count = null,
        string? period = null,
        string? startedAt = null,
        string? userID = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("bits/leaderboard", Method.Get);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddOptionalParameter("count", count);
        request.AddOptionalParameter("period", period);
        request.AddOptionalParameter("started_at", startedAt);
        request.AddOptionalParameter("user_id", userID);

        return Deserialize<TwitchBitsLeaderboard>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Applies specified tags to a specified stream, overwriting any existing tags applied to that stream.
    /// If no tags are specified, all tags previously applied to the stream are removed.
    /// Automated tags are not affected by this operation.
    /// </summary>
    public async Task<bool> ReplaceStreamTags(
        string broadcasterID,
        TwitchTagsUpdate? tags = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("streams/tags", Method.Put);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");
        request.AddHeader("Content-Type", "application/json");

        request.AddParameter("broadcaster_id", broadcasterID);

        //Serialize "toUpdate" to json
        if (tags?.TagIDs is not null && tags.TagIDs.Count > 0)
        {
            //Serialize "toUpdate" to json and add
            request.AddJsonBody(tags);
        }

        RestResponse response = await restClient.ExecuteAsync(request);

        return response.StatusCode == HttpStatusCode.NoContent;
    }


    /// <summary>
    /// Gets the Webhook subscriptions of a user identified by a Bearer token, in order of expiration.
    /// </summary>
    public async Task<TwitchWebhookResponse?> GetWebhookSubscriptions(
        string clientCredentialsToken,
        string? after = null,
        string? first = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("webhooks/subscriptions", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {clientCredentialsToken}");

        request.AddOptionalParameter("after", after);
        request.AddOptionalParameter("first", first);

        return Deserialize<TwitchWebhookResponse>(await restClient.ExecuteAsync(request));
    }


    /// <summary>
    /// Subscrube/Unsubscribe the indicated topic for webhooks.
    /// </summary>
    public async Task<bool> WebhookSubscribe(
        string callback,
        string mode,
        string topic,
        int lease,
        string secret)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("webhooks/hub", Method.Post);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddParameter("hub.callback", callback);
        request.AddParameter("hub.mode", mode);
        request.AddParameter("hub.topic", topic);
        request.AddParameter("hub.lease_seconds", lease);
        request.AddParameter("hub.secret", secret);

        RestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.Accepted)
        {
            communication.SendErrorMessage($"Bad Response code from webhooks subscription: {response.Content}");
        }

        return response.StatusCode == HttpStatusCode.Accepted;
    }

    /// <summary>
    /// Retrieves the list of available Cheermotes, animated emotes to which viewers can assign Bits, to cheer in chat.
    /// Cheermotes returned are available throughout Twitch, in all Bits-enabled channels.
    /// </summary>
    public async Task<TwitchCheermotes?> GetCheermotes(
        string? broadcasterID = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("bits/cheermotes", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddOptionalParameter("broadcaster_id", broadcasterID);

        return Deserialize<TwitchCheermotes>(await restClient.ExecuteAsync(request));
    }

    /// <summary>
    /// Returns a list of Custom Reward objects for the Custom Rewards on a channel.
    /// Developers only have access to update and delete rewards that the same/calling client_id created.
    /// </summary>
    public async Task<TwitchCustomReward?> GetCustomReward(
        string? id = null,
        bool? onlyManageableRewards = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("channel_points/custom_rewards", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BroadcasterAccessToken}");

        request.AddParameter("broadcaster_id", botConfig.BroadcasterId);

        request.AddOptionalParameter("id", id);
        request.AddOptionalParameter("only_manageable_rewards", onlyManageableRewards);

        return Deserialize<TwitchCustomReward>(await restClient.ExecuteAsync(request));
    }

    public async Task<TwitchCustomReward?> CreateCustomReward(
        string title,
        int cost,
        string? prompt = null,
        bool? enabled = null,
        string? backgroundColor = null,
        bool? userInputRequired = null,
        int? maxPerStream = null,
        int? maxPerUserPerStream = null,
        int? globalCooldown = null,
        bool? skipQueue = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("channel_points/custom_rewards", Method.Post);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BroadcasterAccessToken}");
        request.AddQueryParameter("broadcaster_id", botConfig.BroadcasterId);

        request.AddJsonBody(new TwitchCustomRewardCreate(
            Title: title,
            Cost: cost,
            Prompt: prompt,
            BackgroundColor: backgroundColor,
            IsEnabled: enabled,
            IsUserInputRequired: userInputRequired,
            IsMaxPerStreamEnabled: maxPerStream.HasValue ? true : null,
            MaxPerStream: maxPerStream,
            IsMaxPerUserPerStreamEnabled: maxPerUserPerStream.HasValue ? true : null,
            MaxPerUserPerStream: maxPerUserPerStream,
            IsGlobalCooldownEnabled: globalCooldown.HasValue ? true : null,
            GlobalCooldownSeconds: globalCooldown,
            ShouldRedemptionsSkipRewardQueue: skipQueue));

        return Deserialize<TwitchCustomReward>(await restClient.ExecuteAsync(request));
    }

    public async Task<TwitchCustomRewardRedemption?> GetCustomRewardRedemptions(
        string rewardId,
        string? id = null,
        string? status = null,
        string? sort = null,
        string? after = null,
        int? first = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("channel_points/custom_rewards/redemptions", Method.Get);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BroadcasterAccessToken}");

        request.AddParameter("broadcaster_id", botConfig.BroadcasterId);
        request.AddParameter("reward_id", rewardId);

        request.AddOptionalParameter("id", id);
        request.AddOptionalParameter("status", status);
        request.AddOptionalParameter("sort", sort);
        request.AddOptionalParameter("after", after);
        request.AddOptionalParameter("first", first);

        return Deserialize<TwitchCustomRewardRedemption>(await restClient.ExecuteAsync(request));
    }

    public async Task<TwitchCustomRewardRedemption?> UpdateCustomRewardRedemptions(
        string rewardId,
        string id,
        string status)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("channel_points/custom_rewards/redemptions", Method.Patch);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BroadcasterAccessToken}");

        request.AddQueryParameter("id", id);
        request.AddQueryParameter("broadcaster_id", botConfig.BroadcasterId);
        request.AddQueryParameter("reward_id", rewardId);

        request.AddJsonBody(new TwitchRedemptionUpdate(status));

        return Deserialize<TwitchCustomRewardRedemption>(await restClient.ExecuteAsync(request));
    }

    public async Task<TwitchCustomRewardRedemption?> UpdateCustomRewardProperties(
        string id,
        string? title = null,
        int? cost = null,
        string? prompt = null,
        string? backgroundColor = null,
        bool? enabled = null,
        bool? userInputRequired = null,
        int? maxPerStream = null,
        int? maxPerUserPerStream = null,
        int? globalCooldown = null,
        bool? paused = null,
        bool? skipQueue = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("channel_points/custom_rewards", Method.Patch);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BroadcasterAccessToken}");

        request.AddQueryParameter("broadcaster_id", botConfig.BroadcasterId);
        request.AddQueryParameter("id", id);

        request.AddJsonBody(new TwitchCustomRewardUpdate(
            Title: title,
            Prompt: prompt,
            Cost: cost,
            BackgroundColor: backgroundColor,
            IsEnabled: enabled,
            IsUserInputRequired: userInputRequired,
            IsMaxPerStreamEnabled: maxPerStream.HasValue ? true : null,
            MaxPerStream: maxPerStream,
            IsMaxPerUserPerStreamEnabled: maxPerUserPerStream.HasValue ? true : null,
            MaxPerUserPerStream: maxPerUserPerStream,
            IsGlobalCooldownEnabled: globalCooldown.HasValue ? true : null,
            GlobalCooldownSeconds: globalCooldown,
            IsPaused: paused,
            ShouldRedemptionsSkipRewardQueue: skipQueue));

        return Deserialize<TwitchCustomRewardRedemption>(await restClient.ExecuteAsync(request));
    }

    public async Task<bool> SendChatAnnouncement(
        string message,
        string? color = null)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("chat/announcements", Method.Post);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddQueryParameter("broadcaster_id", botConfig.BroadcasterId);
        request.AddQueryParameter("moderator_id", botConfig.BotId);

        request.AddJsonBody(new TwitchChatAnnouncementData(
            Message: message,
            Color: color));

        RestResponse response = await restClient.ExecuteAsync(request);

        return response.StatusCode == HttpStatusCode.NoContent;
    }

    public async Task<bool> SendShoutout(string broadcasterId)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("chat/shoutouts", Method.Post);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BotAccessToken}");

        request.AddQueryParameter("from_broadcaster_id", botConfig.BroadcasterId);
        request.AddQueryParameter("to_broadcaster_id", broadcasterId);
        request.AddQueryParameter("moderator_id", botConfig.BotId);

        RestResponse response = await restClient.ExecuteAsync(request);

        return response.StatusCode == HttpStatusCode.NoContent;
    }

    /// <summary>
    /// Subscribe to EventSub Websocket event
    /// </summary>
    public async Task<TwitchSubscribeResponse?> EventSubSubscribe(
        string subscriptionType,
        string sessionId)
    {
        RestClient restClient = new RestClient(HelixURI);
        RestRequest request = new RestRequest("eventsub/subscriptions", Method.Post);
        request.AddHeader("Client-ID", botConfig.TwitchClientId);
        request.AddHeader("Authorization", $"Bearer {botConfig.BroadcasterAccessToken}");

        request.AddJsonBody(new TwitchSubscribeRequest(
            SubscriptionType: subscriptionType,
            Version: GetVersion(subscriptionType),
            Condition: GetCondition(subscriptionType),
            Transport: new Transport(
                Method: "websocket",
                SessionId: sessionId)));

        RestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.Accepted)
        {
            communication.SendWarningMessage($"Bad response to Subscribe request: {response.StatusCode} - {response.Content}");
            return default;
        }

        return JsonSerializer.Deserialize<TwitchSubscribeResponse>(response.Content!);
    }

    private T? Deserialize<T>(RestResponse response)
    {
        if (response.StatusCode != HttpStatusCode.OK)
        {
            communication.SendWarningMessage($"Failed to get response of type {typeof(T).Name} with status code {response.StatusCode}: {response.StatusDescription}");
            return default;
        }

        return JsonSerializer.Deserialize<T>(response.Content!);
    }

    private static T? Deserialize<T>(RestResponse response, Func<string, string> contentMutator)
    {
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(contentMutator(response.Content!));
    }

    private static string GetVersion(string subscriptionType) => subscriptionType switch
    {
        "channel.follow" => "2",
        "channel.update" => "2",
        _ => "1"
    };

    private Condition GetCondition(string subscriptionType)
    {
        switch (subscriptionType)
        {
            case "channel.follow":
                return new Condition(
                    BroadcasterUserId: botConfig.BroadcasterId,
                    ModeratorUserId: botConfig.BroadcasterId);

            case "channel.update":
            case "channel.subscribe":
            case "channel.subscription.end":
            case "channel.subscription.gift":
            case "channel.subscription.message":
            case "channel.cheer":
            case "channel.ban":
            case "channel.unban":
            case "channel.moderator.add":
            case "channel.moderator.remove":
            case "channel.channel_points_custom_reward.add":
            case "channel.poll.begin":
            case "channel.poll.progress":
            case "channel.poll.end":
            case "channel.prediction.begin":
            case "channel.prediction.progress":
            case "channel.prediction.lock":
            case "channel.prediction.end":
            case "channel.goal.begin":
            case "channel.goal.progress":
            case "channel.goal.end":
            case "channel.hype_train.begin":
            case "channel.hype_train.progress":
            case "channel.hype_train.end":
            case "stream.online":
            case "stream.offline":
            case "channel.channel_points_custom_reward.update":
            case "channel.channel_points_custom_reward.remove":
            case "channel.channel_points_custom_reward_redemption.add":
            case "channel.channel_points_custom_reward_redemption.update":
                return new Condition(
                    BroadcasterUserId: botConfig.BroadcasterId);

            case "channel.raid":
                return new Condition(
                    ToBroadcasterUserId: botConfig.BroadcasterId);

            default: throw new NotSupportedException($"SubscriptionType Not Supported: {subscriptionType}");
        }
    }
}
