namespace TASagentTwitchBot.Core.TTS.Parsing.Tokens;

public class ApparentCommandToken : ParsingUnit
{
    public readonly string command;
    public ApparentCommandToken(int position, string command)
        : base(position)
    {
        this.command = command;
    }

    public override string ToString() => $"!{command}";
}
