using System.Text.Json;
using System.Text.Json.Serialization;

namespace TASagentTwitchBot.Core.API.Twitch;

public record TwitchGames(
    [property: JsonPropertyName("data")] List<TwitchGames.Datum> Data,
    [property: JsonPropertyName("pagination")] Pagination Pagination)
{
    public record Datum(
        [property: JsonPropertyName("id")] string ID,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("box_art_url")] string BoxArtURL);
}

public record Pagination(
    [property: JsonPropertyName("cursor")] string Cursor);

public record TwitchUsers(
    [property: JsonPropertyName("data")] List<TwitchUsers.Datum> Data)
{
    public record Datum(
        [property: JsonPropertyName("id")] string ID,
        [property: JsonPropertyName("login")] string Login,
        [property: JsonPropertyName("display_name")] string DisplayName,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("broadcaster_type")] string BroadcasterType,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("profile_image_url")] string ProfileImageURL,
        [property: JsonPropertyName("offline_image_url")] string OfflineImageURL,
        [property: JsonPropertyName("view_count")] int ViewCount);
}

public record TwitchStreamTags(
    [property: JsonPropertyName("data")] List<TwitchStreamTags.Datum> Data)
{
    public record Datum(
        [property: JsonPropertyName("tag_id")] string TagID,
        [property: JsonPropertyName("is_auto")] bool IsAuto,
        [property: JsonPropertyName("localization_names")] Datum.LocalizationData LocalizationNames,
        [property: JsonPropertyName("localization_descriptions")] Datum.LocalizationData LocalizationDescriptions)
    {
        public record LocalizationData(
            [property: JsonPropertyName("bg_bg")] string BgBg,
            [property: JsonPropertyName("cs_cz")] string CsCz,
            [property: JsonPropertyName("da_dk")] string DaDk,
            [property: JsonPropertyName("de_de")] string DeDe,
            [property: JsonPropertyName("el_gr")] string ElGr,
            [property: JsonPropertyName("en_us")] string EnUs,
            [property: JsonPropertyName("es_es")] string EsEs,
            [property: JsonPropertyName("es_mx")] string EsMx,
            [property: JsonPropertyName("fi_fi")] string FiFi,
            [property: JsonPropertyName("fr_fr")] string FrFr,
            [property: JsonPropertyName("hu_hu")] string HuHu,
            [property: JsonPropertyName("it_it")] string ItIt,
            [property: JsonPropertyName("ja_jp")] string JaJp,
            [property: JsonPropertyName("ko_kr")] string KoKr,
            [property: JsonPropertyName("nl_nl")] string NlNl,
            [property: JsonPropertyName("no_no")] string NoNo,
            [property: JsonPropertyName("pl_pl")] string PlPl,
            [property: JsonPropertyName("pt_br")] string PtBr,
            [property: JsonPropertyName("pt_pt")] string PtPt,
            [property: JsonPropertyName("ro_ro")] string RoRo,
            [property: JsonPropertyName("ru_ru")] string RuRu,
            [property: JsonPropertyName("sk_sk")] string SkSk,
            [property: JsonPropertyName("sv_se")] string SvSe,
            [property: JsonPropertyName("th_th")] string ThTh,
            [property: JsonPropertyName("tr_tr")] string TrTr,
            [property: JsonPropertyName("vi_vn")] string ViVn,
            [property: JsonPropertyName("zh_cn")] string ZhCn,
            [property: JsonPropertyName("zh_tw")] string ZhTw);
    }
}

public record TwitchFollows(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("data")] List<TwitchFollows.Datum> Data,
    [property: JsonPropertyName("pagination")] Pagination Pagination)
{
    public record Datum(
        [property: JsonPropertyName("from_id")] string FromID,
        [property: JsonPropertyName("from_name")] string FromName,
        [property: JsonPropertyName("to_id")] string ToID,
        [property: JsonPropertyName("to_name")] string ToName,
        [property: JsonPropertyName("followed_at")] string FollowedAt);
}

