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
    private readonly IServerEventSubHandler eventSubHandler;

    public BotEventSubHub(
        UserManager<ApplicationUser> userManager,
        IServerEventSubHandler eventSubHandler)
    {
        this.userManager = userManager;
        this.eventSubHandler = eventSubHandler;
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

    public async Task Subscribe(string subType)
    {
        ApplicationUser user = await userManager.GetUserAsync(Context.User);
        await eventSubHandler.SubscribeToStandardEvent(user, subType);
    }

    public async Task ReportDesiredEventSubs(HashSet<string> subTypes)
    {
        ApplicationUser user = await userManager.GetUserAsync(Context.User);
        await eventSubHandler.ReportDesiredEventSubs(user, subTypes);
    }

    public async Task ReportUndesiredEventSub(string subType)
    {
        ApplicationUser user = await userManager.GetUserAsync(Context.User);
        await eventSubHandler.ReportUndesiredEventSub(user, subType);
    }
}
