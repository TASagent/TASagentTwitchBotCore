using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Audio.Effects;

namespace TASagentTwitchBot.Core.TTS
{
    public class DisabledTTSRenderer : ITTSRenderer
    {
        Task<AudioRequest> ITTSRenderer.TTSRequest(TTSVoice voicePreference, TTSPitch pitchPreference, Effect effectsChain, string ttsText) => Task.FromResult<AudioRequest>(null);
        Task<AudioRequest> ITTSRenderer.TTSRequest(TTSVoice voicePreference, TTSPitch pitchPreference, Effect effectsChain, string[] splitTTSText) => Task.FromResult<AudioRequest>(null);
    }
}
