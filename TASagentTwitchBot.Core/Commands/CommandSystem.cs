namespace TASagentTwitchBot.Core.Commands;

public class CommandSystem
{
    private readonly Config.BotConfiguration botConfig;
    private readonly ICommunication communication;
    private readonly ErrorHandler errorHandler;

    //Command Handlers
    private readonly ICommandContainer[] commandContainers;

    //Command Cache
    private readonly Dictionary<string, CommandHandler> commandHandlers = new Dictionary<string, CommandHandler>();
    private readonly Dictionary<string, HelpFunction> helpFunctions = new Dictionary<string, HelpFunction>();
    private readonly Dictionary<string, SetFunction> setFunctions = new Dictionary<string, SetFunction>();
    private readonly Dictionary<string, GetFunction> getFunctions = new Dictionary<string, GetFunction>();

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
            commandContainer.RegisterCommands(commandHandlers, helpFunctions, setFunctions, getFunctions);
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

        string cleanMessage = chatter.Message.Trim();

        string[] splitMessage = cleanMessage.Split(' ', options: StringSplitOptions.RemoveEmptyEntries);

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

        //Check Set Commands
        if (botConfig.CommandConfiguration.SetEnabled && command == "set")
        {
            //Handle Set Commands
            if (splitMessage.Length == 1)
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Set what?");
            }
            else if (setFunctions.TryGetValue(splitMessage[1].ToLowerInvariant(), out SetFunction? setFunction))
            {
                await setFunction(chatter, GetRemainingCommand(splitMessage, 2));
            }
            else
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unrecognized Set target \"{splitMessage[0].ToLowerInvariant()}\".");
            }

            return;
        }

        //Check Get Commands
        if (botConfig.CommandConfiguration.GetEnabled && command == "get")
        {
            //Handle Get Commands
            if (splitMessage.Length == 1)
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Get what?");
            }
            else if (getFunctions.TryGetValue(splitMessage[1].ToLowerInvariant(), out GetFunction? getFunction))
            {
                await getFunction(chatter, GetRemainingCommand(splitMessage, 2));
            }
            else
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unrecognized Get target \"{splitMessage[0].ToLowerInvariant()}\".");
            }

            return;
        }

        //Roll over to standard command system
        if (commandHandlers.TryGetValue(command, out CommandHandler? commandHandler))
        {
            //Invoke handler
            await commandHandler(chatter, GetRemainingCommand(splitMessage, 1));
        }
        else if (botConfig.CommandConfiguration.EnableErrorHandling)
        {
            string response = GetUnrecognizedCommandMessage(chatter, splitMessage);
            if (!string.IsNullOrEmpty(response))
            {
                communication.SendPublicChatMessage(response);
            }

            communication.SendDebugMessage($"Unrecognized command: {cleanMessage}");
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
        else if (botConfig.CommandConfiguration.EnableErrorHandling)
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
}
