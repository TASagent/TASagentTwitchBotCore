using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.Commands
{
    public class TestCommandSystem : ICommandContainer
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly ICommunication communication;
        private readonly Notifications.ISubscriptionHandler subscriptionHandler;
        private readonly Notifications.IRaidHandler raidHandler;
        private readonly Notifications.ICheerHandler cheerHandler;

        private readonly Database.IUserHelper userHelper;

        public TestCommandSystem(
            Config.IBotConfigContainer botConfigContainer,
            ICommunication communication,
            Notifications.ISubscriptionHandler subscriptionHandler,
            Notifications.IRaidHandler raidHandler,
            Notifications.ICheerHandler cheerHandler,
            Database.IUserHelper userHelper)
        {
            botConfig = botConfigContainer.BotConfig;

            this.communication = communication;
            this.subscriptionHandler = subscriptionHandler;
            this.raidHandler = raidHandler;
            this.cheerHandler = cheerHandler;

            this.userHelper = userHelper;
        }

        public void RegisterCommands(
            Dictionary<string, CommandHandler> commands,
            Dictionary<string, HelpFunction> helpFunctions,
            Dictionary<string, SetFunction> setFunctions,
            Dictionary<string, GetFunction> getFunctions)
        {
            commands.Add("testraid", TestRaidHandler);
            commands.Add("testsub", TestSubHandler);
            commands.Add("testcheer", TestCheerHandler);
        }

        public IEnumerable<string> GetPublicCommands()
        {
            yield break;
        }

        private async Task TestRaidHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Admin)
            {
                communication.SendPublicChatMessage($"You are not authorized to test raid notifications, @{chatter.User.TwitchUserName}.");
                return;
            }

            string userId = botConfig.BroadcasterId;
            int userCount = 100;

            //Optional Raider Name
            if (remainingCommand.Length > 0)
            {
                Database.User raider = await userHelper.GetUserByTwitchLogin(remainingCommand[0]);

                if (raider is not null)
                {
                    userId = raider.TwitchUserId;
                }
            }

            //Optional Raider Count
            if (remainingCommand.Length > 1 && int.TryParse(remainingCommand[1], out int newUserCount))
            {
                userCount = newUserCount;
            }

            raidHandler.HandleRaid(userId, userCount, true);

            return;
        }

        private async Task TestSubHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Admin)
            {
                communication.SendPublicChatMessage($"You are not authorized to test sub notifications, @{chatter.User.TwitchUserName}.");
                return;
            }

            string user = "TASagent";
            string submessage = "This is my sub message";

            if (remainingCommand.Length >= 1)
            {
                user = remainingCommand[0];
                submessage = "";
            }

            Database.User subUser = await userHelper.GetUserByTwitchLogin(user.ToLower(), false);

            if (subUser == null)
            {
                communication.SendWarningMessage($"Requested user {user} not found in database. Substituting broadcaster.");
                subUser = await userHelper.GetUserByTwitchId(botConfig.BroadcasterId, false);
            }

            if (remainingCommand.Length < 2 || !int.TryParse(remainingCommand[1], out int months))
            {
                months = 1;
            }

            if (remainingCommand.Length < 3 || !int.TryParse(remainingCommand[2], out int tier))
            {
                tier = 1;
            }

            subscriptionHandler.HandleSubscription(
                userId: subUser.TwitchUserId,
                message: submessage,
                monthCount: months,
                tier: tier,
                approved: true);
        }

        private async Task TestCheerHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Admin)
            {
                communication.SendPublicChatMessage($"You are not authorized to test cheer notifications, @{chatter.User.TwitchUserName}.");
                return;
            }

            if (remainingCommand.Length < 4)
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, error in testing cheer notification.  Syntax is !testcheer <user> <cheertype> <bits> <message>.");
            }
            else
            {
                Database.User cheerer = await userHelper.GetUserByTwitchLogin(remainingCommand[0].ToLower(), false);

                if (cheerer == null)
                {
                    communication.SendWarningMessage($"Requested user {remainingCommand[0]} not found in database. Substituting broadcaster.");
                    cheerer = await userHelper.GetUserByTwitchId(botConfig.BroadcasterId, false);
                }

                if (!int.TryParse(remainingCommand[2], out int quantity))
                {
                    quantity = 1000;
                }

                string message = string.Join(' ', remainingCommand[3..]) + $" {remainingCommand[1]}{remainingCommand[2]}";

                cheerHandler.HandleCheer(
                    cheerer: cheerer,
                    message: message,
                    quantity: quantity,
                    approved: true);
            }
        }
    }
}
