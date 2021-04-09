using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/Midi/[action]")]
    [ConditionalFeature("Midi")]
    public class MidiController : ControllerBase
    {
        private readonly Audio.MidiKeyboardHandler midiKeyboardHandler;

        public MidiController(
            Audio.MidiKeyboardHandler midiKeyboardHandler)
        {
            this.midiKeyboardHandler = midiKeyboardHandler;
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
        public IActionResult CurrentMidiDevice(
            SettingsController.DeviceRequest deviceRequest)
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
        public IActionResult CurrentMidiOutputDevice(
            SettingsController.DeviceRequest deviceRequest)
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
    }
}
