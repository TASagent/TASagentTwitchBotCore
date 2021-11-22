using System.Text.Json.Serialization;

namespace TASagentTwitchBot.Core.API.Tiltify;

public record CampaignRequest(
    [property: JsonPropertyName("meta")] RequestMetaData RequestMetaData,
    [property: JsonPropertyName("data")] Campaign Campaign);

public record Campaign(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("url")] string URL,
    [property: JsonPropertyName("startsAt")] double StartsAt,
    [property: JsonPropertyName("endsAt")] double EndsAt,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("avatar")] Avatar Avatar,
    [property: JsonPropertyName("causeId")] int CauseId,
    [property: JsonPropertyName("fundraisingEventId")] int FundraisingEventId,
    [property: JsonPropertyName("fundraiserGoalAmount")] double FundraiserGoalAmount,
    [property: JsonPropertyName("originalGoalAmount")] double OriginalGoalAmount,
    [property: JsonPropertyName("amountRaised")] double AmountRaised,
    [property: JsonPropertyName("supportingAmountRaised")] double SupportingAmountRaised,
    [property: JsonPropertyName("totalAmountRaised")] double TotalAmountRaised,
    [property: JsonPropertyName("supportable")] bool Supportable,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("user")] User User,
    [property: JsonPropertyName("livestream")] Livestream Livestream,
    [property: JsonPropertyName("causeCurrency")] string CauseCurrency);

public record CampaignDonationRequest(
    [property: JsonPropertyName("meta")] RequestMetaData RequestMetaData,
    [property: JsonPropertyName("data")] List<CampaignDonation> CampaignDonations,
    [property: JsonPropertyName("links")] PaginationTokens PaginationTokens);

public record CampaignDonation(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("amount")] double Amount,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("comment")] string Comment,
    [property: JsonPropertyName("completedAt")] double CompletedAt,
    [property: JsonPropertyName("rewardId")] int? RewardId);

public record CampaignScheduleRequest(
    [property: JsonPropertyName("meta")] RequestMetaData RequestMetaData,
    [property: JsonPropertyName("data")] List<CampaignSchedule> CampaignSchedule,
    [property: JsonPropertyName("links")] PaginationTokens PaginationTokens);

public record CampaignSchedule(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("startsAt")] double StartsAt);

public record PaginationTokens(
    [property: JsonPropertyName("prev")] string Prev,
    [property: JsonPropertyName("next")] string Next,
    [property: JsonPropertyName("self")] string Self);

public record Livestream(
    [property: JsonPropertyName("type")] string StreamType,
    [property: JsonPropertyName("channel")] string Channel);

public record Team(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("username")] string UserName,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("url")] string URL,
    [property: JsonPropertyName("avatar")] Avatar Avatar);

public record User(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("username")] string UserName,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("url")] string URL,
    [property: JsonPropertyName("avatar")] Avatar Avatar);

public record Avatar(
    [property: JsonPropertyName("src")] string Src,
    [property: JsonPropertyName("alt")] string Alt,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

public record RequestMetaData(
    [property: JsonPropertyName("status")] int Status);
