namespace TASagentTwitchBot.Core.TTS.Parsing.Tokens;

public class MarkupToken : ParsingUnit
{
    public readonly TTSMarkup markup;

    public MarkupToken(int position, TTSMarkup markup)
        : base(position)
    {
        this.markup = markup;
    }

    public override string ToString() =>
        markup switch
        {
            TTSMarkup.Underscore => "_",
            TTSMarkup.Tilde => "~",

            TTSMarkup.Plus => "+",
            TTSMarkup.Minus => "-",
            TTSMarkup.Asterisk => "*",
            TTSMarkup.Slash => "/",
            TTSMarkup.Carrot => "^",

            TTSMarkup.Comma => ",",
            TTSMarkup.Period => ".",
            TTSMarkup.Bang => "!",
            TTSMarkup.QuestionMark => "?",

            TTSMarkup.Octothorpe => "#",
            TTSMarkup.DollarSign => "$",
            TTSMarkup.PercentSign => "%",
            TTSMarkup.Ampersand => "&",

            TTSMarkup.DoubleQuote => "\"",
            TTSMarkup.SingleQuote => "'",

            TTSMarkup.OpenAngleBracket => "<",
            TTSMarkup.CloseAngleBracket => ">",

            TTSMarkup.OpenParen => "(",
            TTSMarkup.CloseParen => ")",

            TTSMarkup.OpenCurlyBoi => "{",
            TTSMarkup.CloseCurlyBoi => "}",

            TTSMarkup.OpenBracket => "[",
            TTSMarkup.CloseBracket => "]",

            _ => throw new Exception($"Unexpected TTSMarkup Value: {markup}"),
        };
}
