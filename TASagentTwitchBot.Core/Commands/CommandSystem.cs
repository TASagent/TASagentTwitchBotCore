using System.Diagnostics;

namespace TASagentTwitchBot.Core.Commands;

public class CommandSystem : ICommandRegistrar
{
    private readonly Config.BotConfiguration botConfig;
    private readonly ICommunication communication;
    private readonly ErrorHandler errorHandler;

    //Command Handlers
    private readonly ICommandContainer[] commandContainers;

    //Command Cache
    private readonly Dictionary<string, CommandHandler> commandHandlers = new Dictionary<string, CommandHandler>();
    private readonly Dictionary<string, CommandHandler> customCommandHandlers = new Dictionary<string, CommandHandler>();
    private readonly Dictionary<string, HelpFunction> helpFunctions = new Dictionary<string, HelpFunction>();
    private readonly Dictionary<(string command, string scope), CommandHandler> scopedHandlers = new Dictionary<(string command, string scope), CommandHandler>();
    private readonly HashSet<string> scopedCommands = new HashSet<string>();

    private readonly Dictionary<string, ResponseHandler> whisperHandlers = new Dictionary<string, ResponseHandler>();

    public CommandSystem(
        Config.BotConfiguration botConfig,
        ICommunication communication,
        ErrorHandler errorHandler,
        IEnumerable<ICommandContainer> commandContainers)
    {
        this.botConfig = botConfig;
        this.communication = communication;
        this.errorHandler = errorHandler;

        this.commandContainers = commandContainers.ToArray();

        foreach (ICommandContainer commandContainer in this.commandContainers)
        {
            commandContainer.RegisterCommands(this);
        }

        communication.ReceiveMessageHandlers += HandleChatMessage;
    }

    private async void HandleChatMessage(IRC.TwitchChatter chatter)
    {
        try
        {
            if (chatter.Whisper)
            {
                await HandleWhisper(chatter);
            }
            else if (IsCommand(chatter.Message))
            {
                await HandleCommand(chatter);
            }
        }
        catch (Exception ex)
        {
            errorHandler.LogSystemException(ex);
        }
    }

    private IEnumerable<string> GetAllPublicCommandStrings()
    {
        yield return "!help";

        foreach (ICommandContainer commandContainer in commandContainers)
        {
            foreach (string commandString in commandContainer.GetPublicCommands())
            {
                yield return $"!{commandString}";
            }
        }
    }

    protected virtual string GetGenericHelpMessage() =>
        $"Commands: {string.Join(", ", GetAllPublicCommandStrings())}. {botConfig.CommandConfiguration.GenericHelpMessage}";

    protected virtual string GetUnrecognizedCommandMessage(IRC.TwitchChatter chatter, string[] splitMessage) =>
        $"@{chatter.User.TwitchUserName}, {botConfig.CommandConfiguration.UnknownCommandResponse} ({splitMessage[0]})";

    public async Task HandleCommand(IRC.TwitchChatter chatter)
    {
        if (chatter.User.AuthorizationLevel == AuthorizationLevel.Restricted)
        {
            //Restricted users get NOTHING
            return;
        }

        string[] splitMessage = chatter.Message.Trim().Split(' ', options: StringSplitOptions.RemoveEmptyEntries);

        string command = splitMessage[0][1..].ToLowerInvariant();

        //Check Help Commands
        if (botConfig.CommandConfiguration.HelpEnabled &&
            (command == "help" ||
            command == "commands" ||
            command == "man"))
        {
            //Handle Help Commands
            if (splitMessage.Length == 1)
            {
                //General Help
                string response = GetGenericHelpMessage();
                if (!string.IsNullOrEmpty(response))
                {
                    communication.SendPublicChatMessage(response);
                }
            }
            else if (helpFunctions.TryGetValue(splitMessage[1].ToLowerInvariant(), out HelpFunction? helpFunction))
            {
                string helpString = helpFunction(chatter, GetRemainingCommand(splitMessage, 2));
                communication.SendPublicChatMessage(helpString);
            }
            else
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unrecognized command for Help: \"{string.Join(' ', splitMessage[1..])}\".");
            }

