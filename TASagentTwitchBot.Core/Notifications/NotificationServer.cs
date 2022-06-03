using Microsoft.AspNetCore.SignalR;
using BGC.Collections.Generic;

using TASagentTwitchBot.Core.Web.Hubs;

namespace TASagentTwitchBot.Core.Notifications;

public class NotificationServer
{
    private readonly IHubContext<OverlayHub> _overlayHubContext;
    private readonly IHubContext<TTSMarqueeHub> _ttsMarqueeHubContext;

    private const string ASSET_URL = "/Assets/Images";

    private readonly string imagePath;

    private readonly DepletableBag<string> imageURLs;

    public NotificationServer(
        IHubContext<OverlayHub> overlayHubContext,
        IHubContext<TTSMarqueeHub> ttsMarqueeHubContext,
        IWebHostEnvironment env)
    {
        _overlayHubContext = overlayHubContext;
        _ttsMarqueeHubContext = ttsMarqueeHubContext;

        imagePath = Path.Combine(env.WebRootPath, "Assets", "Images");

        imageURLs = new DepletableBag<string>()
        {
            AutoRefill = true
        };

        foreach (string imagePath in Directory.GetFiles(imagePath))
        {
            string url = $"{ASSET_URL}/{Path.GetFileName(imagePath)}";
            imageURLs.Add(url);
        }
    }

    public string GetNextImageURL() => imageURLs.PopNext()!;

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
