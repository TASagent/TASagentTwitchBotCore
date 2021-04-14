using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/OAuth/[Action]")]
    [ConditionalFeature("Twitch")]
    public class OAuthController : ControllerBase
    {
        public OAuthController()
        {
        }

        [HttpPost]
        public IActionResult BotCode(
            [FromServices] API.Twitch.IBotTokenValidator botTokenValidator,
            [FromQuery(Name = "code")] string code,
            [FromQuery(Name = "state")] string state)
        {
            botTokenValidator.SetCode(code, state);
            return Ok();
        }

        [HttpPost]
        public IActionResult BroadcasterCode(
            [FromServices] API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator,
            [FromQuery(Name = "code")] string code,
            [FromQuery(Name = "state")] string state)
        {
            broadcasterTokenValidator.SetCode(code, state);
            return Ok();
        }
    }
}
