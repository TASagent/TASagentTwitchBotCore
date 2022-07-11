
using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Audio.Effects;

namespace TASagentTwitchBot.Core.TTS;

[AutoRegister]
public interface ITTSRenderer
{
    bool IsTTSVoiceValid(string voice);
    TTSVoiceInfo? GetTTSVoiceInfo(string voice);
    Task<bool> SetTTSEnabled(bool enabled);

    Task<AudioRequest?> TTSRequest(
        Commands.AuthorizationLevel authorizationLevel,
        string voicePreference,
        TTSPitch pitchPreference,
        TTSSpeed speedPreference,
        Effect effectsChain,
        string ttsText);
}
