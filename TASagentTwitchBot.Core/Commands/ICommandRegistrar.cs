namespace TASagentTwitchBot.Core.Commands;

public interface ICommandRegistrar
{
    bool ContainsGlobalCommand(string command);
    void RegisterGlobalCommand(string command, CommandHandler handler);

    bool ContainsScopedCommand(string command, string scope);
    void RegisterScopedCommand(string command, string scope, CommandHandler handler);

    void RegisterHelpCommand(string command, HelpFunction handler);

    bool ContainsCustomCommand(string command);
    void RegisterCustomCommand(string command, CommandHandler handler);
    bool RemoveCustomCommand(string command);
}
