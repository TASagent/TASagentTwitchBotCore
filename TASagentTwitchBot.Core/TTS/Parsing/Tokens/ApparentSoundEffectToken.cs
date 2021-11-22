namespace TASagentTwitchBot.Core.TTS.Parsing.Tokens;

/// <summary>
/// Sound effect embedded in TTS
/// </summary>
public class ApparentSoundEffectToken : ParsingUnit
{
    public readonly string soundEffect;

    public ApparentSoundEffectToken(int position, string soundEffect)
        : base(position)
    {
        this.soundEffect = soundEffect;
    }

    public override string ToString() => $"/{soundEffect}";
}