public record TwitchVideos(
    [property: JsonPropertyName("data")] List<TwitchVideos.Datum> Data,
    [property: JsonPropertyName("pagination")] Pagination Pagination)
{
    public record Datum(
        [property: JsonPropertyName("id")] string ID,
        [property: JsonPropertyName("user_id")] string UserID,
        [property: JsonPropertyName("user_name")] string UserName,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("created_at")] string CreatedAt,
        [property: JsonPropertyName("published_at")] string PubliushedAt,
        [property: JsonPropertyName("url")] string URL,
        [property: JsonPropertyName("thumbnail_url")] string ThumbnailURL,
        [property: JsonPropertyName("viewable")] string Viewable,
        [property: JsonPropertyName("view_count")] int ViewCount,
        [property: JsonPropertyName("language")] string Language,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("duration")] string Duration);
}

public record TwitchClips(
    [property: JsonPropertyName("data")] List<TwitchClips.Datum> Data)
{
    public record Datum(
        [property: JsonPropertyName("id")] string ID,
        [property: JsonPropertyName("url")] string URL,
        [property: JsonPropertyName("embed_url")] string EmbedURL,
        [property: JsonPropertyName("broadcaster_id")] string BroadcasterID,
        [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
        [property: JsonPropertyName("creator_id")] string CreatorID,
        [property: JsonPropertyName("creator_name")] string CreatorName,
        [property: JsonPropertyName("video_id")] string VideoID,
        [property: JsonPropertyName("game_id")] string GameID,
        [property: JsonPropertyName("language")] string Language,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("view_count")] int ViewCount,
        [property: JsonPropertyName("created_at")] string CreatedAt,
        [property: JsonPropertyName("thumbnail_url")] string ThumbnailURL);
}

public record TwitchStreams(
    [property: JsonPropertyName("data")] List<TwitchStreamData> Data,
    [property: JsonPropertyName("pagination")] Pagination Pagination);

public record TwitchStreamData(
    [property: JsonPropertyName("id")] string ID,
    [property: JsonPropertyName("user_id")] string UserID,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("game_id")] string GameID,
    [property: JsonPropertyName("game_name")] string GameName,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("viewer_count")] int ViewerCount,
    [property: JsonPropertyName("started_at")] DateTime StartedAt,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("thumbnail_url")] string ThumbnailURL,
    [property: JsonPropertyName("tag_ids")] List<string> TagIDs,
    [property: JsonPropertyName("is_mature")] bool IsMature);


public record TwitchChannels(
    [property: JsonPropertyName("data")] List<TwitchChannels.Datum> Data)
{
    public record Datum(
        [property: JsonPropertyName("broadcaster_id")] string BroadcasterID,
        [property: JsonPropertyName("game_name")] string GameName,
        [property: JsonPropertyName("game_id")] string GameID,
        [property: JsonPropertyName("broadcaster_language")] string BroadcasterLanguage,
        [property: JsonPropertyName("title")] string Title);
}

public record TwitchSubscriptions(
    [property: JsonPropertyName("data")] List<TwitchSubscriptions.Datum> Data,
    [property: JsonPropertyName("pagination")] Pagination Pagination)
{
    public record Datum(
        [property: JsonPropertyName("broadcaster_id")] string BroadcasterID,
        [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
        [property: JsonPropertyName("is_gift")] bool IsGift,
        [property: JsonPropertyName("tier")] string Tier,
        [property: JsonPropertyName("plan_name")] string PlanName,
        [property: JsonPropertyName("user_id")] string UserID,
        [property: JsonPropertyName("user_name")] string UserName);
}

public record TwitchCreatedClip(
    [property: JsonPropertyName("data")] List<TwitchCreatedClip.Datum> Data)
{
    public record Datum(
        [property: JsonPropertyName("id")] string ID,
        [property: JsonPropertyName("edit_url")] string EditURL);
}

public record DateRange(
    [property: JsonPropertyName("started_at")] DateTime StartedAt,
    [property: JsonPropertyName("ended_at")] DateTime EndedAt);

public record TwitchGameAnalytics(
    [property: JsonPropertyName("data")] List<TwitchGameAnalytics.Datum> Data,
    [property: JsonPropertyName("pagination")] Pagination Pagination)
{
    public record Datum(
        [property: JsonPropertyName("game_id")] string GameID,
        [property: JsonPropertyName("url")] string URL,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("date_range")] DateRange DateRange);
}

