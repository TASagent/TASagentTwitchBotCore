using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using BGC.IO;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers;

[ApiController]
[Route("/TASagentBotAPI/SFX/[action]")]
[ConditionalFeature("SFX")]
[ConditionalFeature("Audio")]
public class SFXController : ControllerBase
{
    private readonly ISoundEffectSystem soundEffectSystem;
    private readonly IAudioPlayer audioPlayer;
    private readonly Config.BotConfiguration botConfig;

    public SFXController(
        Config.BotConfiguration botConfig,
        ISoundEffectSystem soundEffectSystem,
        IAudioPlayer audioPlayer)
    {
        this.botConfig = botConfig;
        this.soundEffectSystem = soundEffectSystem;
        this.audioPlayer = audioPlayer;
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Privileged)]
    public IActionResult PlayImmediate(SoundEffectLookup request)
    {
        if (string.IsNullOrEmpty(request.Effect))
        {
            return BadRequest();
        }

        string sfxString = request.Effect.ToLowerInvariant();

        if (sfxString.StartsWith('/'))
        {
            sfxString = sfxString[1..];
        }

        SoundEffect? soundEffect = soundEffectSystem.GetSoundEffectByAlias(sfxString);

        if (soundEffect is null)
        {
            return BadRequest();
        }

        audioPlayer.DemandPlayAudioImmediate(new SoundEffectRequest(soundEffect));

        return Ok();
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Privileged)]
    public IActionResult PlayImmediateByName(SoundEffectLookup request)
    {
        if (string.IsNullOrEmpty(request.Effect))
        {
            return BadRequest();
        }

        string sfxString = request.Effect.ToLowerInvariant();

        if (sfxString.StartsWith('/'))
        {
            sfxString = sfxString[1..];
        }

        SoundEffect? soundEffect = soundEffectSystem.GetSoundEffectByName(sfxString);

        if (soundEffect is null)
        {
            return BadRequest();
        }

        audioPlayer.DemandPlayAudioImmediate(new SoundEffectRequest(soundEffect));

        return Ok();
    }

    [HttpGet("{soundEffectString}")]
    public async Task<IActionResult> FetchFile(string soundEffectString)
    {
        if (string.IsNullOrEmpty(soundEffectString))
        {
            return BadRequest();
        }

        if (soundEffectString.StartsWith('/'))
        {
            soundEffectString = soundEffectString[1..];
        }

        SoundEffect? soundEffect = soundEffectSystem.GetSoundEffectByAlias(soundEffectString);

        if (soundEffect is null)
        {
            return NotFound();
        }

        byte[] file = await System.IO.File.ReadAllBytesAsync(soundEffect.FilePath);

        new FileExtensionContentTypeProvider()
            .TryGetContentType(soundEffect.FilePath, out string? contentType);

        return File(
            fileContents: file,
            contentType: contentType ?? "",
            fileDownloadName: "");
    }

    [HttpGet]
    public ActionResult<List<SoundEffectDTO>> FetchPage(int? page)
    {
        const int elementsPerPage = 20;

        if (!page.HasValue)
        {
            page = 0;
        }

        return Listify(soundEffectSystem.GetSoundEffects(page.Value, elementsPerPage));
    }

    [HttpGet]
    public ActionResult<List<string>> FetchSoundEffects()
    {
        return soundEffectSystem.GetAllSoundEffectNames()
            .ToList();
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Privileged)]
    public IActionResult Upload(UploadSoundEffect soundEffectUpload)
    {
        if (string.IsNullOrEmpty(soundEffectUpload.Name))
        {
            return BadRequest("Empty sound effect name");
        }

        string fileName = Path.GetFileName(soundEffectUpload.FileName);
        string filePath = DataManagement.PathForDataFile("SoundEffects", fileName);

        filePath = FilePath.NextAvailableFilePath(filePath);

        const string LOOKUP_PATTERN = "base64,";

        int index = soundEffectUpload.File.IndexOf(LOOKUP_PATTERN) + LOOKUP_PATTERN.Length;

        byte[] fileBytes = Convert.FromBase64String(soundEffectUpload.File[index..]);

        //Write new audiofile for 
        System.IO.File.WriteAllBytes(filePath, fileBytes);

        string[] aliases = soundEffectUpload.Aliases.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();

        soundEffectSystem.AddEffect(
            name: soundEffectUpload.Name,
            filePath: filePath,
            aliases: aliases,
            serialize: true);

        return Ok();
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public IActionResult Remove(SoundEffectLookup soundEffectLookup)
    {
        if (string.IsNullOrEmpty(soundEffectLookup.Effect))
        {
            return BadRequest("Empty sound effect name");
        }

        if (!soundEffectSystem.RemoveEffect(soundEffectLookup.Effect))
        {
            return BadRequest("Sound effect not found");
        }

        return Ok();
    }

    private static List<SoundEffectDTO> Listify(IEnumerable<SoundEffect> soundEffects) =>
        soundEffects.Select(x => new SoundEffectDTO(
             Name: x.Name,
             Aliases: x.Aliases))
            .ToList();
}

public record UploadSoundEffect(
    string Name,
    string Aliases,
    string FileName,
    string File);

public record SoundEffectLookup(string Effect);
public record SoundEffectDTO(string Name, string[] Aliases);
