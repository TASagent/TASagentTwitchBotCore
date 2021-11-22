using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers;

[ApiController]
[Route("/TASagentBotAPI/Notifications/[action]")]
[ConditionalFeature("Notifications")]
public class NotificationController : ControllerBase
{
    private readonly Notifications.IActivityDispatcher activityDispatcher;
    private readonly IMessageAccumulator messsageAccumulator;
    private readonly IHubContext<Hubs.MonitorHub> monitorHub;

    public NotificationController(
        Notifications.IActivityDispatcher activityDispatcher,
        IMessageAccumulator messsageAccumulator,
        IHubContext<Hubs.MonitorHub> monitorHub)
    {
        this.activityDispatcher = activityDispatcher;
        this.messsageAccumulator = messsageAccumulator;
        this.monitorHub = monitorHub;
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Privileged)]
    public IActionResult Skip()
    {
        activityDispatcher.Skip();
        return Ok();
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Privileged)]
    public async Task<IActionResult> UpdatePendingNotification(UpdatePendingNotificationMessage message)
    {
        if (!messsageAccumulator.RemovePendingNotification(message.Index) ||
            !activityDispatcher.UpdatePendingRequest(message.Index, message.Approved))
        {
            return BadRequest();
        }

        await monitorHub.Clients.All.SendAsync("NotifyPendingRemoved", message.Index);


        //Failed to authenticate
        return Ok();
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Privileged)]
    public IActionResult ReplayNotification(ReplayNotificationMessage message)
    {
        if (!activityDispatcher.ReplayNotification(message.Index))
        {
            return BadRequest();
        }

        return Ok();
    }

    public record UpdatePendingNotificationMessage(int Index, bool Approved);
    public record ReplayNotificationMessage(int Index);
}
