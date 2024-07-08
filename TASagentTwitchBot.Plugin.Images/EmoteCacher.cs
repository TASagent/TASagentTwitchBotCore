using System.Drawing;
using System.Drawing.Imaging;
using TASagentTwitchBot.Core;

namespace TASagentTwitchBot.Plugin.Images;

[AutoRegister]
public interface IEmoteCacher
{
    Task<Bitmap> GetEmoteBitmap(string url);
}

public class EmoteCacher : IEmoteCacher
{
    public Dictionary<string, CachedEmote> CachedEmotes { get; } = new Dictionary<string, CachedEmote>();

    public EmoteCacher()
    {

    }

    public async Task<Bitmap> GetEmoteBitmap(string url)
    {
        if (CachedEmotes.TryGetValue(url, out CachedEmote? emote))
        {
            return await emote.bitmapDownload;
        }

        emote = new CachedEmote(url);
        CachedEmotes.Add(url, emote);
        return await emote.bitmapDownload;
    }
}

public class CachedEmote
{
    public string URL { get; }

    public readonly Task<Bitmap> bitmapDownload;

    public CachedEmote(string url)
    {
        URL = url;

        bitmapDownload = FetchImage();
    }

    private async Task<Bitmap> FetchImage()
    {
        HttpClient client = new HttpClient
        {
            BaseAddress = new Uri(URL)
        };

        HttpResponseMessage response = await client.GetAsync(URL);

        return new Bitmap(await response.Content.ReadAsStreamAsync());
    }
}