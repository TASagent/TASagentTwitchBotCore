using System;
using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/SFX/[action]")]
    public class SFXController : ControllerBase
    {
        private readonly ISoundEffectSystem soundEffectSystem;
        private readonly Notifications.IActivityDispatcher activityDispatcher;
        private readonly IAudioPlayer audioPlayer;
        private readonly Config.BotConfiguration botConfig;

        public SFXController(
            ISoundEffectSystem soundEffectSystem,
            IAudioPlayer audioPlayer,
            Notifications.IActivityDispatcher activityDispatcher,
            Config.IBotConfigContainer botConfigContainer)
        {
            this.soundEffectSystem = soundEffectSystem;
            this.audioPlayer = audioPlayer;
            this.activityDispatcher = activityDispatcher;
            botConfig = botConfigContainer.BotConfig;
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Privileged)]
        public IActionResult PlayImmediate(SoundEffect request)
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

            Audio.SoundEffect soundEffect = soundEffectSystem.GetSoundEffectByAlias(sfxString);

            if (soundEffect is null)
            {
                return BadRequest();
            }

            audioPlayer.DemandPlayAudioImmediate(new SoundEffectRequest(soundEffect));

            return Ok();
        }

        [HttpPost]
        [AuthRequired(AuthDegree.Privileged)]
        public IActionResult Skip()
        {
            activityDispatcher.Skip();
            return Ok();
        }
    }

    public record SoundEffect(string Effect);
}
