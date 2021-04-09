using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.TTS;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/TTS/[action]")]
    [ConditionalFeature("TTS")]
    public class TTSController : ControllerBase
    {
        private readonly IAudioEffectSystem audioEffectSystem;
        private readonly Notifications.ITTSHandler ttsHandler;

        public TTSController(
            IAudioEffectSystem audioEffectSystem,
            Notifications.ITTSHandler ttsHandler)
        {
            this.audioEffectSystem = audioEffectSystem;
            this.ttsHandler = ttsHandler;
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Privileged)]
        public IActionResult Play(
            TTSRequest request)
        {
            if (string.IsNullOrEmpty(request.Voice) ||
                string.IsNullOrEmpty(request.Pitch) ||
                string.IsNullOrEmpty(request.Text))
            {
                return BadRequest();
            }

            string user = request.User;

            if (string.IsNullOrEmpty(user))
            {
                user = "Admin";
            }
            Effect effect;

            if (string.IsNullOrEmpty(request.Effect) || request.Effect.ToLower() == "none")
            {
                effect = new NoEffect();
            }
            else
            {
                effect = audioEffectSystem.Parse(request.Effect);
            }

            ttsHandler.HandleTTS(
                user: new Database.User()
                    {
                        AuthorizationLevel = Commands.AuthorizationLevel.Elevated,
                        TwitchUserName = user,
                        TTSVoicePreference = request.Voice.TranslateTTSVoice(),
                        TTSPitchPreference = request.Pitch.TranslateTTSPitch(),
                        TTSEffectsChain = effect.GetEffectsChain()
                    },
                message: request.Text,
                approved: true);

            return Ok();
        }
    }

    public record TTSRequest(string Voice, string Pitch, string Effect, string Text, string User);
}
