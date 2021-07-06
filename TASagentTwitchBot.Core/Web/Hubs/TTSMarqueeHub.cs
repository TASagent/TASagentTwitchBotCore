using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace TASagentTwitchBot.Core.Web.Hubs
{
    public class TTSMarqueeHub : Hub
    {
        //public async Task TriggerTTSNotification(string message)
        //{
        //    await Clients.All.SendAsync("ReceiveTTSNotification", message);
        //}
    }
}
