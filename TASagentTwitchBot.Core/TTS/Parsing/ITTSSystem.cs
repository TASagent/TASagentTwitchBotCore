using TASagentTwitchBot.Core.Audio.Effects;

namespace TASagentTwitchBot.Core.TTS.Parsing;

[AutoRegister]
public interface ITTSSystem
{
    string SystemName { get; }
    IEnumerable<string> GetVoices();
    TTSSystemRenderer CreateRenderer(string voice, TTSPitch pitch, TTSSpeed speed, Effect effectsChain);
    string GetDefaultVoice();
    TTSVoiceInfo GetTTSVoiceInfo(string voiceString);
}
