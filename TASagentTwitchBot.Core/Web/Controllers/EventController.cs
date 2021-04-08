using System;
using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/Event/[action]")]
    public class EventController : ControllerBase
    {
        private readonly ICommunication communication;
        private readonly ApplicationManagement applicationManagement;

        public EventController(
            ICommunication communication,
            ApplicationManagement applicationManagement)
        {
            this.communication = communication;
            this.applicationManagement = applicationManagement;
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult Quit()
        {
            applicationManagement.TriggerExit();
            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult Speak(PrintMessage message)
        {
            communication.SendPublicChatMessage(message.Message);
            return Ok();
        }

        [HttpPost]
        [AuthRequired]
        public IActionResult Print(PrintMessage message)
        {
            communication.SendDebugMessage(message.Message);
            return Ok();
        }
    }

    public record PrintMessage(string Message);
}
