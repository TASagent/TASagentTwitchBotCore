using Microsoft.AspNetCore.Mvc;

namespace TASagentTwitchBot.Core.Web.Controllers;

[ApiController]
[Route("/TASagentBotAPI/OAuth/[Action]")]
[ConditionalFeature("Twitch")]
public class OAuthController : ControllerBase
{
    public OAuthController()
    {
    }

    [HttpPost]
    [HttpGet]
    public IActionResult BotCode(
        [FromServices] API.Twitch.IBotTokenValidator botTokenValidator,
        [FromQuery(Name = "code")] string code,
        [FromQuery(Name = "state")] string state)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return BadRequest();
        }

        botTokenValidator.SetCode(code, state);
        return Ok();
    }

    [HttpPost]
    [HttpGet]
    public IActionResult BroadcasterCode(
        [FromServices] API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator,
        [FromQuery(Name = "code")] string code,
        [FromQuery(Name = "state")] string state)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return BadRequest();
        }

        broadcasterTokenValidator.SetCode(code, state);
        return Ok();
    }
}
