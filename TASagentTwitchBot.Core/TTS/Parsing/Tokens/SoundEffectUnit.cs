using TASagentTwitchBot.Core.Audio;

namespace TASagentTwitchBot.Core.TTS.Parsing.Tokens
{
    /// <summary>
    /// Sound effect embedded in TTS
    /// </summary>
    public class SoundEffectUnit : ParsingUnit
    {
        public readonly SoundEffect soundEffect;

        public SoundEffectUnit(int position, SoundEffect soundEffect)
            : base(position)
        {
            this.soundEffect = soundEffect;
        }

        public override string ToString() => $"/{{{soundEffect.Name}}}";
    }
}