public record TwitchUserExtensions(
    [property: JsonPropertyName("data")] List<TwitchUserExtensions.Datum> Data)
{
    public record Datum(
        [property: JsonPropertyName("id")] string ID,
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("can_activate")] bool CanActivate,
        [property: JsonPropertyName("type")] List<string> Type);
}

public record TwitchActiveUserExtensions(
    [property: JsonPropertyName("data")] TwitchActiveUserExtensions.Datum Data)
{
    public record Datum(
        [property: JsonPropertyName("panel")] Datum.PanelDatum Panel,
        [property: JsonPropertyName("overlay")] Datum.OverlayDatum Overlay,
        [property: JsonPropertyName("component")] Datum.ComponentData Component)
    {
        public record PanelDatum(
            [property: JsonPropertyName("1")] ExtensionDatum One,
            [property: JsonPropertyName("2")] ExtensionDatum Two,
            [property: JsonPropertyName("3")] ExtensionDatum Three);

        public record OverlayDatum(
            [property: JsonPropertyName("1")] ExtensionDatum One);

        public record ComponentData(
            [property: JsonPropertyName("1")] ComponentData.Datum One,
            [property: JsonPropertyName("2")] ComponentData.Datum Two)
        {
            public record Datum(
                [property: JsonPropertyName("active")] bool Active,
                [property: JsonPropertyName("id")] string ID,
                [property: JsonPropertyName("version")] string Version,
                [property: JsonPropertyName("name")] string Name,
                [property: JsonPropertyName("x")] int X,
                [property: JsonPropertyName("y")] int Y);
        }

        public record ExtensionDatum(
            [property: JsonPropertyName("active")] bool Active,
            [property: JsonPropertyName("id")] string ID,
            [property: JsonPropertyName("version")] string Version,
            [property: JsonPropertyName("name")] string Name);
    }
}

public record TwitchUpdateActiveUserExtensions(
    [property: JsonPropertyName("data")] TwitchUpdateActiveUserExtensions.Datum Data)
{
    public record Datum(
        [property: JsonPropertyName("panel")] Datum.PanelDatum Panel,
        [property: JsonPropertyName("overlay")] Datum.OverlayDatum Overlay,
        [property: JsonPropertyName("component")] Datum.ComponentData Component)
    {
        public record PanelDatum(
            [property: JsonPropertyName("1")] ExtensionDatum One,
            [property: JsonPropertyName("2")] ExtensionDatum Two,
            [property: JsonPropertyName("3")] ExtensionDatum Three);

        public record OverlayDatum(
            [property: JsonPropertyName("1")] ExtensionDatum One);

        public record ComponentData(
            [property: JsonPropertyName("1")] ComponentData.Datum One,
            [property: JsonPropertyName("2")] ComponentData.Datum Two)
        {
            public record Datum(
                [property: JsonPropertyName("active")] bool Active,
                [property: JsonPropertyName("id")] string ID,
                [property: JsonPropertyName("version")] string Version,
                [property: JsonPropertyName("x")] int X,
                [property: JsonPropertyName("y")] int Y);
        }

        public record ExtensionDatum(
            [property: JsonPropertyName("active")] bool Active,
            [property: JsonPropertyName("id")] string ID,
            [property: JsonPropertyName("version")] string Version);
    }
}

public record TwitchChatAnnouncementData(
    [property: JsonPropertyName("message")]
    string Message,

    [property: JsonPropertyName("color")]
    [property:JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Color);

public record TwitchExtensionAnalytics(
    [property: JsonPropertyName("data")] List<TwitchExtensionAnalytics.Datum> Data)
{
    public record Datum(
        [property: JsonPropertyName("extension_id")] string ExtensionID,
        [property: JsonPropertyName("url")] string URL,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("date_range")] DateRange DateRange);
}

