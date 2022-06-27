using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Plugin.Audio.Midi.Web.Controllers;

[ApiController]
[Route("/TASagentBotAPI/Midi/[action]")]
public class MidiController : ControllerBase
{
    private readonly IMidiKeyboardHandler midiKeyboardHandler;
    private readonly IMidiDeviceManager midiDeviceManager;
    private readonly IAudioDeviceManager audioDeviceManager;

    public MidiController(
        IAudioDeviceManager audioDeviceManager,
        IMidiKeyboardHandler midiKeyboardHandler,
        IMidiDeviceManager midiDeviceManager)
    {
        this.midiKeyboardHandler = midiKeyboardHandler;
        this.audioDeviceManager = audioDeviceManager;
        this.midiDeviceManager = midiDeviceManager;
    }

    [HttpGet]
    [AuthRequired(AuthDegree.Admin)]
    public ActionResult<IEnumerable<string>> MidiDevices() =>
        midiDeviceManager.GetMidiDevices();

    [HttpGet]
    [AuthRequired(AuthDegree.Admin)]
    public ActionResult<string?> CurrentMidiDevice() =>
        midiDeviceManager.GetMidiDeviceName(0);

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public IActionResult CurrentMidiDevice(
        DeviceRequest deviceRequest)
    {
        if (!midiDeviceManager.UpdateMidiDevice(0, deviceRequest.Device))
        {
            return BadRequest();
        }

        return Ok();
    }

    [HttpGet]
    [AuthRequired(AuthDegree.Admin)]
    public ActionResult<string> CurrentMidiOutputDevice() =>
        audioDeviceManager.GetAudioDeviceName(AudioDeviceType.MidiOutput);

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public IActionResult CurrentMidiOutputDevice(
        DeviceRequest deviceRequest)
    {
        if (!audioDeviceManager.OverrideAudioDevice(AudioDeviceType.MidiOutput, deviceRequest.Device))
        {
            return BadRequest();
        }

        return Ok();
    }

    [HttpGet]
    [AuthRequired(AuthDegree.Admin)]
    public ActionResult<IEnumerable<string>> MidiInstruments() =>
        midiKeyboardHandler.GetSupportedInstruments();

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public IActionResult CurrentMidiInstrument(
        MidiRequest deviceRequest)
    {
        if (!midiKeyboardHandler.BindToInstrument(deviceRequest.Effect))
        {
            return BadRequest();
        }

        return Ok();
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public IActionResult CurrentMidiSoundEffect(
        MidiRequest deviceRequest)
    {
        if (!midiKeyboardHandler.BindToSoundEffect(deviceRequest.Effect))
        {
            return BadRequest();
        }

        return Ok();
    }

    public record MidiRequest(string Effect, int Channel);
    public record DeviceRequest(string Device);
}
