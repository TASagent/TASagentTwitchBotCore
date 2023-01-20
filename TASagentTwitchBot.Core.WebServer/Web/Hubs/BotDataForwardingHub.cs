using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.WebServer.Models;
using TASagentTwitchBot.Core.WebServer.TTS;

namespace TASagentTwitchBot.Core.WebServer.Web.Hubs;

[Authorize(AuthenticationSchemes = "Token", Roles = "DataForwarding")]
public class BotDataForwardingHub : Hub
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ILogger<BotDataForwardingHub> logger;
    private readonly IServerDataForwardingHandler dataForwardingHandler;
    private readonly IDataForwardingConnectionManager dataForwardingConnectionManager;

    public BotDataForwardingHub(
        UserManager<ApplicationUser> userManager,
        ILogger<BotDataForwardingHub> logger,
        IServerDataForwardingHandler dataForwardingHandler,
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
            await Groups.AddToGroupAsync(Context.ConnectionId, user.TwitchBroadcasterName.ToUpper());

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

    public async Task ClearFileList(string context)
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

        dataForwardingHandler.ClearFileList(user.TwitchBroadcasterName, context);
    }

    public async Task AppendFileList(string context, List<ServerDataFile> dataFiles)
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

        dataForwardingHandler.AppendFileList(user.TwitchBroadcasterName, context, dataFiles);
    }

    public void UploadDataFileMetaData(string requestIdentifier, string? contentType, int totalBytes)
    {
        dataForwardingHandler.ReceiveFileMetaData(requestIdentifier, contentType, totalBytes);
    }

    public void UploadDataFileData(string requestIdentifier, byte[] data, int current)
    {
        dataForwardingHandler.ReceiveFileData(requestIdentifier, data, current);
    }

    public void CancelFileTransfer(string requestIdentifier, string reason)
    {
        dataForwardingHandler.CancelFileTransfer(requestIdentifier, reason);
    }
}
