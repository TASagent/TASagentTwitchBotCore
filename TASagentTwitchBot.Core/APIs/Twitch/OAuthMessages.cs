using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TASagentTwitchBot.Core.API.Twitch
{
    public record TokenRequest(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("scope")] List<string> Scope,
        [property: JsonPropertyName("token_type")] string TokenType);

    public record TokenRefreshRequest(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("scope")] List<string> Scope);

    public record TokenValidationRequest(
        [property: JsonPropertyName("client_id")] string ClientID,
        [property: JsonPropertyName("login")] string Login,
        [property: JsonPropertyName("scopes")] List<string> Scopes,
        [property: JsonPropertyName("user_id")] string UserID,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
