﻿using System.Text.Json.Serialization;

namespace TASagentTwitchBot.Core.API.BTTV;

public record BTTVChannelData(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("bots")] List<string> Bots,
    [property: JsonPropertyName("channelEmotes")] List<BTTVChannelEmote> ChannelEmotes,
    [property: JsonPropertyName("sharedEmotes")] List<BTTVSharedEmote> SharedEmotes);


public abstract record Emote(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("imageType")] string ImageType)
{
    public abstract string GetSmallURL();
    public abstract string GetMediumURL();
    public abstract string GetLargeURL();
}

public record BTTVEmote(
    [property: JsonPropertyName("id")] string Id,
    string Code,
    string ImageType)
    : Emote(Code, ImageType)
{
    public override string GetSmallURL() => $"https://cdn.betterttv.net/emote/{Id}/1x";
    public override string GetMediumURL() => $"https://cdn.betterttv.net/emote/{Id}/2x";
    public override string GetLargeURL() => $"https://cdn.betterttv.net/emote/{Id}/3x";
}

public record BTTVGlobalEmote(
    string Id,
    string Code,
    string ImageType,
    [property: JsonPropertyName("userId")] string UserId)
    : BTTVEmote(Id, Code, ImageType);

public record BTTVChannelEmote(
    string Id,
    string Code,
    string ImageType,
    [property: JsonPropertyName("userId")] string UserId)
    : BTTVEmote(Id, Code, ImageType);

public record BTTVSharedEmote(
    string Id,
    string Code,
    string ImageType,
    [property: JsonPropertyName("user")] BTTVUser User)
    : BTTVEmote(Id, Code, ImageType);

public record FFZEmote(
    [property: JsonPropertyName("id")] int Id,
    string Code,
    string ImageType,
    [property: JsonPropertyName("user")] FFZUser User,
    [property: JsonPropertyName("images")] FFZEmoteImageCollection Images)
    : Emote(Code, ImageType)
{
    public override string GetSmallURL() => Images.Small;
    public override string GetMediumURL() => Images.Medium;
    public override string GetLargeURL() => Images.Large;
}

public record FFZEmoteImageCollection(
    [property: JsonPropertyName("1x")] string Small,
    [property: JsonPropertyName("2x")] string Medium,
    [property: JsonPropertyName("4x")] string Large);

public record User(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("displayName")] string DisplayName);

public record BTTVUser(
    [property: JsonPropertyName("id")] string Id,
    string Name,
    string DisplayName,
    [property: JsonPropertyName("providerId")] string ProviderId)
    : User(Name, DisplayName);

public record FFZUser(
    [property: JsonPropertyName("id")] int Id,
    string Name,
    string DisplayName)
    : User(Name, DisplayName);
