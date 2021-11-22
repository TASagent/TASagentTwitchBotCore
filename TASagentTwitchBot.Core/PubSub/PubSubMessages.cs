using System.Text.Json.Serialization;

using TASagentTwitchBot.Core.API.Twitch;

namespace TASagentTwitchBot.Core.PubSub;

public record BasicPubSubMessage(
    [property: JsonPropertyName("type")] string TypeString);


public record PubSubMessage(
    string TypeString,
    [property: JsonPropertyName("nonce")] string Nonce) : BasicPubSubMessage(TypeString);

public record PubSubResponseMessage(
    string TypeString,
    [property: JsonPropertyName("error")] string ErrorString) : PubSubMessage(TypeString, Guid.NewGuid().ToString());

public record ListenResponse(
    string TypeString,
    [property: JsonPropertyName("data")] ListenResponse.Datum Data) : BasicPubSubMessage(TypeString)
{
    public record Datum(
        [property: JsonPropertyName("topic")] string Topic,
        [property: JsonPropertyName("message")] string Message);
}

public record ListenMessage : PubSubMessage
{
    [JsonPropertyName("data")]
    public Datum? Data { get; init; }

    public ListenMessage() : base("LISTEN", Guid.NewGuid().ToString()) { }

    public ListenMessage(Datum data)
        : base("LISTEN", Guid.NewGuid().ToString())
    {
        Data = data;
    }

    public ListenMessage(IEnumerable<string> topics, string authToken)
        : base("LISTEN", Guid.NewGuid().ToString())
    {
        Data = new Datum(topics.ToList(), authToken);
    }

    public record Datum(
        [property: JsonPropertyName("topics")] List<string> Topics,
        [property: JsonPropertyName("auth_token")] string AuthToken);
}

public record BaseMessageData(
    [property: JsonPropertyName("type")] string TypeString);

public record ChannelPointMessageData(
    [property: JsonPropertyName("data")] ChannelPointMessageData.Datum Data) : BaseMessageData("reward-redeemed")
{
    public record Datum(
        [property: JsonPropertyName("timestamp")] DateTime Timestamp,
        [property: JsonPropertyName("redemption")] Datum.RedemptionData Redemption)
    {
        public record RedemptionData(
            [property: JsonPropertyName("id")] string Id,
            [property: JsonPropertyName("user")] RedemptionData.UserData User,
            [property: JsonPropertyName("channel_id")] string ChannelId,
            [property: JsonPropertyName("redeemed_at")] DateTime RedeemedAt,
            [property: JsonPropertyName("reward")] RedemptionData.RewardData Reward,
            [property: JsonPropertyName("user_input")] string UserInput,
            [property: JsonPropertyName("status")] string Status)
        {
            public record UserData(
                [property: JsonPropertyName("id")] string Id,
                [property: JsonPropertyName("login")] string Login,
                [property: JsonPropertyName("display_name")] string DisplayName);

            public record RewardData(
                [property: JsonPropertyName("id")] string Id,
                [property: JsonPropertyName("channel_id")] string ChannelId,
                [property: JsonPropertyName("title")] string Title,
                [property: JsonPropertyName("prompt")] string Prompt,
                [property: JsonPropertyName("cost")] int Cost,
                [property: JsonPropertyName("is_user_input_required")] bool IsUserInputRequired,
                [property: JsonPropertyName("is_sub_only")] bool IsSubOnly,
                [property: JsonPropertyName("image")] TwitchCustomReward.Datum.ImageData Image,
                [property: JsonPropertyName("default_image")] TwitchCustomReward.Datum.ImageData DefaultImage,
                [property: JsonPropertyName("background_color")] string BackgroundColor,
                [property: JsonPropertyName("is_enabled")] bool IsEnabled,
                [property: JsonPropertyName("is_paused")] bool IsPaused,
                [property: JsonPropertyName("is_in_stock")] bool IsInStock,
                [property: JsonPropertyName("max_per_stream")] TwitchCustomReward.Datum.StreamMax MaxPerStream,
                [property: JsonPropertyName("should_redemptions_skip_request_queue")] bool ShouldRedemptionsSkipRequestQueue);
        }
    }
}