using System;

using TASagentTwitchBot.Core.TTS.Parsing.Tokens;

namespace TASagentTwitchBot.Core.TTS.Parsing
{
    public class TTSParsingException : Exception
    {
        public int position;

        public TTSParsingException(ParsingUnit source, string message)
            : base(message)
        {
            position = source.position;
        }

        public TTSParsingException(int position, string message)
            : base(message)
        {
            this.position = position;
        }
    }

}
