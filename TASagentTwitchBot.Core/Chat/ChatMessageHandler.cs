using System;
using Microsoft.Extensions.DependencyInjection;

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
        private readonly IServiceScopeFactory scopeFactory;

        public ChatMessageHandler(
            ICommunication communication,
            Notifications.ICheerHandler cheerHandler,
            Config.IBotConfigContainer botConfigContainer,
            IServiceScopeFactory scopeFactory)
        {
            this.communication = communication;
            this.cheerHandler = cheerHandler;

            botConfig = botConfigContainer.BotConfig;
            this.scopeFactory = scopeFactory;
        }

        public virtual async void HandleChatMessage(IRC.IRCMessage message)
        {
            if (message.ircCommand != IRC.IRCCommand.PrivMsg && message.ircCommand != IRC.IRCCommand.Whisper)
            {
                communication.SendDebugMessage($"Error: Passing forward non-chat message:\n    {message}");
                return;
            }


            IRC.TwitchChatter chatter = await IRC.TwitchChatter.FromIRCMessage(message, communication, scopeFactory);

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
