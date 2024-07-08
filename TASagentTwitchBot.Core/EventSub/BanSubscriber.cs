using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace TASagentTwitchBot.Core.EventSub;

[Obsolete("Currently not supported by Moderators.")]
public class BanSubscriber : IEventSubSubscriber
{
    private readonly ICommunication communication;

    public BanSubscriber(
        ICommunication communication)
    {
        this.communication = communication;
    }

    public void RegisterHandlers(Dictionary<string, EventHandler> handlers)
    {
        handlers.Add("channel.ban", HandleBanEvent);
        handlers.Add("channel.unban", HandleUnbanEvent);
    }

    public async Task HandleBanEvent(JsonElement twitchEvent)
    {
        communication.SendDebugMessage($"Ban Event:\n{twitchEvent}\n");
    }
    public async Task HandleUnbanEvent(JsonElement twitchEvent)
    {
        communication.SendDebugMessage($"Unban Event:\n{twitchEvent}\n");
    }
}