public record TwitchStreamsMetadata(
    [property: JsonPropertyName("data")] List<TwitchStreamsMetadata.Datum> Data,
    [property: JsonPropertyName("pagination")] Pagination Pagination)
{
    public record Datum(
        [property: JsonPropertyName("user_id")] string UserID,
        [property: JsonPropertyName("user_name")] string UserName,
        [property: JsonPropertyName("game_id")] string GameID,
        [property: JsonPropertyName("overwatch")] Datum.OverwatchData Overwatch,
        [property: JsonPropertyName("hearthstone")] Datum.HearthstoneData Hearthstone)
    {
        public record OverwatchData(
            [property: JsonPropertyName("broadcaster")] OverwatchData.BroadcasterData Broadcaster)
        {
            public record BroadcasterData(
                [property: JsonPropertyName("hero")] BroadcasterData.HeroData Hero)
            {
                public record HeroData(
                    [property: JsonPropertyName("role")] string Role,
                    [property: JsonPropertyName("name")] string Name,
                    [property: JsonPropertyName("ability")] string Ability);
            }
        }

        public record HearthstoneData(
            [property: JsonPropertyName("broadcaster")] HearthstoneData.BroadcasterData Broadcaster,
            [property: JsonPropertyName("opponent")] HearthstoneData.OpponentData Opponent)
        {
            public record BroadcasterData(
                [property: JsonPropertyName("hero")] BroadcasterData.HeroData Hero)
            {
                public record HeroData(
                    [property: JsonPropertyName("type")] string Type,
                    [property: JsonPropertyName("class")] string ClassName,
                    [property: JsonPropertyName("name")] string Name);
            }

            public record OpponentData(
                [property: JsonPropertyName("hero")] OpponentData.HeroData Hero)
            {
                public record HeroData(
                    [property: JsonPropertyName("type")] string Type,
                    [property: JsonPropertyName("class")] string ClassName,
                    [property: JsonPropertyName("name")] string Name);
            }
        }
    }
}

public record TwitchBitsLeaderboard(
    [property: JsonPropertyName("data")] List<TwitchBitsLeaderboard.Datum> Data,
    [property: JsonPropertyName("date_range")] DateRange DateRange,
    [property: JsonPropertyName("total")] int Total)
{
    public record Datum(
        [property: JsonPropertyName("user_id")] string UserID,
        [property: JsonPropertyName("user_name")] string UserName,
        [property: JsonPropertyName("rank")] int Rank,
        [property: JsonPropertyName("score")] int Score);
}

public record TwitchTagsUpdate(
    [property: JsonPropertyName("tag_ids")] List<string> TagIDs);

public record TwitchWebhookResponse(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("data")] List<TwitchWebhookResponse.Datum> Data,
    [property: JsonPropertyName("pagination")] Pagination Pagination)
{
    public record Datum(
        [property: JsonPropertyName("topic")] string Topic,
        [property: JsonPropertyName("callback")] string Callback,
        [property: JsonPropertyName("expires_at")] string ExpiresAt);
}

public record TwitchCheermotes(
    [property: JsonPropertyName("data")] List<TwitchCheermotes.Datum> Data)
{
    public record Datum(
        [property: JsonPropertyName("prefix")] string Prefix,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("order")] int Order,
        [property: JsonPropertyName("last_updated")] DateTime LastUpdated,
        [property: JsonPropertyName("is_charitable")] bool IsCharitable,
        [property: JsonPropertyName("tiers")] List<Datum.Tier> Tiers)
    {
        public record Tier(
            [property: JsonPropertyName("id")] string Id,
            [property: JsonPropertyName("min_bits")] int MinBits,
            [property: JsonPropertyName("color")] string Color,
            [property: JsonPropertyName("can_cheer")] bool CanCheer,
            [property: JsonPropertyName("show_in_bits_card")] bool ShowInBitsCard,
            [property: JsonPropertyName("images")] Tier.ImageSet Images)
        {
            public record ImageSet(
                [property: JsonPropertyName("dark")] ImageSet.Theme Dark,
                [property: JsonPropertyName("light")] ImageSet.Theme Light)
            {
                public record Theme(
                    [property: JsonPropertyName("animated")] Theme.Images Animated,
                    [property: JsonPropertyName("static")] Theme.Images Static)
                {
                    public record Images(
                        [property: JsonPropertyName("1")] string SmallURL,
                        [property: JsonPropertyName("1.5")] string SmallishURL,
                        [property: JsonPropertyName("2")] string MediumURL,
                        [property: JsonPropertyName("3")] string LargeURL,
                        [property: JsonPropertyName("4")] string HugeURL);
                }
            }

        }
    }
}

