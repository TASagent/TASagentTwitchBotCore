using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.WebServer.Models;
using TASagentTwitchBot.Core.WebServer.TTS;
using System.Collections.Concurrent;
using Google.Api;
using System.Diagnostics.CodeAnalysis;

namespace TASagentTwitchBot.Core.WebServer.Web.Hubs;

[Authorize(AuthenticationSchemes = "Token", Roles = "DataForwarding")]
public class BotDataForwardingHub : Hub
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ILogger<BotDataForwardingHub> logger;
    private readonly IServerDataForwardingSFXHandler dataForwardingHandler;
    private readonly IDataForwardingConnectionManager dataForwardingConnectionManager;

    public BotDataForwardingHub(
        UserManager<ApplicationUser> userManager,
        ILogger<BotDataForwardingHub> logger,
        IServerDataForwardingSFXHandler dataForwardingHandler,
        IDataForwardingConnectionManager dataForwardingConnectionManager)
    {
        this.userManager = userManager;
        this.logger = logger;
        this.dataForwardingHandler = dataForwardingHandler;
        this.dataForwardingConnectionManager = dataForwardingConnectionManager;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        if (Context.User is null)
        {
            logger.LogWarning("Received DataForwarding connection from null user {User} with connectionId {ConnectionId}", Context.UserIdentifier, Context.ConnectionId);
            return;
        }

        ApplicationUser? user = await userManager.GetUserAsync(Context.User);

        if (user is not null && !string.IsNullOrEmpty(user.TwitchBroadcasterName))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, user.TwitchBroadcasterName.ToLower());

            dataForwardingConnectionManager.AddConnection(user.TwitchBroadcasterName, Context.ConnectionId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.User is not null)
        {
            ApplicationUser? user = await userManager.GetUserAsync(Context.User);

            if (user is not null && !string.IsNullOrEmpty(user.TwitchBroadcasterName))
            {
                dataForwardingConnectionManager.RemoveConnection(user.TwitchBroadcasterName);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task UpdateSoundEffects(List<ServerSoundEffect> soundEffects)
    {
        if (Context.User is null)
        {
            logger.LogWarning("Received DataForwarding request from null user {User} with connectionId {ConnectionId}", Context.UserIdentifier, Context.ConnectionId);
            return;
        }

        ApplicationUser? user = await userManager.GetUserAsync(Context.User);

        if (user is null || string.IsNullOrEmpty(user.TwitchBroadcasterName))
        {
            logger.LogWarning("Received DataForwarding request from unknown user {User} with connectionId {ConnectionId}", Context.User.ToString(), Context.ConnectionId);
            return;
        }

        dataForwardingHandler.UpdateSoundEffectList(user.TwitchBroadcasterName, soundEffects);
    }

    public void UploadSoundEffect(string requestIdentifier, string name, byte[] data, string? contentType)
    {
        dataForwardingHandler.ReceiveSoundEffect(requestIdentifier, name, data, contentType);
    }

    public void CancelSoundEffect(string requestIdentifier, string reason)
    {
        dataForwardingHandler.CancelSoundEffect(requestIdentifier, reason);
    }
}
