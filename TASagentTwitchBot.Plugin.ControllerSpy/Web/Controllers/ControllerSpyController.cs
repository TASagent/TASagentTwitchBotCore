using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Web;
using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Plugin.ControllerSpy.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/ControllerSpy/[action]")]
    [ConditionalFeature("Audio")]
    public class ControllerSpyController : ControllerBase
    {
        private readonly IControllerManager controllerManager;

        public ControllerSpyController(
            IControllerManager controllerManager)
        {
            this.controllerManager = controllerManager;
        }

        [HttpGet]
        [AuthRequired(AuthDegree.Admin)]
        public ActionResult<IEnumerable<string>> GetPorts()
        {
            return controllerManager.GetPorts();
        }

        [HttpGet]
        [AuthRequired(AuthDegree.Admin)]
        public ActionResult<string> CurrentPort()
        {
            return controllerManager.GetCurrentPort();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult Attach(PortIdentification port)
        {
            if (string.IsNullOrEmpty(port.Port))
            {
                controllerManager.Detatch();
                return Ok();
            }

            if (!controllerManager.Attach(port.Port))
            {
                return BadRequest();
            }

            return Ok();
        }
    }

    public record PortIdentification(string Port);
}
