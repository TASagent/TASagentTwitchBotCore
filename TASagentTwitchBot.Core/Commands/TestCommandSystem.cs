using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace TASagentTwitchBot.Core.Commands
{
    public class TestCommandSystem : ICommandContainer
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly ICommunication communication;
        private readonly Audio.Effects.IAudioEffectSystem audioEffectSystem;
        private readonly Notifications.ISubscriptionHandler subscriptionHandler;
        private readonly Notifications.IRaidHandler raidHandler;
        private readonly Notifications.ICheerHandler cheerHandler;
        private readonly Bits.CheerHelper cheerHelper;

        private readonly Database.BaseDatabaseContext db;

        public TestCommandSystem(
            Config.IBotConfigContainer botConfigContainer,
            ICommunication communication,
            Audio.Effects.IAudioEffectSystem audioEffectSystem,
            Notifications.ISubscriptionHandler subscriptionHandler,
            Notifications.IRaidHandler raidHandler,
            Notifications.ICheerHandler cheerHandler,
            Bits.CheerHelper cheerHelper,
            Database.BaseDatabaseContext db)
        {
            botConfig = botConfigContainer.BotConfig;

            this.communication = communication;
            this.audioEffectSystem = audioEffectSystem;
            this.subscriptionHandler = subscriptionHandler;
            this.raidHandler = raidHandler;
            this.cheerHandler = cheerHandler;
            this.cheerHelper = cheerHelper;

            this.db = db;
        }

        public void RegisterCommands(
            Dictionary<string, CommandHandler> commands,
            Dictionary<string, HelpFunction> helpFunctions,
            Dictionary<string, SetFunction> setFunctions)
        {
            commands.Add("testraid", TestRaidHandler);
            commands.Add("testsub", TestSubHandler);
            commands.Add("testcheer", TestCheerHandler);
        }

        public IEnumerable<string> GetPublicCommands()
        {
            yield break;
        }

        private Task TestRaidHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Admin)
            {
                communication.SendPublicChatMessage($"You are not authorized to test raid notifications, @{chatter.User.TwitchUserName}.");
                return Task.CompletedTask;
            }

            raidHandler.HandleRaid(botConfig.BroadcasterId, 100, true);

            return Task.CompletedTask;
        }

        private Task TestSubHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Admin)
            {
                communication.SendPublicChatMessage($"You are not authorized to test sub notifications, @{chatter.User.TwitchUserName}.");
                return Task.CompletedTask;
            }

            string user = "TASagent";
            string submessage = "This is my sub message";

            if (remainingCommand.Length >= 1)
            {
                user = remainingCommand[0];
                submessage = "";
            }

            Database.User subUser = db.Users.FirstOrDefault(x => x.TwitchUserName.ToLower() == user.ToLower());

            if (subUser == null)
            {
                communication.SendWarningMessage($"Requested user {user} not found in database. Substituting broadcaster.");
                subUser = db.Users.Where(x => x.TwitchUserId == botConfig.BroadcasterId).FirstOrDefault();
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

            return Task.CompletedTask;
        }

        private Task TestCheerHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Admin)
            {
                communication.SendPublicChatMessage($"You are not authorized to test cheer notifications, @{chatter.User.TwitchUserName}.");
                return Task.CompletedTask;
            }

            if (remainingCommand.Length < 4)
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, error in testing cheer notification.  Syntax is !testcheer <user> <cheertype> <bits> <message>.");
            }
            else
            {
                Database.User cheerer = db.Users.FirstOrDefault(x => x.TwitchUserName.ToLower() == remainingCommand[0].ToLower());

                if (cheerer == null)
                {
                    communication.SendWarningMessage($"Requested user {remainingCommand[0]} not found in database. Substituting broadcaster.");
                    cheerer = db.Users.Where(x => x.TwitchUserId == botConfig.BroadcasterId).FirstOrDefault();
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

            return Task.CompletedTask;
        }
    }
}
