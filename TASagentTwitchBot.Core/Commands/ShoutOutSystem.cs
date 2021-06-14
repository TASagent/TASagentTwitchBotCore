using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using TASagentTwitchBot.Core.Database;
using TASagentTwitchBot.Core.API.Twitch;

namespace TASagentTwitchBot.Core.Commands
{
    public class ShoutOutSystem : ICommandContainer
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly ICommunication communication;
        private readonly IUserHelper userHelper;

        private readonly HelixHelper helixHelper;

        public ShoutOutSystem(
            Config.BotConfiguration botConfig,
            ICommunication communication,
            IUserHelper userHelper,
            HelixHelper helixHelper)
        {
            this.botConfig = botConfig;
            this.communication = communication;
            this.userHelper = userHelper;

            this.helixHelper = helixHelper;
        }

        public void RegisterCommands(
            Dictionary<string, CommandHandler> commands,
            Dictionary<string, HelpFunction> helpFunctions,
            Dictionary<string, SetFunction> setFunctions,
            Dictionary<string, GetFunction> getFunctions)
        {
            commands.Add("so", ShoutOutCommandHandler);
            helpFunctions.Add("so", ShoutOutHelpHandler);
        }

        public IEnumerable<string> GetPublicCommands()
        {
            yield return "so";
        }

        private string ShoutOutHelpHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (remainingCommand == null || remainingCommand.Length == 0)
            {
                return "Send a shout-out message about another user.";
            }
            else
            {
                return $"No shout out subcommand found: {string.Join(' ', remainingCommand)}";
            }
        }

        private async Task ShoutOutCommandHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
            {
                communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
                return;
            }

            if (remainingCommand == null || remainingCommand.Length == 0)
            {
                //Get Random Quote
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no user specified for Shout-Out command.");
                return;
            }

            string userName = remainingCommand[0].ToLowerInvariant();
            if (userName[0] == '@')
            {
                //strip off superfluous @ signs
                userName = userName[1..];
            }

            User matchingUser = await userHelper.GetUserByTwitchLogin(userName);

            if (matchingUser == null)
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, twitch user \"{userName}\" not found.");
                return;
            }

            TwitchChannels channelsInfo = await helixHelper.GetChannels(matchingUser.TwitchUserId);

            if (channelsInfo == null || channelsInfo.Data.Count == 0)
            {
                communication.SendErrorMessage($"Unable to request channel information about user \"{matchingUser.TwitchUserName}\".");
                communication.SendPublicChatMessage($"Check out {matchingUser.TwitchUserName} at twitch.tv/{matchingUser.TwitchUserName}");
                return;
            }

            TwitchChannels.Datum channelInfo = channelsInfo.Data[0];

            communication.SendPublicChatMessage($"Check out {matchingUser.TwitchUserName} at twitch.tv/{matchingUser.TwitchUserName} - " +
                $"Their last stream was of {channelInfo.GameName}, entitled \"{channelInfo.Title}\"");
        }
    }
}
