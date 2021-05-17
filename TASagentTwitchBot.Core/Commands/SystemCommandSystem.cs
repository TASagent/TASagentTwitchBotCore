using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace TASagentTwitchBot.Core.Commands
{
    public class SystemCommandSystem : ICommandContainer
    {
        private readonly ICommunication communication;
        private readonly ApplicationManagement applicationManagement;

        public SystemCommandSystem(
            ICommunication communication,
            ApplicationManagement applicationManagement)
        {
            this.communication = communication;
            this.applicationManagement = applicationManagement;
        }

        public void RegisterCommands(
            Dictionary<string, CommandHandler> commands,
            Dictionary<string, HelpFunction> helpFunctions,
            Dictionary<string, SetFunction> setFunctions,
            Dictionary<string, GetFunction> getFunctions)
        {
            commands.Add("quit", QuitHandler);
        }

        public IEnumerable<string> GetPublicCommands()
        {
            yield break;
        }

        private async Task QuitHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
            {
                communication.SendPublicChatMessage($"You are not authorized to disconnect me, @{chatter.User.TwitchUserName}.");
                return;
            }

            communication.SendPublicChatMessage($"Alright @{chatter.User.TwitchUserName}, I'm heading out. Goodbye!");
            await Task.Delay(2000);
            applicationManagement.TriggerExit();
        }
    }
}
