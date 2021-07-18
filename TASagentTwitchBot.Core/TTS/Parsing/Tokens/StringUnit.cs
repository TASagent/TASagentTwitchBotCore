namespace TASagentTwitchBot.Core.TTS.Parsing.Tokens
{
    /// <summary>
    /// Accumulated string literal
    /// </summary>
    public class StringUnit : ParsingUnit
    {
        public readonly string text;
        public StringUnit(int position, string value)
            : base(position)
        {
            this.text = value;
        }

        public override string ToString() => text;
    }
}
