namespace TASagentTwitchBot.Core.TTS.Parsing.Tokens;

public class RenderModeModifier : ParsingUnit
{
    public readonly TTSRenderMode renderMode;
    public readonly bool enable;

    public RenderModeModifier(
        int position,
        TTSRenderMode renderMode,
        bool enable)
        : base(position)
    {
        this.renderMode = renderMode;
        this.enable = enable;
    }
}
