namespace TASagentTwitchBot.Core.TTS.Parsing.Tokens
{
    public abstract class ParsingUnit
    {
        public int position;

        public ParsingUnit(int position)
        {
            this.position = position;
        }
    }
}
