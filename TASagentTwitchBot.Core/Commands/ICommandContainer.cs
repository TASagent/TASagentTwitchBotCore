namespace TASagentTwitchBot.Core.Commands;

public delegate Task CommandHandler(IRC.TwitchChatter chatter, string[] remainingCommand);
public delegate Task SetFunction(IRC.TwitchChatter chatter, string[] remainingCommand);
public delegate Task GetFunction(IRC.TwitchChatter chatter, string[] remainingCommand);
public delegate string HelpFunction(IRC.TwitchChatter chatter, string[] remainingCommand);

public delegate Task ResponseHandler(IRC.TwitchChatter chatter);

public interface ICommandContainer
{
    void RegisterCommands(ICommandRegistrar commandRegistrar);

    IEnumerable<string> GetPublicCommands();
}
