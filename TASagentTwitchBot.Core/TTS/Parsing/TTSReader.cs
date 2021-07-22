using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using TASagentTwitchBot.Core.TTS.Parsing.Tokens;

namespace TASagentTwitchBot.Core.TTS.Parsing
{
    public class TTSReader : IDisposable
    {
        private readonly TextReader textReader;
        private readonly StringBuilder wordBuilder = new StringBuilder();

        private int position = 0;

        /// <summary>Indicates whether there are still characters to be read.</summary>
        public bool CanRead => textReader.Peek() != -1;

        public TTSReader(string text)
        {
            textReader = new StringReader(text);
        }

        public IEnumerable<ParsingUnit> GetTokens()
        {
            while (CanRead)
            {
                yield return ReadNextToken();
            }

            yield return new SentinelToken(position);
        }
         
        private ParsingUnit ReadNextToken()
        {
            int startingPosition = position;
            char next = Read();

            switch (next)
            {
                //Escape Sequence
                case '\\':
                    if (IsSpecialCharacter(Peek()))
                    {
                        //Escaped Character
                        //Advance reader
                        return new StringUnit(startingPosition, $"{Read()}");
                    }
                    else
                    {
                        //Unexpected escape character
                        //Return just escape character without advancing
                        return new StringUnit(startingPosition, "\\");
                    }

                //Markup
                case '_': return new MarkupToken(startingPosition, TTSMarkup.Underscore);
                case '~': return new MarkupToken(startingPosition, TTSMarkup.Tilde);

                case '+': return new MarkupToken(startingPosition, TTSMarkup.Plus);
                case '-': return new MarkupToken(startingPosition, TTSMarkup.Minus);
                case '*': return new MarkupToken(startingPosition, TTSMarkup.Asterisk);
                case '^': return new MarkupToken(startingPosition, TTSMarkup.Carrot);

                case ',': return new MarkupToken(startingPosition, TTSMarkup.Comma);
                case '.': return new MarkupToken(startingPosition, TTSMarkup.Period);
                case '?': return new MarkupToken(startingPosition, TTSMarkup.QuestionMark);

                case '#': return new MarkupToken(startingPosition, TTSMarkup.Octothorpe);
                case '$': return new MarkupToken(startingPosition, TTSMarkup.DollarSign);
                case '%': return new MarkupToken(startingPosition, TTSMarkup.PercentSign);
                case '&': return new MarkupToken(startingPosition, TTSMarkup.Ampersand);

                case '"': return new MarkupToken(startingPosition, TTSMarkup.DoubleQuote);
                case '\'': return new MarkupToken(startingPosition, TTSMarkup.SingleQuote);

                case '<': return new MarkupToken(startingPosition, TTSMarkup.OpenAngleBracket);
                case '>': return new MarkupToken(startingPosition, TTSMarkup.CloseAngleBracket);

                case '(': return new MarkupToken(startingPosition, TTSMarkup.OpenParen);
                case ')': return new MarkupToken(startingPosition, TTSMarkup.CloseParen);

                case '[': return new MarkupToken(startingPosition, TTSMarkup.OpenBracket);
                case ']': return new MarkupToken(startingPosition, TTSMarkup.CloseBracket);

                case '{': return new MarkupToken(startingPosition, TTSMarkup.OpenCurlyBoi);
                case '}': return new MarkupToken(startingPosition, TTSMarkup.CloseCurlyBoi);

                case '!':
                    {
                        wordBuilder.Clear();

                        while (CanRead &&
                            (!IsSpecialCharacter(next = Peek()) || (next == '_' && wordBuilder.Length > 0)) &&
                            !char.IsWhiteSpace(next))
                        {
                            Read();
                            wordBuilder.Append(next);
                        }

                        if (wordBuilder.Length == 0)
                        {
                            return new StringUnit(startingPosition, "!");
                        }
                        else
                        {
                            return new ApparentCommandToken(startingPosition, wordBuilder.ToString());
                        }
                    }

                case '/':
                    {
                        wordBuilder.Clear();

                        while (CanRead &&
                            (!IsSpecialCharacter(next = Peek()) || (next == '_' && wordBuilder.Length > 0)) &&
                            !char.IsWhiteSpace(next))
                        {
                            Read();
                            wordBuilder.Append(next);
                        }

                        if (wordBuilder.Length == 0)
                        {
                            return new StringUnit(startingPosition, "/");
                        }
                        else
                        {
                            return new ApparentSoundEffectToken(startingPosition, wordBuilder.ToString());
                        }
                    }

                default:
                    {
                        wordBuilder.Clear();
                        wordBuilder.Append(next);

                        while (CanRead &&
                            !IsSpecialCharacter(next = Peek()))
                        {
                            Read();
                            wordBuilder.Append(next);
                        }

                        return new StringUnit(startingPosition, wordBuilder.ToString());
                    }
            }
        }

        public char Read()
        {
            int next = textReader.Read();

            if (next == -1)
            {
                throw new TTSParsingException(position + 1, "Tried to Read past the end of the input text");
            }

            position++;

            return (char)next;
        }

        public char Peek()
        {
            int next = textReader.Peek();

            if (next == -1)
            {
                throw new TTSParsingException(position + 1, "Tried to Peek past the end of the input text");
            }

            return (char)next;
        }

        private static bool IsSpecialCharacter(char c) =>
            c switch
            {
                '\\' or '_' or '~' or
                '+' or '-' or '*' or '/' or '^' or
                ',' or '.' or '!' or '?' or
                '#' or '$' or '%' or '&' or
                '\'' or '"' or
                '<' or '<' or
                '(' or ')' or
                '[' or ']' or
                '{' or '}' => true,
                _ => false,
            };

        #region IDisposable Support

        // To detect redundant calls
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    textReader.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
