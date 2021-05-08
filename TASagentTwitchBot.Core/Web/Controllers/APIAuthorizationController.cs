using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/Auth/[Action]")]
    public class APIAuthorizationController : ControllerBase
    {
        private readonly Config.IBotConfigContainer botConfigContainer;
        private readonly IMessageAccumulator messageAccumulator;
        private readonly ICommunication communication;

        public APIAuthorizationController(
            Config.IBotConfigContainer botConfigContainer,
            ICommunication communication,
            IMessageAccumulator messageAccumulator)
        {
            this.botConfigContainer = botConfigContainer;
            this.communication = communication;
            this.messageAccumulator = messageAccumulator;
        }

        [HttpPost]
        public ActionResult<AuthorizationResult> Authorize(AuthorizationAttempt request)
        {
            var hash = BaseConfigurator.HashPassword(request.Password, botConfigContainer.BotConfig.AuthConfiguration.Salt);
            AuthDegree attemptedAuth = 
                botConfigContainer.BotConfig.AuthConfiguration.TryCredentials(hash, out string authString);

            if (!botConfigContainer.BotConfig.AuthConfiguration.PublicAuthAllowed && attemptedAuth <= AuthDegree.Privileged)
            {
                communication.SendWarningMessage($"User tried to authenticate as {attemptedAuth} with password \"{request.Password}\" while locked down.");
                return Forbid();
            }

            if (attemptedAuth == AuthDegree.None)
            {
                communication.SendWarningMessage($"User failed to authenticate with password \"{request.Password}\".");
                return Unauthorized();
            }

            communication.SendWarningMessage($"{attemptedAuth} authenticated.");

            return new AuthorizationResult(attemptedAuth.ToString(), authString);
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult Lockdown(LockdownStatus status)
        {
            botConfigContainer.BotConfig.AuthConfiguration.PublicAuthAllowed = !status.Locked;

            if (status.Locked)
            {
                //Becoming locked
                //Generate a new Auth strings
                botConfigContainer.BotConfig.AuthConfiguration.RegenerateAuthStrings();

                //Reset authenticated users
                messageAccumulator.ClearAuthenticatedUsers();
            }

            //Save
            botConfigContainer.SerializeData();

            return Ok();
        }

        [HttpGet]
        public ActionResult<LockdownStatus> Lockdown()
        {
            return new LockdownStatus(!botConfigContainer.BotConfig.AuthConfiguration.PublicAuthAllowed);
        }
    }

    public record AuthorizationAttempt(string Password);
    public record AuthorizationResult(string Role, string AuthString);
    public record LockdownStatus(bool Locked);
}
