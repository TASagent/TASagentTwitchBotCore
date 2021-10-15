using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.WebServer.Web.Hubs;

namespace TASagentTwitchBot.Core.WebServer.Connections
{
    public interface ISocketManager
    {
        void NotifyConnection(string channelId);
        void NotifyDisconnection(string channelId);
        Task ForwardMessage(string channelName, string message);
    }

    public class SocketManager : ISocketManager
    {
        private readonly List<string> liveConnections = new List<string>();
        private readonly IHubContext<BotHub> botHubContext;

        public SocketManager(
            IHubContext<BotHub> botHubContext)
        {
            this.botHubContext = botHubContext;
        }

        public void NotifyConnection(string channelId)
        {
            liveConnections.Add(channelId);
        }

        public void NotifyDisconnection(string channelId)
        {
            liveConnections.Remove(channelId);
        }

        public async Task ForwardMessage(string channelId, string message)
        {
            await botHubContext.Clients.Group(channelId).SendAsync("ReceiveMessage", message);
        }
    }

    public record EventSubData(
        List<EventSubData.ChannelDatum> ChannelData)
    {
        public record ChannelDatum(
            string ChannelName,
            string BroadcasterID,
            List<string> Subscriptions);
    }
}
