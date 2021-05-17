using System;
using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/Mic/[action]")]
    [ConditionalFeature("Audio")]
    public class MicController : ControllerBase
    {
        private readonly Config.IBotConfigContainer botConfigContainer;
        private readonly Audio.IMicrophoneHandler microphoneHandler;
        private readonly Audio.Effects.IAudioEffectSystem audioEffectSystem;

        public MicController(
            Config.IBotConfigContainer botConfigContainer,
            Audio.IMicrophoneHandler microphoneHandler,
            Audio.Effects.IAudioEffectSystem audioEffectSystem)
        {
            this.botConfigContainer = botConfigContainer;
            this.microphoneHandler = microphoneHandler;
            this.audioEffectSystem = audioEffectSystem;
        }

        [HttpGet]
        public ActionResult<MicEnabled> Enabled() =>
            new MicEnabled(botConfigContainer.BotConfig.MicConfiguration.Enabled);

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult Enabled(MicEnabled micEnabled)
        {
            //Set CompressorConfig
            botConfigContainer.BotConfig.MicConfiguration.Enabled = micEnabled.Enabled;

            //Save
            botConfigContainer.SerializeData();

            //Update Microphone
            microphoneHandler.ResetVoiceStream();
            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult Reset()
        {
            //Update Microphone
            microphoneHandler.ResetVoiceStream();
            return Ok();
        }

        [HttpPost]
        [AuthRequired]
        public IActionResult Effect(MicEffect request)
        {
            Audio.Effects.Effect effect;

            if (string.IsNullOrEmpty(request.Effect) || request.Effect.ToLower() == "none")
            {
                effect = new Audio.Effects.NoEffect();
            }
            else
            {
                effect = audioEffectSystem.SafeParse(request.Effect);
            }

            if (effect is null)
            {
                return BadRequest();
            }

            microphoneHandler.UpdateVoiceEffect(effect);
            return Ok();
        }

        [HttpGet]
        public ActionResult<MicEffect> Effect()
        {
            string effectString = microphoneHandler.GetCurrentEffect();

            if (string.IsNullOrEmpty(effectString))
            {
                effectString = "None";
            }

            return new MicEffect(effectString);
        }

        [HttpPost]
        [AuthRequired]
        public IActionResult PitchFactor(MicPitchFactor request)
        {
            if (double.IsNaN(request.Factor))
            {
                return BadRequest();
            }

            microphoneHandler.SetPitch(request.Factor);

            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult Compressor(Config.CompressorConfiguration request)
        {
            //Set CompressorConfig
            botConfigContainer.BotConfig.MicConfiguration.CompressorConfiguration = request;

            //Save
            botConfigContainer.SerializeData();

            //Update Microphone
            microphoneHandler.ResetVoiceStream();
            return Ok();
        }

        [HttpGet]
        public ActionResult<Config.CompressorConfiguration> Compressor() =>
            botConfigContainer.BotConfig.MicConfiguration.CompressorConfiguration;

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult Expander(Config.ExpanderConfiguration request)
        {
            //Set CompressorConfig
            botConfigContainer.BotConfig.MicConfiguration.ExpanderConfiguration = request;

            //Save
            botConfigContainer.SerializeData();

            //Update Microphone
            microphoneHandler.ResetVoiceStream();
            return Ok();
        }

        [HttpGet]
        public ActionResult<Config.ExpanderConfiguration> Expander() =>
            botConfigContainer.BotConfig.MicConfiguration.ExpanderConfiguration;

        [HttpPost]
        [AuthRequired(AuthDegree.Admin)]
        public IActionResult NoiseGate(Config.NoiseGateConfiguration request)
        {
            //Set CompressorConfig
            botConfigContainer.BotConfig.MicConfiguration.NoiseGateConfiguration = request;

            //Save
            botConfigContainer.SerializeData();

            //Update Microphone
            microphoneHandler.ResetVoiceStream();
            return Ok();
        }

        [HttpGet]
        public ActionResult<Config.NoiseGateConfiguration> NoiseGate() =>
            botConfigContainer.BotConfig.MicConfiguration.NoiseGateConfiguration;
    }

    public record MicEnabled(bool Enabled);
    public record MicEffect(string Effect);
    public record MicPitchFactor(double Factor);
}
