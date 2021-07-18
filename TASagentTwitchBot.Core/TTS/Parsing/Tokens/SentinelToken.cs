namespace TASagentTwitchBot.Core.TTS.Parsing.Tokens
{
    public class SentinelToken : ParsingUnit
    {
        public SentinelToken(int position)
            : base(position)
        {

        }

        public override string ToString() => "$";
    }

}
