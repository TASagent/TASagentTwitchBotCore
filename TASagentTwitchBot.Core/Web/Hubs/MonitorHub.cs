using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Hubs
{
    public class MonitorHub : Hub
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly IMessageAccumulator messsageAccumulator;

        public MonitorHub(
            Config.BotConfiguration botConfig,
            IMessageAccumulator messsageAccumulator)
        {
            this.botConfig = botConfig;
            this.messsageAccumulator = messsageAccumulator;
        }

        public async Task<bool> Authenticate(string token)
        {
            AuthDegree attemptedAuth = botConfig.AuthConfiguration.CheckAuthString(token);

            if (!botConfig.AuthConfiguration.PublicAuthAllowed && attemptedAuth <= AuthDegree.Privileged)
            {
                return false;
            }

            if (attemptedAuth == AuthDegree.None)
            {
                return false;
            }

            messsageAccumulator.AddAuthenticatedUser(Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, "Authenticated");

            return true;
        }

        public MessageBlock<SimpleMessage> RequestAllChats()
        {
            if (messsageAccumulator.IsAuthenticatedUser(Context.ConnectionId))
            {
                return messsageAccumulator.GetAllChats();
            }

            //Failed to authenticate
            return new MessageBlock<SimpleMessage>(new System.Collections.Generic.List<SimpleMessage>());
        }

        public MessageBlock<SimpleMessage> RequestAllEvents()
        {
            if (messsageAccumulator.IsAuthenticatedUser(Context.ConnectionId))
            {
                return messsageAccumulator.GetAllEvents();
            }

            //Failed to authenticate
            return new MessageBlock<SimpleMessage>(new System.Collections.Generic.List<SimpleMessage>());
        }

        public MessageBlock<SimpleMessage> RequestAllDebugs()
        {
            if (messsageAccumulator.IsAuthenticatedUser(Context.ConnectionId))
            {
                return messsageAccumulator.GetAllDebugs();
            }

            //Failed to authenticate
            return new MessageBlock<SimpleMessage>(new System.Collections.Generic.List<SimpleMessage>());
        }

        public MessageBlock<NotificationMessage> RequestAllNotifications()
        {
            if (messsageAccumulator.IsAuthenticatedUser(Context.ConnectionId))
            {
                return messsageAccumulator.GetAllNotifications();
            }

            //Failed to authenticate
            return new MessageBlock<NotificationMessage>(new System.Collections.Generic.List<NotificationMessage>());
        }

        public MessageBlock<NotificationMessage> RequestAllPendingNotifications()
        {
            if (messsageAccumulator.IsAuthenticatedUser(Context.ConnectionId))
            {
                return messsageAccumulator.GetAllPendingNotifications();
            }

            //Failed to authenticate
            return new MessageBlock<NotificationMessage>(new System.Collections.Generic.List<NotificationMessage>());
        }
    }
}