            return;
        }

        //Try Scoped Commands
        if (botConfig.CommandConfiguration.ScopedEnabled && scopedCommands.Contains(command) && splitMessage.Length > 1)
        {
            if (scopedHandlers.TryGetValue((command, splitMessage[1].ToLowerInvariant()), out CommandHandler? handler))
            {
                await handler(chatter, GetRemainingCommand(splitMessage, 2));
                return;
            }
        }

        //Try standard global commands
        if (commandHandlers.TryGetValue(command, out CommandHandler? commandHandler))
        {
            //Invoke handler
            await commandHandler(chatter, GetRemainingCommand(splitMessage, 1));
            return;
        }
        
        //Try custom commands
        if (customCommandHandlers.TryGetValue(command, out CommandHandler? customCommandHandler))
        {
            //Invoke handler
            await customCommandHandler(chatter, GetRemainingCommand(splitMessage, 1));
            return;
        }
        
        if (botConfig.CommandConfiguration.GlobalErrorHandlingEnabled)
        {
            string response = GetUnrecognizedCommandMessage(chatter, splitMessage);
            if (!string.IsNullOrEmpty(response))
            {
                communication.SendPublicChatMessage(response);
            }

            communication.SendDebugMessage($"Unrecognized command: {chatter.Message}");
        }
    }

    public virtual async Task HandleWhisper(IRC.TwitchChatter chatter)
    {
        if (chatter.User.AuthorizationLevel == AuthorizationLevel.Restricted)
        {
            //Restricted users get NOTHING
            return;
        }

        if (whisperHandlers.TryGetValue(chatter.User.TwitchUserName, out ResponseHandler? responseHandler))
        {
            whisperHandlers.Remove(chatter.User.TwitchUserName);
            await responseHandler(chatter);
        }
        else if (botConfig.CommandConfiguration.GlobalErrorHandlingEnabled)
        {
            communication.SendChatWhisper(chatter.User.TwitchUserName, botConfig.CommandConfiguration.UnknownCommandResponse);
        }
    }

    public virtual bool IsCommand(string message) => message.TrimStart().StartsWith('!');

    private static string[] GetRemainingCommand(string[] splitMessage, int fromIndex)
    {
        if (splitMessage.Length <= fromIndex)
        {
            return Array.Empty<string>();
        }

        return splitMessage[fromIndex..];
    }

    #region ICommandRegistrar

    bool ICommandRegistrar.ContainsGlobalCommand(string command) => commandHandlers.ContainsKey(command.ToLower());
    void ICommandRegistrar.RegisterGlobalCommand(string command, CommandHandler handler) => commandHandlers.Add(command.ToLower(), handler);

    bool ICommandRegistrar.ContainsScopedCommand(string command, string scope) => scopedHandlers.ContainsKey((command.ToLower(), scope.ToLower()));
    void ICommandRegistrar.RegisterScopedCommand(string command, string scope, CommandHandler handler)
    {
        scopedCommands.Add(command.ToLower());
        scopedHandlers.Add((command.ToLower(), scope.ToLower()), handler);
    }

    void ICommandRegistrar.RegisterHelpCommand(string command, HelpFunction handler) => helpFunctions.Add(command.ToLower(), handler);

    bool ICommandRegistrar.ContainsCustomCommand(string command) => customCommandHandlers.ContainsKey(command.ToLower());
    void ICommandRegistrar.RegisterCustomCommand(string command, CommandHandler handler) => customCommandHandlers.Add(command.ToLower(), handler);
    bool ICommandRegistrar.RemoveCustomCommand(string command) => customCommandHandlers.Remove(command.ToLower());

    #endregion ICommandRegistrar
}
