using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Web;
using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Plugin.TTTAS.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/TTTAS/[action]")]
    [ConditionalFeature("Audio")]
    public class TTTASController : ControllerBase
    {
        private readonly ITTTASProvider tttasProvider;

        public TTTASController(
            ITTTASProvider tttasProvider)
        {
            this.tttasProvider = tttasProvider;
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult ShowPrompt()
        {
            tttasProvider.ShowPrompt();
            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult StartRecording()
        {
            tttasProvider.StartRecording();
            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult EndRecording()
        {
            tttasProvider.EndRecording();
            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult ClearPrompt()
        {
            tttasProvider.ClearPrompt();
            return Ok();
        }
    }
}
