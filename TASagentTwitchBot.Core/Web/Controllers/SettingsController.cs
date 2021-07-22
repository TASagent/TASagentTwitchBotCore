using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/Settings/[action]")]
    [ConditionalFeature("Audio")]
    public class SettingsController : ControllerBase
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly Audio.IMicrophoneHandler microphoneHandler;
        private readonly Audio.IAudioPlayer audioPlayer;

        public SettingsController(
            Config.BotConfiguration botConfig,
            Audio.IMicrophoneHandler microphoneHandler,
            Audio.IAudioPlayer audioPlayer)
        {
            this.botConfig = botConfig;
            this.microphoneHandler = microphoneHandler;
            this.audioPlayer = audioPlayer;
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

        [HttpGet]
        public ActionResult<ErrHEnabled> ErrorHEnabled() =>
            new ErrHEnabled(botConfig.CommandConfiguration.EnableErrorHandling);

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult ErrorHEnabled(ErrHEnabled eHEnabled)
        {
            //Set CompressorConfig
            botConfig.CommandConfiguration.EnableErrorHandling = eHEnabled.Enabled;

            //Save
            botConfig.Serialize();

            return Ok();
        }

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
        public record ErrHEnabled(bool Enabled);
        public record DeviceRequest(string Device);
        public record MidiRequest(string Effect, int Channel);
    }
}
