using System;
using Microsoft.AspNetCore.Mvc;

namespace TASagentTwitchBot.Core.Web.Controllers
{
    [ApiController]
    [Route("/BrowserSource")]
    [ConditionalFeature("Overlay")]
    public class BrowserSourceController : ControllerBase
    {
        public BrowserSourceController()
        {
        }

        [HttpPut]
        [Route("Logging/Error")]
        public IActionResult LogError(string errorString)
        {
            //TwitchBotApplication.Output.WriteWarningLine($"BrowserSource Error: {errorString}");
            return Ok();
        }

        [HttpPut]
        [Route("Logging/Info")]
        public IActionResult LogInfo(string errorString)
        {
            //TwitchBotApplication.Output.WriteDebugLine($"BrowserSource Notice: {errorString}");
            return Ok();
        }
    }
}
