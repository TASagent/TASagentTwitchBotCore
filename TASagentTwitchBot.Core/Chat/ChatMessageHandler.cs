using System;

namespace TASagentTwitchBot.Core.Chat
{
    public interface IChatMessageHandler
    {
        void HandleChatMessage(IRC.IRCMessage message);
    }

    public class ChatMessageHandler : IChatMessageHandler
    {
        private readonly ICommunication communication;
        private readonly Notifications.ICheerHandler cheerHandler;

        private readonly Config.BotConfiguration botConfig;
        private readonly Database.BaseDatabaseContext db;

        public ChatMessageHandler(
            ICommunication communication,
            Notifications.ICheerHandler cheerHandler,
            Config.IBotConfigContainer botConfigContainer,
            Database.BaseDatabaseContext db)
        {
            this.communication = communication;
            this.cheerHandler = cheerHandler;

            botConfig = botConfigContainer.BotConfig;
            this.db = db;
        }

        public virtual async void HandleChatMessage(IRC.IRCMessage message)
        {
            if (message.ircCommand != IRC.IRCCommand.PrivMsg && message.ircCommand != IRC.IRCCommand.Whisper)
            {
                communication.SendDebugMessage($"Error: Passing forward non-chat message:\n    {message}");
                return;
            }

            IRC.TwitchChatter chatter = await IRC.TwitchChatter.FromIRCMessage(message, communication, db);

            if (chatter == null)
            {
                return;
            }

            communication.DispatchChatMessage(chatter);

            if (chatter.Bits != 0 && chatter.Bits >= botConfig.BitTTSThreshold)
            {
                cheerHandler.HandleCheer(chatter.User, chatter.Message, chatter.Bits, true);
            }
        }
    }

}
