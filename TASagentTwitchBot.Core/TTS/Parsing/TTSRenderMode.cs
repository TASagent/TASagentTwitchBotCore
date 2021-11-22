namespace TASagentTwitchBot.Core.TTS.Parsing;

[System.Flags]
public enum TTSRenderMode
{
    Normal = 0,
    Whisper = 1,
    Emphasis = 1 << 1,
    Censor = 1 << 2,

    MASK = Whisper | Emphasis | Censor
}
