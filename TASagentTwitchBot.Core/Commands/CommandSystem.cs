using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.Commands
{
    public class CommandSystem
    {
        private readonly ICommunication communication;
        private readonly ErrorHandler errorHandler;

        //Command Handlers
        private readonly ICommandContainer[] commandContainers;

        //Command Cache
        private readonly Dictionary<string, CommandHandler> commandHandlers = new Dictionary<string, CommandHandler>();
        private readonly Dictionary<string, HelpFunction> helpFunctions = new Dictionary<string, HelpFunction>();
        private readonly Dictionary<string, SetFunction> setFunctions = new Dictionary<string, SetFunction>();

        private readonly Dictionary<string, ResponseHandler> whisperHandlers = new Dictionary<string, ResponseHandler>();

        public CommandSystem(
            ICommunication communication,
            ErrorHandler errorHandler,
            IEnumerable<ICommandContainer> commandContainers)
        {
            this.communication = communication;
            this.errorHandler = errorHandler;

            this.commandContainers = commandContainers.ToArray();

            foreach (ICommandContainer commandContainer in this.commandContainers)
            {
                commandContainer.RegisterCommands(commandHandlers, helpFunctions, setFunctions);
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

            switch (command)
            {
                case "help":
                case "commands":
                case "man":
                    //Get Help
                    if (splitMessage.Length == 1)
                    {
                        //General Help
                        communication.SendPublicChatMessage($"Commands: {string.Join(", ", GetAllPublicCommandStrings())}. For more information, visit https://TASagent.github.io/");
                    }
                    else if (helpFunctions.ContainsKey(splitMessage[1].ToLowerInvariant()))
                    {
                        string[] remainingCommand = null;

                        if (splitMessage.Length > 2)
                        {
                            remainingCommand = splitMessage[2..];
                        }
                        else
                        {
                            remainingCommand = Array.Empty<string>();
                        }

                        string helpString = helpFunctions[splitMessage[1].ToLowerInvariant()](chatter, remainingCommand);

                        communication.SendPublicChatMessage(helpString);
                    }
                    else
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unrecognized command for Help: \"{string.Join(' ', splitMessage[1..])}\".");
                    }
                    break;

                case "set":
                    //All Set Commands
                    if (splitMessage.Length == 1)
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Set what?");
                    }
                    else if (setFunctions.ContainsKey(splitMessage[1].ToLowerInvariant()))
                    {
                        string[] remainingCommand = null;

                        if (splitMessage.Length > 2)
                        {
                            remainingCommand = splitMessage[2..];
                        }
                        else
                        {
                            remainingCommand = Array.Empty<string>();
                        }

                        await setFunctions[splitMessage[1].ToLowerInvariant()](chatter, remainingCommand);
                    }
                    else
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unrecognized Set target \"{splitMessage[0].ToLowerInvariant()}\".");
                    }
                    break;

                default:
                    //Roll over to standard command system
                    if (commandHandlers.ContainsKey(command))
                    {
                        string[] remainingCommand = null;

                        if (splitMessage.Length > 1)
                        {
                            remainingCommand = splitMessage[1..];
                        }
                        else
                        {
                            remainingCommand = Array.Empty<string>();
                        }

                        //Invoke handler
                        await commandHandlers[command](chatter, remainingCommand);
                    }
                    else
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, You wot m8‽ (!{command})");
                        communication.SendDebugMessage($"Unrecognized command: {cleanMessage}");
                    }
                    break;
            }
        }

        public async Task HandleWhisper(IRC.TwitchChatter chatter)
        {
            if (chatter.User.AuthorizationLevel == AuthorizationLevel.Restricted)
            {
                //Restricted users get NOTHING
                return;
            }

            if (whisperHandlers.ContainsKey(chatter.User.TwitchUserName))
            {
                ResponseHandler responseHandler = whisperHandlers[chatter.User.TwitchUserName];
                whisperHandlers.Remove(chatter.User.TwitchUserName);
                await responseHandler(chatter);
            }
            else
            {
                communication.SendChatWhisper(chatter.User.TwitchUserName, $"You wot m8‽");
            }
        }

        public bool IsCommand(string message) => message.TrimStart().StartsWith('!');
    }
}
