using Microsoft.AspNetCore.SignalR;

namespace TASagentTwitchBot.Core.Web.Hubs;

public class EmoteHub : Hub
{
    //public async Task TriggerEmote(List<string> emoteURLs)
    //{
    //    await Clients.All.SendAsync("ReceiveEmote", emoteURLs);
    //}
}
