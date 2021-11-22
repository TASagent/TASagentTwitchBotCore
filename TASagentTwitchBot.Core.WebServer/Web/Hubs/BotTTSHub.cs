using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

using TASagentTwitchBot.Core.WebServer.Models;
using TASagentTwitchBot.Core.WebServer.TTS;
using TASagentTwitchBot.Core.TTS;
using System.Text.Json.Serialization;

namespace TASagentTwitchBot.Core.WebServer.Web.Hubs;

[Authorize(AuthenticationSchemes = "Token", Roles = "TTS")]
public class BotTTSHub : Hub
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IServerTTSRenderer ttsHandler;

    public BotTTSHub(
        UserManager<ApplicationUser> userManager,
        IServerTTSRenderer ttsHandler)
    {
        this.userManager = userManager;
        this.ttsHandler = ttsHandler;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        ApplicationUser user = await userManager.GetUserAsync(Context.User);

        if (user is not null && !string.IsNullOrEmpty(user.TwitchBroadcasterId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, user.TwitchBroadcasterId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    public async Task RequestTTS(ServerTTSRequest ttsRequest)
    {
        ApplicationUser user = await userManager.GetUserAsync(Context.User);
        await ttsHandler.HandleTTSRequest(userManager, user, ttsRequest);
    }

    public async Task RequestRawTTS(RawServerTTSRequest rawTTSRequest)
    {
        ApplicationUser user = await userManager.GetUserAsync(Context.User);
        await ttsHandler.HandleRawTTSRequest(userManager, user, rawTTSRequest);
    }
}

public record RawServerTTSRequest(
    [property: JsonPropertyName("requestIdentifier")] string RequestIdentifier,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("voice")] string Voice,
    [property: JsonPropertyName("pitch")] string Pitch,
    [property: JsonPropertyName("speed")] string Speed);
