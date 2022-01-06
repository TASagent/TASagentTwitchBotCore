using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.API.Twitch;
using TASagentTwitchBot.Core.WebServer.Models;
using TASagentTwitchBot.Core.WebServer.Web.Middleware;

namespace TASagentTwitchBot.Core.WebServer.Controllers;

[ApiController]
[Route("/TASagentServerAPI/[controller]/[action]")]
public class EventSubController : Controller
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly EventSub.IServerEventSubHandler eventSubHandler;
    private readonly ILogger<EventSubController> logger;

    public EventSubController(
        UserManager<ApplicationUser> userManager,
        EventSub.IServerEventSubHandler eventSubHandler,
        ILogger<EventSubController> logger)
    {
        this.userManager = userManager;
        this.eventSubHandler = eventSubHandler;
        this.logger = logger;
    }

    [RewindRequired]
    [HttpPost("{userId}")]
    public async Task<IActionResult> Event(
        TwitchEventSubPayload payload,
        string userId)
    {
        string messageId = Request.Headers["Twitch-Eventsub-Message-Id"];
        string messageTimestamp = Request.Headers["Twitch-Eventsub-Message-Timestamp"];
        string signature = Request.Headers["Twitch-Eventsub-Message-Signature"];
        string messageType = Request.Headers["Twitch-Eventsub-Message-Type"];

        if (string.IsNullOrEmpty(signature))
        {
            logger.LogWarning("Received EventCall without a signature.");
            return BadRequest("No Signature in Header");
        }

        if (string.IsNullOrEmpty(messageType))
        {
            logger.LogWarning("Received EventCall without a Message-Type.");
            return BadRequest("No Message-Type in Header");
        }

        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("Received EventCall without a userId.");
            return BadRequest("Malformed URL");
        }

        if (payload is null)
        {
            logger.LogWarning("Received EventCall without a payload.");
            return BadRequest();
        }

        ApplicationUser user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            //User not identified.
            //Return Ok if it is a revocation anyway
            if (messageType == "revocation")
            {
                logger.LogInformation("Received revocation for unrecognized user: {userId}, {payload}", userId, payload);
                if (eventSubHandler.HandleSubRevocation(payload.Subscription.Id))
                {
                    return Ok();
                }

                logger.LogInformation("Received unexpected webhook revocation: {payload}", payload);
                return Ok();
            }

            logger.LogInformation("Received EventSub for unrecognized user: {userId}, {payload}", userId, payload);
            return BadRequest();
        }

        Request.Body.Position = 0;
        using StreamReader reader = new StreamReader(
            stream: Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: -1,
            leaveOpen: true);

        string body = await reader.ReadToEndAsync();

        if (!eventSubHandler.VerifyEventSubMessage(
            secret: user.SubscriptionSecret,
            signature: signature,
            message: $"{messageId}{messageTimestamp}{body}"))
        {
            logger.LogWarning("Received EventCall but the Signature failed.");
            return Unauthorized("Signature failed");
        }

        switch (messageType)
        {
            case "webhook_callback_verification":
                //Handle Subscription Verification
                if (string.IsNullOrEmpty(payload.Challenge))
                {
                    logger.LogWarning("Received webhook_callback_verification without a Challenge payload: {body}", body);
                    return BadRequest("No Challenge Payload found");
                }

                if (eventSubHandler.HandleSubVerification(payload.Subscription.Id))
                {
                    logger.LogInformation("Confirmed Sub Payload, subscription verified.");
                    return Ok(payload.Challenge);
                }

                logger.LogInformation("Received unexpected webhook_callback_verification: {body}", body);
                return BadRequest("No match to pending subs");

            case "revocation":
                if (eventSubHandler.HandleSubRevocation(payload.Subscription.Id))
                {
                    return Ok();
                }

                logger.LogInformation("Received unexpected webhook revocation: {body}", body);
                return Ok();

            case "notification":
                //Handle event
                eventSubHandler.HandleEventPayload(user, payload);
                return Ok();

            default:
                logger.LogInformation("Received unexpected messageType: {messageType}, {body}", messageType, body);
                //Handle event anyway
                eventSubHandler.HandleEventPayload(user, payload);
                return Ok();
        }
    }
}
