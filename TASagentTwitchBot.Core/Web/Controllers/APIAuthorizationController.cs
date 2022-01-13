using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers;

[ApiController]
[Route("/TASagentBotAPI/Auth/[Action]")]
public class APIAuthorizationController : ControllerBase
{
    private readonly Config.BotConfiguration botConfig;
    private readonly ICommunication communication;

    public APIAuthorizationController(
        Config.BotConfiguration botConfig,
        ICommunication communication)
    {
        this.botConfig = botConfig;
        this.communication = communication;
    }

    [HttpPost]
    public ActionResult<AuthorizationResult> Authorize(AuthorizationAttempt request)
    {
        AuthDegree attemptedAuth;
        string authString;

        try
        {
            attemptedAuth = botConfig.AuthConfiguration.TryCredentials(request.Password, out authString);
        }
        catch (Exception e)
        {
            communication.SendErrorMessage($"Attempt to validate password encountered exception: {e}.");
            return StatusCode(500);
        }

        if (!botConfig.AuthConfiguration.PublicAuthAllowed && attemptedAuth <= AuthDegree.Privileged)
        {
            if (attemptedAuth == AuthDegree.None)
            {
                communication.SendWarningMessage($"User tried to authenticate with an invalid password while locked down.");
            }
            else
            {
                //Rejected, valid authentication attempt
                communication.SendWarningMessage($"User tried to authenticate as {attemptedAuth} while locked down.");
            }

            return Forbid();
        }

        if (attemptedAuth == AuthDegree.None)
        {
            communication.SendWarningMessage($"A user has failed to authenticate.");
            return Unauthorized();
        }

        communication.SendWarningMessage($"{attemptedAuth} authenticated.");

        return new AuthorizationResult(attemptedAuth.ToString(), authString);
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public IActionResult Lockdown(
        LockdownStatus status,
        [FromServices] IMessageAccumulator messageAccumulator)
    {
        botConfig.AuthConfiguration.PublicAuthAllowed = !status.Locked;

        if (status.Locked)
        {
            //Becoming locked
            //Generate a new Auth strings
            botConfig.AuthConfiguration.RegenerateAuthStrings();

            //Reset authenticated users
            messageAccumulator.ClearAuthenticatedUsers();
        }

        //Save updated authstrings to file
        botConfig.Serialize();

        return Ok();
    }

    [HttpGet]
    public ActionResult<LockdownStatus> Lockdown()
    {
        return new LockdownStatus(!botConfig.AuthConfiguration.PublicAuthAllowed);
    }
}

public record AuthorizationAttempt(string Password);
public record AuthorizationResult(string Role, string AuthString);
public record LockdownStatus(bool Locked);
