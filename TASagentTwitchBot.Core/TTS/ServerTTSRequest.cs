namespace TASagentTwitchBot.Core.TTS
{
    public record ServerTTSRequest(
        string RequestIdentifier,
        string Ssml,
        TTSVoice Voice,
        TTSPitch Pitch,
        TTSSpeed Speed);
}
