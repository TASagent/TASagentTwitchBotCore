namespace TASagentTwitchBot.Core.TTS.Parsing.Tokens;

public abstract class CommandUnit : ParsingUnit
{
    public CommandUnit(int position)
        : base(position)
    {
    }

    public static bool ParseAndSubstitute(List<ParsingUnit> tokens, int i)
    {
        if (tokens[i] is not ApparentCommandToken apparentCommand)
        {
            throw new TTSParsingException(tokens[i].position, $"Expected ApparentCommandToken, received: {tokens[i]}");
        }

        switch (apparentCommand.command.ToLowerInvariant())
        {
            case "pause": return PauseCommandUnit.ParseAndSubstitutePause(tokens, i);

            default:
                return false;
        }
    }

    public abstract override string ToString();
}

public class PauseCommandUnit : CommandUnit
{
    public readonly int duration;
    public PauseCommandUnit(int position, int duration)
        : base(position)
    {
        this.duration = duration;
    }

    public static bool ParseAndSubstitutePause(List<ParsingUnit> tokens, int i)
    {
        int duration = 1000;

        if (tokens[i + 1] is MarkupToken openParen && openParen.markup == TTSMarkup.OpenParen &&
            tokens[i + 2] is StringUnit durationToken && int.TryParse(durationToken.text, out int tempDuration) &&
            tokens[i + 3] is MarkupToken closeParen && closeParen.markup == TTSMarkup.CloseParen)
        {
            //Arguments provided and parsed
            duration = Math.Clamp(tempDuration, 0, 10_000);

            //Remove consumed tokens
            tokens.RemoveRange(i + 1, 3);
        }

        //Substitute command token
        tokens[i] = new PauseCommandUnit(tokens[i].position, duration);

        return true;
    }

    public override string ToString() => $"!pause({duration})";
}
