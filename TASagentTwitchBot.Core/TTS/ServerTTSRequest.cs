namespace TASagentTwitchBot.Core.TTS;

public record ServerTTSRequest(
    string RequestIdentifier,
    string Ssml,
    string Voice,
    TTSPitch Pitch,
    TTSSpeed Speed);
