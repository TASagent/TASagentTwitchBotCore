using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

using TASagentTwitchBot.Core.WebServer.Models;
using TASagentTwitchBot.Core.WebServer.EventSub;

namespace TASagentTwitchBot.Core.WebServer.Web.Hubs;

[Authorize(AuthenticationSchemes = "Token", Roles = "EventSub")]
public class BotEventSubHub : Hub
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ILogger<BotEventSubHub> logger;
    private readonly IServerEventSubHandler eventSubHandler;

    public BotEventSubHub(
        UserManager<ApplicationUser> userManager,
        ILogger<BotEventSubHub> logger,
        IServerEventSubHandler eventSubHandler)
    {
        this.userManager = userManager;
        this.logger = logger;
        this.eventSubHandler = eventSubHandler;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        if (Context.User is null)
        {
            logger.LogWarning("Received EventSub connection from null user {User} with connectionId {ConnectionId}", Context.UserIdentifier, Context.ConnectionId);
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

    public async Task Subscribe(string subType)
    {
        if (Context.User is null)
        {
            logger.LogWarning("Received EventSub Subscribe request from null user {User} with connectionId {ConnectionId}", Context.UserIdentifier, Context.ConnectionId);
            return;
        }

        ApplicationUser? user = await userManager.GetUserAsync(Context.User);

        if (user is null)
        {
            logger.LogWarning("Received EventSub Subscribe request from unknown user {User} with connectionId {ConnectionId}", Context.User.ToString(), Context.ConnectionId);
            return;
        }

        await eventSubHandler.SubscribeToStandardEvent(user, subType);
    }

    public async Task ReportDesiredEventSubs(HashSet<string> subTypes)
    {
        if (Context.User is null)
        {
            logger.LogWarning("Received EventSub ReportDesiredEventSubs request from null user {User} with connectionId {ConnectionId}", Context.UserIdentifier, Context.ConnectionId);
            return;
        }

        ApplicationUser? user = await userManager.GetUserAsync(Context.User);

        if (user is null)
        {
            logger.LogWarning("Received EventSub ReportDesiredEventSubs request from unknown user {User} with connectionId {ConnectionId}", Context.User.ToString(), Context.ConnectionId);
            return;
        }

        await eventSubHandler.ReportDesiredEventSubs(user, subTypes);
    }

    public async Task ReportUndesiredEventSub(string subType)
    {
        if (Context.User is null)
        {
            logger.LogWarning("Received EventSub ReportUndesiredEventSub request from null user {User} with connectionId {ConnectionId}", Context.UserIdentifier, Context.ConnectionId);
            return;
        }

        ApplicationUser? user = await userManager.GetUserAsync(Context.User);

        if (user is null)
        {
            logger.LogWarning("Received EventSub ReportUndesiredEventSub request from unknown user {User} with connectionId {ConnectionId}", Context.User.ToString(), Context.ConnectionId);
            return;
        }

        await eventSubHandler.ReportUndesiredEventSub(user, subType);
    }
}
