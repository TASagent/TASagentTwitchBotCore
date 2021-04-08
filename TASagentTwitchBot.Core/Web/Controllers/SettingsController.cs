using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/Settings/[action]")]
    public class SettingsController : ControllerBase
    {
        private readonly Audio.IMicrophoneHandler microphoneHandler;
        private readonly Audio.IAudioPlayer audioPlayer;
        private readonly Audio.MidiKeyboardHandler midiKeyboardHandler;

        public SettingsController(
            Audio.IMicrophoneHandler microphoneHandler,
            Audio.IAudioPlayer audioPlayer,
            Audio.MidiKeyboardHandler midiKeyboardHandler)
        {
            this.microphoneHandler = microphoneHandler;
            this.audioPlayer = audioPlayer;
            this.midiKeyboardHandler = midiKeyboardHandler;
        }

        [HttpGet]
        [AuthRequired(AuthDegree.Admin)]
        public ActionResult<IEnumerable<string>> OutputDevices() =>
            microphoneHandler.GetOutputDevices();

        [HttpGet]
        [AuthRequired(AuthDegree.Admin)]
        public ActionResult<IEnumerable<string>> InputDevices() =>
            microphoneHandler.GetInputDevices();

        [HttpGet]
        [AuthRequired(AuthDegree.Admin)]
        public ActionResult<string> CurrentVoiceOutputDevice() =>
            microphoneHandler.GetCurrentVoiceOutputDevice();

        [HttpGet]
        [AuthRequired(AuthDegree.Admin)]
        public ActionResult<string> CurrentVoiceInputDevice() =>
            microphoneHandler.GetCurrentVoiceInputDevice();

        [HttpGet]
        [AuthRequired(AuthDegree.Admin)]
        public ActionResult<string> CurrentEffectOutputDevice() =>
            audioPlayer.GetCurrentEffectOutputDevice();

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult CurrentVoiceOutputDevice(DeviceRequest deviceRequest)
        {
            if (!microphoneHandler.UpdateVoiceOutputDevice(deviceRequest.Device))
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult CurrentVoiceInputDevice(DeviceRequest deviceRequest)
        {
            if (!microphoneHandler.UpdateVoiceInputDevice(deviceRequest.Device))
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult CurrentEffectOutputDevice(DeviceRequest deviceRequest)
        {
            if (!audioPlayer.UpdateEffectOutputDevice(deviceRequest.Device))
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpGet]
        [AuthRequired(AuthDegree.Admin)]
        public ActionResult<IEnumerable<string>> MidiDevices() =>
            midiKeyboardHandler.GetMidiDevices();

        [HttpGet]
        [AuthRequired(AuthDegree.Admin)]
        public ActionResult<string> CurrentMidiDevice() =>
            midiKeyboardHandler.GetCurrentMidiDevice();

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult CurrentMidiDevice(DeviceRequest deviceRequest)
        {
            if (!midiKeyboardHandler.UpdateCurrentMidiDevice(deviceRequest.Device))
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpGet]
        [AuthRequired(AuthDegree.Admin)]
        public ActionResult<string> CurrentMidiOutputDevice() =>
            midiKeyboardHandler.GetCurrentMidiOutputDevice();

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult CurrentMidiOutputDevice(DeviceRequest deviceRequest)
        {
            if (!midiKeyboardHandler.UpdateCurrentMidiOutputDevice(deviceRequest.Device))
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
        public IActionResult CurrentMidiInstrument(MidiRequest deviceRequest)
        {
            if (!midiKeyboardHandler.BindToInstrument(deviceRequest.Effect))
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult CurrentMidiSoundEffect(MidiRequest deviceRequest)
        {
            if (!midiKeyboardHandler.BindToSoundEffect(deviceRequest.Effect))
            {
                return BadRequest();
            }

            return Ok();
        }

        public record DeviceRequest(string Device);
        public record MidiRequest(string Effect, int Channel);
    }
}
