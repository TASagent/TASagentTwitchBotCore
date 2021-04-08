using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace TASagentTwitchBot.Core.Web.Hubs
{
    public class OverlayHub : Hub
    {
        //public async Task TriggerImageNotification(string text, double duration, string image)
        //{
        //    await Clients.All.SendAsync("ReceiveImageNotification", text, duration, image);
        //}

        //public async Task TriggerVideoNotification(string text, double duration, string video)
        //{
        //    await Clients.All.SendAsync("ReceiveVideoNotification", text, duration, video);
        //}
    }
}
