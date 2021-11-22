using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TASagentTwitchBot.Core.WebServer.Models;
using TASagentTwitchBot.Core.WebServer.Web.Hubs;

namespace TASagentTwitchBot.Core.WebServer.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> logger;

    public HomeController(ILogger<HomeController> logger)
    {
        this.logger = logger;
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
        [FromServices] IHubContext<BotEventSubHub> botEventSubHub)
    {
        if (string.IsNullOrEmpty(message))
        {
            message = "Hi";
        }

        message = $"Admin Message: {message}";

        await botEventSubHub.Clients.All.SendAsync("ReceiveMessage", message);
        logger.LogInformation($"Sending message: {message}");

        return RedirectToAction("Index");
    }
}