public record TwitchCustomReward(
    [property: JsonPropertyName("data")] List<TwitchCustomReward.Datum> Data)
{
    public record Datum(
        [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
        [property: JsonPropertyName("broadcaster_id")] string BroadcasterID,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("image")] Datum.ImageData Image,
        [property: JsonPropertyName("background_color")] string BackgroundColor,
        [property: JsonPropertyName("is_enabled")] bool IsEnabled,
        [property: JsonPropertyName("cost")] int Cost,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("is_user_input_required")] bool IsUserInputRequired,
        [property: JsonPropertyName("max_per_stream_setting")] Datum.StreamMax MaxPerStreamSetting,
        [property: JsonPropertyName("max_per_user_per_stream_setting")] Datum.UserMax MaxPerUserPerStreamSetting,
        [property: JsonPropertyName("global_cooldown_setting")] Datum.Cooldown GlobalCooldownSetting,
        [property: JsonPropertyName("is_paused")] bool IsPaused,
        [property: JsonPropertyName("is_in_stock")] bool IsInStock,
        [property: JsonPropertyName("default_image")] Datum.ImageData DefaultImage,
        [property: JsonPropertyName("should_redemptions_skip_request_queue")] bool ShouldRedemptionsSkipRequestQueue,
        [property: JsonPropertyName("redemptions_redeemed_current_stream")] int? RedemptionsRedeemedCurrentStream,
        [property: JsonPropertyName("cooldown_expires_at")] string CooldownExpiresAt)
    {
        public record StreamMax(
            [property: JsonPropertyName("is_enabled")] bool IsEnabled,
            [property: JsonPropertyName("max_per_stream")] int MaxPerStream);

        public record UserMax(
            [property: JsonPropertyName("is_enabled")] bool IsEnabled,
            [property: JsonPropertyName("max_per_user_per_stream")] int MaxPerUserPerStream);

        public record Cooldown(
            [property: JsonPropertyName("is_enabled")] bool IsEnabled,
            [property: JsonPropertyName("global_cooldown_seconds")] int GlobalCooldownSeconds);

        public record ImageData(
            [property: JsonPropertyName("url_1x")] string Url1X,
            [property: JsonPropertyName("url_2x")] string Url2X,
            [property: JsonPropertyName("url_4x")] string Url4X);
    }
}

public record TwitchCustomRewardRedemption(
    [property: JsonPropertyName("data")] List<TwitchCustomRewardRedemption.Datum> Data)
{
    public record Datum(
        [property: JsonPropertyName("broadcaster_name")] string BroadcasterName,
        [property: JsonPropertyName("broadcaster_id")] string BroadcasterID,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("user_id")] string UserID,
        [property: JsonPropertyName("user_name")] string UserName,
        [property: JsonPropertyName("user_input")] string UserInput,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("redeemed_at")] DateTime RedeemedAt,
        [property: JsonPropertyName("reward")] RewardData RewardData,
        [property: JsonPropertyName("pagination")] PaginationData Pagination);

    public record RewardData(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("cost")] int Cost,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("prompt")] string Prompt);

    public record PaginationData(
        [property: JsonPropertyName("cursor")] string Cursor);
}


