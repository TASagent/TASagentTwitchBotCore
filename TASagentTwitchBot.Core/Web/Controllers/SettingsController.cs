using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers;

[ApiController]
[Route("/TASagentBotAPI/Settings/[action]")]
[ConditionalFeature("Audio")]
public class SettingsController : ControllerBase
{
    private readonly Config.BotConfiguration botConfig;
    private readonly Audio.IAudioDeviceManager audioDeviceManager;

    public SettingsController(
        Config.BotConfiguration botConfig,
        Audio.IAudioDeviceManager audioDeviceManager)
    {
        this.botConfig = botConfig;
        this.audioDeviceManager = audioDeviceManager;
    }

    [HttpGet]
    [AuthRequired(AuthDegree.Admin)]
    public ActionResult<IEnumerable<string>> OutputDevices() =>
        audioDeviceManager.GetOutputDevices();

    [HttpGet]
    [AuthRequired(AuthDegree.Admin)]
    public ActionResult<IEnumerable<string>> InputDevices() =>
        audioDeviceManager.GetInputDevices();

    [HttpGet]
    [AuthRequired(AuthDegree.Admin)]
    public ActionResult<string> CurrentVoiceOutputDevice() =>
        audioDeviceManager.GetAudioDeviceName(Audio.AudioDeviceType.VoiceOutput);

    [HttpGet]
    [AuthRequired(AuthDegree.Admin)]
    public ActionResult<string> CurrentVoiceInputDevice() =>
        audioDeviceManager.GetAudioDeviceName(Audio.AudioDeviceType.MicrophoneInput);

    [HttpGet]
    [AuthRequired(AuthDegree.Admin)]
    public ActionResult<string> CurrentEffectOutputDevice() =>
        audioDeviceManager.GetAudioDeviceName(Audio.AudioDeviceType.EffectOutput);

    [HttpGet]
    public ActionResult<ErrHEnabled> ErrorHEnabled() =>
        new ErrHEnabled(botConfig.CommandConfiguration.GlobalErrorHandlingEnabled);

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public IActionResult ErrorHEnabled(ErrHEnabled eHEnabled)
    {
        //Set CompressorConfig
        botConfig.CommandConfiguration.GlobalErrorHandlingEnabled = eHEnabled.Enabled;

        //Save
        botConfig.Serialize();

        return Ok();
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public IActionResult CurrentVoiceOutputDevice(DeviceRequest deviceRequest)
    {
        if (!audioDeviceManager.OverrideAudioDevice(Audio.AudioDeviceType.VoiceOutput, deviceRequest.Device))
        {
            return BadRequest();
        }

        return Ok();
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public IActionResult CurrentVoiceInputDevice(DeviceRequest deviceRequest)
    {
        if (!audioDeviceManager.OverrideAudioDevice(Audio.AudioDeviceType.MicrophoneInput, deviceRequest.Device))
        {
            return BadRequest();
        }

        return Ok();
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public IActionResult CurrentEffectOutputDevice(DeviceRequest deviceRequest)
    {
        if (!audioDeviceManager.OverrideAudioDevice(Audio.AudioDeviceType.EffectOutput, deviceRequest.Device))
        {
            return BadRequest();
        }

        return Ok();
    }
    public record ErrHEnabled(bool Enabled);
    public record DeviceRequest(string Device);
}
