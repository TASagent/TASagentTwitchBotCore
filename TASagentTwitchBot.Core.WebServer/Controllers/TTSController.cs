using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.WebServer.Models;
using TASagentTwitchBot.Core.WebServer.TTS;

namespace TASagentTwitchBot.Core.WebServer.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "Token", Roles = "TTSExternal")]
[Route("/TASagentServerAPI/[controller]/[action]")]
public class TTSController : Controller
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IServerTTSRenderer ttsHandler;

    public TTSController(
        UserManager<ApplicationUser> userManager,
        IServerTTSRenderer ttsHandler)
    {
        this.userManager = userManager;
        this.ttsHandler = ttsHandler;
    }

    [HttpPost]
    public async Task<ActionResult> Synthesize(RawServerExternalTTSRequest request)
    {
        if (string.IsNullOrEmpty(request.Text))
        {
            return BadRequest("No text included");
        }

        ApplicationUser user = await userManager.GetUserAsync(HttpContext.User);
        byte[]? data = await ttsHandler.HandleRawExternalTTSRequest(userManager, user, request);

        if (data is null)
        {
            return BadRequest();
        }

        return File(data, "audio/mpeg", "synthesis.mp3");
    }
}

public record RawServerExternalTTSRequest(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("voice")] string Voice,
    [property: JsonPropertyName("pitch")] string Pitch,
    [property: JsonPropertyName("speed")] string Speed);
