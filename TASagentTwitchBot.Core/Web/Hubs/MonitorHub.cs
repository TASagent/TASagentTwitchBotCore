using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Hubs
{
    public class MonitorHub : Hub
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly IMessageAccumulator messsageAccumulator;
        private readonly Notifications.IActivityDispatcher activityDispatcher;

        public MonitorHub(
            Config.IBotConfigContainer botConfigContainer,
            IMessageAccumulator messsageAccumulator,
            Notifications.IActivityDispatcher activityDispatcher)
        {
            botConfig = botConfigContainer.BotConfig;
            this.messsageAccumulator = messsageAccumulator;
            this.activityDispatcher = activityDispatcher;
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

        public async Task<bool> UpdatePendingNotification(int index, bool approve)
        {
            if (messsageAccumulator.IsAuthenticatedUser(Context.ConnectionId))
            {
                messsageAccumulator.RemovePendingNotification(index);
                bool success = activityDispatcher.UpdatePendingRequest(index, approve);

                if (success)
                {
                    await Clients.OthersInGroup("Authenticated").SendAsync(
                        "NotifyPendingRemoved", index);
                }

                return success;
            }

            //Failed to authenticate
            return false;
        }

        public bool ReplayNotification(int index)
        {
            if (messsageAccumulator.IsAuthenticatedUser(Context.ConnectionId))
            {
                return activityDispatcher.ReplayNotification(index);
            }

            //Failed to authenticate
            return false;
        }
    }
}
