using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.WebServer.TTS;
using TASagentTwitchBot.Core.WebServer.Web;

namespace TASagentTwitchBot.Core.WebServer.Controllers;

[ApiController]
[Route("/TASagentServerAPI/DataForwarding/{userNameString}/SFX")]
public class DataForwardingSFXController : Controller
{
    private readonly IServerDataForwardingSFXHandler dataForwardingSFXHandler;

    public DataForwardingSFXController(
        IServerDataForwardingSFXHandler dataForwardingSFXHandler)
    {
        this.dataForwardingSFXHandler = dataForwardingSFXHandler;
    }

    [HttpGet("")]
    [AllowCrossSite]
    public ActionResult<List<ServerSoundEffect>> List(string userNameString)
    {
        return dataForwardingSFXHandler.GetSoundEffectList(userNameString);
    }

    [HttpGet("{soundEffectString}")]
    [AllowCrossSite]
    public async Task<IActionResult> Fetch(string userNameString, string soundEffectString)
    {
        if (string.IsNullOrEmpty(soundEffectString))
        {
            return BadRequest();
        }

        if (soundEffectString.StartsWith('/'))
        {
            soundEffectString = soundEffectString[1..];
        }

        ServerSoundEffectData? soundEffect = await dataForwardingSFXHandler.GetSoundEffectByAlias(userNameString, soundEffectString);

        if (soundEffect is null)
        {
            return NotFound();
        }

        return File(
            fileContents: soundEffect.Data,
            contentType: soundEffect.ContentType ?? "",
            fileDownloadName: "");
    }
}