public record TwitchCustomRewardUpdate(
    [property: JsonPropertyName("title")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Title = null,

    [property: JsonPropertyName("prompt")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Prompt = null,

    [property: JsonPropertyName("cost")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? Cost = null,

    [property: JsonPropertyName("background_color")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? BackgroundColor = null,

    [property: JsonPropertyName("is_enabled")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsEnabled = null,

    [property: JsonPropertyName("is_user_input_required")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsUserInputRequired = null,

    [property: JsonPropertyName("is_max_per_stream_enabled")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsMaxPerStreamEnabled = null,

    [property: JsonPropertyName("max_per_stream")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? MaxPerStream = null,

    [property: JsonPropertyName("is_max_per_user_per_stream_enabled")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsMaxPerUserPerStreamEnabled = null,

    [property: JsonPropertyName("max_per_user_per_stream")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? MaxPerUserPerStream = null,

    [property: JsonPropertyName("is_global_cooldown_enabled")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsGlobalCooldownEnabled = null,

    [property: JsonPropertyName("global_cooldown_seconds")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? GlobalCooldownSeconds = null,

    [property: JsonPropertyName("is_paused")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsPaused = null,

    [property: JsonPropertyName("should_redemptions_skip_reward_queue")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? ShouldRedemptionsSkipRewardQueue = null);

public record TwitchCustomRewardCreate(

    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("cost")] int Cost,

    [property: JsonPropertyName("prompt")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Prompt = null,

    [property: JsonPropertyName("background_color")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? BackgroundColor = null,

    [property: JsonPropertyName("is_enabled")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsEnabled = null,

    [property: JsonPropertyName("is_user_input_required")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsUserInputRequired = null,

    [property: JsonPropertyName("is_max_per_stream_enabled")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsMaxPerStreamEnabled = null,

    [property: JsonPropertyName("max_per_stream")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? MaxPerStream = null,

    [property: JsonPropertyName("is_max_per_user_per_stream_enabled")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsMaxPerUserPerStreamEnabled = null,

    [property: JsonPropertyName("max_per_user_per_stream")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? MaxPerUserPerStream = null,

    [property: JsonPropertyName("is_global_cooldown_enabled")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsGlobalCooldownEnabled = null,

    [property: JsonPropertyName("global_cooldown_seconds")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? GlobalCooldownSeconds = null,

    [property: JsonPropertyName("should_redemptions_skip_reward_queue")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? ShouldRedemptionsSkipRewardQueue = null);


public record TwitchRedemptionUpdate(
    [property: JsonPropertyName("status")] string Status);

public record TwitchSubscribeRequest(
    [property: JsonPropertyName("type")] string SubscriptionType,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("condition")] Condition Condition,
    [property: JsonPropertyName("transport")] Transport Transport);

public record TwitchSubscribeResponse(
    [property: JsonPropertyName("data")] TwitchSubscriptionDatum[] Data,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("total_cost")] int TotalCost,
    [property: JsonPropertyName("max_total_cost")] int MaxTotalCost);

public record TwitchGetSubscriptionsResponse(
    [property: JsonPropertyName("data")] TwitchSubscriptionDatum[] Data,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("total_cost")] int TotalCost,
    [property: JsonPropertyName("max_total_cost")] int MaxTotalCost,
    [property: JsonPropertyName("pagination")] Pagination Pagination);

public record TwitchSubscriptionDatum(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("condition")] Condition Condition,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    [property: JsonPropertyName("transport")] Transport Transport,
    [property: JsonPropertyName("cost")] int Cost)
{
    public TwitchSubscriptionStatus GetSubscriptionStatus() => Status switch
    {
        "enabled" => TwitchSubscriptionStatus.Enabled,
        "webhook_callback_verification_pending" => TwitchSubscriptionStatus.WebhookCallbackVerificationPending,
        "webhook_callback_verification_failed" => TwitchSubscriptionStatus.WebhookCallbackVerificationFailed,
        "notification_failures_exceeded" => TwitchSubscriptionStatus.NotificationFailuresExceeded,
        "authorization_revoked" => TwitchSubscriptionStatus.AuthorizationRevoked,
        "user_removed" => TwitchSubscriptionStatus.UserRemoved,

        _ => TwitchSubscriptionStatus.MAX
    };
}

public record Condition(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("broadcaster_user_id")]
    string? BroadcasterUserId = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("from_broadcaster_user_id")]
    string? FromBroadcasterUserId = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("to_broadcaster_user_id")]
    string? ToBroadcasterUserId = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("reward_id")]
    string? RewardId = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("extension_client_id")]
    string? ExtensionClientId = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("client_id")]
    string? ClientId = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("user_id")]
    string? UserId = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("moderator_user_id")]
    string? ModeratorUserId = null);

public record Transport(
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("callback")] string Callback,
    [property: JsonPropertyName("secret")] string? Secret = null);

public record TwitchEventSubPayload(
    [property: JsonPropertyName("subscription")] TwitchSubscriptionDatum Subscription,
    [property: JsonPropertyName("event")] JsonElement TwitchEvent = default,
    [property: JsonPropertyName("challenge")] string? Challenge = null);

public enum TwitchDeleteSubscriptionResponse
{
    Success = 0,
    NotFound,
    AuthFailed,
    MAX
}

public enum TwitchSubscriptionStatus
{
    Enabled = 0,
    WebhookCallbackVerificationPending,
    WebhookCallbackVerificationFailed,
    NotificationFailuresExceeded,
    AuthorizationRevoked,
    UserRemoved,
    MAX
}
