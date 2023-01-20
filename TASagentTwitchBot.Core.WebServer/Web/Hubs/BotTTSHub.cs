using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

using TASagentTwitchBot.Core.WebServer.Models;
using TASagentTwitchBot.Core.WebServer.TTS;
using TASagentTwitchBot.Core.TTS;

namespace TASagentTwitchBot.Core.WebServer.Web.Hubs;

[Authorize(AuthenticationSchemes = "Token", Roles = "TTS")]
public class BotTTSHub : Hub
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ILogger<BotTTSHub> logger;
    private readonly IServerTTSRenderer ttsHandler;

    public BotTTSHub(
        UserManager<ApplicationUser> userManager,
        ILogger<BotTTSHub> logger,
        IServerTTSRenderer ttsHandler)
    {
        this.userManager = userManager;
        this.logger = logger;
        this.ttsHandler = ttsHandler;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        if (Context.User is null)
        {
            logger.LogWarning("Received TTS connection from null user {User} with connectionId {ConnectionId}", Context.UserIdentifier, Context.ConnectionId);
            return;
        }

        ApplicationUser? user = await userManager.GetUserAsync(Context.User);

        if (user is not null && !string.IsNullOrEmpty(user.TwitchBroadcasterId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, user.TwitchBroadcasterId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    public async Task RequestNewTTS(ServerTTSRequest ttsRequest)
    {
        if (Context.User is null)
        {
            logger.LogWarning("Received TTS request from null user {User} with connectionId {ConnectionId}", Context.UserIdentifier, Context.ConnectionId);
            return;
        }

        ApplicationUser? user = await userManager.GetUserAsync(Context.User);

        if (user is null)
        {
            logger.LogWarning("Received TTS request from unknown user {User} with connectionId {ConnectionId}", Context.User.ToString(), Context.ConnectionId);
            return;
        }

        await ttsHandler.HandleTTSRequest(userManager, user, ttsRequest);
    }

#pragma warning disable CS0618 // Type or member is obsolete
    /// <summary>
    /// Handles legacy requests using the old TTSVoice enum. Included for backwards compatibility
    /// </summary>
    public async Task RequestTTS(LegacyServerTTSRequest legacyTTSRequest)
    {
        if (Context.User is null)
        {
            logger.LogWarning("Received TTS request from null user {User} with connectionId {ConnectionId}", Context.UserIdentifier, Context.ConnectionId);
            return;
        }

        ApplicationUser? user = await userManager.GetUserAsync(Context.User);

        if (user is null)
        {
            logger.LogWarning("Received TTS request from unknown user {User} with connectionId {ConnectionId}", Context.User.ToString(), Context.ConnectionId);
            return;
        }

        ServerTTSRequest ttsRequest = new ServerTTSRequest(
                RequestIdentifier: legacyTTSRequest.RequestIdentifier,
                Ssml: legacyTTSRequest.Ssml,
                Voice: legacyTTSRequest.Voice.Serialize(),
                Pitch: legacyTTSRequest.Pitch,
                Speed: legacyTTSRequest.Speed);

        await ttsHandler.HandleTTSRequest(userManager, user, ttsRequest);
    }
#pragma warning restore CS0618 // Type or member is obsolete

    public async Task RequestRawTTS(RawServerTTSRequest rawTTSRequest)
    {
        if (Context.User is null)
        {
            logger.LogWarning("Received TTS request from null user {User} with connectionId {ConnectionId}", Context.UserIdentifier, Context.ConnectionId);
            return;
        }

        ApplicationUser? user = await userManager.GetUserAsync(Context.User);

        if (user is null)
        {
            logger.LogWarning("Received TTS request from unknown user {User} with connectionId {ConnectionId}", Context.User.ToString(), Context.ConnectionId);
            return;
        }

        await ttsHandler.HandleRawTTSRequest(userManager, user, rawTTSRequest);
    }
}

public record RawServerTTSRequest(
    [property: JsonPropertyName("requestIdentifier")] string RequestIdentifier,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("voice")] string Voice,
    [property: JsonPropertyName("pitch")] string Pitch,
    [property: JsonPropertyName("speed")] string Speed);
