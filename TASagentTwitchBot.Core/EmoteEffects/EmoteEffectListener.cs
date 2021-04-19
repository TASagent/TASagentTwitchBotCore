using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace TASagentTwitchBot.Core.EmoteEffects
{
    public class EmoteEffectListener
    {
        private readonly IHubContext<Web.Hubs.EmoteHub> emoteHubContext;

        public EmoteEffectListener(
            ICommunication communication,
            IHubContext<Web.Hubs.EmoteHub> emoteHubContext)
        {
            communication.ReceiveMessageHandlers += ReceiveMessageHandler;

            this.emoteHubContext = emoteHubContext;
        }

        private async void ReceiveMessageHandler(IRC.TwitchChatter chatter)
        {
            if (chatter.Emotes.Count > 0)
            {
                await emoteHubContext.Clients.All.SendAsync("ReceiveEmotes", chatter.Emotes.Select(x => x.URL).ToList());
            }
        }
    }
}
