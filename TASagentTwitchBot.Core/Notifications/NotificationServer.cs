using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.Web.Hubs;

namespace TASagentTwitchBot.Core.Notifications;

public class NotificationServer
{
    private readonly IHubContext<OverlayHub> _overlayHubContext;
    private readonly IHubContext<TTSMarqueeHub> _ttsMarqueeHubContext;

    public NotificationServer(
        IHubContext<OverlayHub> overlayHubContext,
        IHubContext<TTSMarqueeHub> ttsMarqueeHubContext)
    {
        _overlayHubContext = overlayHubContext;
        _ttsMarqueeHubContext = ttsMarqueeHubContext;
    }

    public async Task ShowNotificationAsync(NotificationMessage message)
    {
        switch (message)
        {
            case ImageNotificationMessage imageMessage:
                await _overlayHubContext.Clients.All.SendAsync("ReceiveImageNotification",
                    imageMessage.GetMessage(),
                    imageMessage.duration,
                    imageMessage.GetImage());
                await Task.Delay((int)imageMessage.duration + 500);
                break;

            case VideoNotificationMessage videoMessage:
                await _overlayHubContext.Clients.All.SendAsync("ReceiveVideoNotification",
                    videoMessage.GetMessage(),
                    videoMessage.duration,
                    videoMessage.GetVideo());
                await Task.Delay((int)videoMessage.duration + 500);
                break;

            default:
                throw new Exception($"Unexpected NotificationMessage: {message}");
        }
    }

    public async Task ShowTTSMessageAsync(string message)
    {
        await _ttsMarqueeHubContext.Clients.All.SendAsync("ReceiveTTSNotification", message);
    }
}
