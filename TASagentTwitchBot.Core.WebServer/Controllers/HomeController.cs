using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using TASagentTwitchBot.Core.WebServer.Models;
using TASagentTwitchBot.Core.WebServer.Web.Hubs;

namespace TASagentTwitchBot.Core.WebServer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Ping(
            string message,
            [FromServices] IHubContext<BotHub> botHub)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "Hi";
            }

            await botHub.Clients.All.SendAsync("ReceiveMessage", message);
            _logger.LogInformation($"Sending message: {message}");

            return RedirectToAction("Index");
        }
    }
}
