using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading.Channels;
using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.EmoteEffects;
using TASagentTwitchBot.Plugin.Images;

namespace TASagentTwitchBot.Plugin.Pixoo;

public class PixooManager : IEmoteListener, IStartupListener, IDisposable
{
    private readonly IEmoteCacher emoteCacher;
    private readonly ICommunication communication;
    private readonly ErrorHandler errorHandler;

    //private readonly Dictionary<string, int> uploadMap = new Dictionary<string, int>();

    private readonly ChannelWriter<string> emoteQueueWriter;
    private readonly ChannelReader<string> emoteQueueReader;

    private int nextImage = 1;
    private bool disposedValue;

    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
    private readonly Task monitorTask;

    public PixooManager(
        IEmoteCacher emoteCacher,
        ICommunication communication,
        ErrorHandler errorHandler)
    {
        this.emoteCacher = emoteCacher;
        this.communication = communication;
        this.errorHandler = errorHandler;

        Channel<string> queueChannel = Channel.CreateUnbounded<string>();

        emoteQueueReader = queueChannel.Reader;
        emoteQueueWriter = queueChannel.Writer;

        //Channel<string> uploadQueueChannel = Channel.CreateUnbounded<string>();

        //emoteUploadQueueReader = uploadQueueChannel.Reader;
        //emoteUploadQueueWriter = uploadQueueChannel.Writer;
        InitialUploadBlank();
        monitorTask = Task.Run(MonitorEmotes);
    }

    private async void InitialUploadBlank()
    {
        await ResetUploadIndices();
        await UploadBlank();
    }

    private async Task UploadBlank()
    {
        Bitmap square = new Bitmap(64, 64, PixelFormat.Format24bppRgb);

        using Graphics graphics = Graphics.FromImage(square);
        graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 0, 0)), 0, 0, 64, 64);
        graphics.DrawImage(square, 0, 0, 64, 64);

        int index = await UploadEmote("blank", square);
        //uploadMap.Add("blank", index);
    }

    public async void NotifyEmotes(IEnumerable<string> urls)
    {
        foreach (string url in urls.Distinct())
        {
            await emoteQueueWriter.WriteAsync(url);
        }
    }

    //Thank you SO https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp
    private Bitmap ResizeAndReformatImage(Image image, int width, int height)
    {
        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        using Graphics graphics = Graphics.FromImage(destImage);
        
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        using ImageAttributes wrapMode = new ImageAttributes();
        wrapMode.SetWrapMode(WrapMode.TileFlipXY);

        graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);

        return destImage;
    }

    private async Task ShowEmote(int index)
    {
        RestClient restClient = new RestClient("http://10.0.0.106");
        RestRequest request = new RestRequest("post", Method.Post);
        request.AddBody(new PlayGifData(
            Command: "Device/PlayTFGif",
            FileType: 0,
            FileName: $"divoom_gif/{index}.gif"));

        communication.SendDebugMessage($"Trying to Show");

        RestResponse response = await restClient.ExecuteAsync(request, generalTokenSource.Token);

        //if (response?.Content is not null)
        //{
        //    communication.SendDebugMessage($"Show: {response.Content}");
        //}
    }

    private async Task<int> UploadEmote(string url, Bitmap bitmap)
    {
        var destRect = new Rectangle(0, 0, 64, 64);
        BitmapData bmpData = bitmap.LockBits(destRect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        
        // Get the address of the first line.
        IntPtr ptr = bmpData.Scan0;

        // Declare an array to hold the bytes of the bitmap.
        int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
        byte[] rgbValues = new byte[bytes];

        // Copy the RGB values into the array.
        System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

        //Swap R and B
        for (int i = 0; i < rgbValues.Length / 3; i++)
        {
            (rgbValues[3 * i], rgbValues[3 * i + 2]) = (rgbValues[3 * i + 2], rgbValues[3 * i]);
        }

        // Copy the RGB values back to the bitmap
        System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

        bitmap.UnlockBits(bmpData);

        RestClient restClient = new RestClient("http://10.0.0.106");
        RestRequest request = new RestRequest("post", Method.Post);
        request.AddBody(new SendAnimationData(
            Command: "Draw/SendHttpGif",
            PicNum: 1,
            PicWidth: 64,
            PicOffset: 0,
            PicID: nextImage,
            PicSpeed: 1000,
            PicData: Convert.ToBase64String(rgbValues)));

        RestResponse response = await restClient.ExecuteAsync(request, generalTokenSource.Token);

        //if (response?.Content is not null)
        //{
        //    communication.SendDebugMessage($"Show: {response.Content}");
        //}

        return nextImage++;
    }

    private async Task ResetUploadIndices()
    {
        RestClient restClient = new RestClient("http://10.0.0.106");
        RestRequest request = new RestRequest("post", Method.Post);
        request.AddBody(new SimpleCommand(
            Command: "Draw/ResetHttpGifId"));

        await restClient.ExecuteAsync(request);
    }

    private async Task MonitorEmotes()
    {
        try
        {
            while(!generalTokenSource.IsCancellationRequested)
            {
                if (!await emoteQueueReader.WaitToReadAsync(generalTokenSource.Token))
                {
                    break;
                }

                emoteQueueReader.TryRead(out string emoteURL);

                //if (uploadMap.TryGetValue(emoteURL, out int index))
                //{
                //    await ShowEmote(index);
                //}
                //else
                {
                    Bitmap bitmap = ResizeAndReformatImage(
                    image: await emoteCacher.GetEmoteBitmap(emoteURL!),
                    width: 64,
                    height: 64);

                    await UploadEmote(emoteURL, bitmap);

                    //index = await UploadEmote(emoteURL, bitmap);

                    //uploadMap.Add(emoteURL, index);
                }

                await Task.Delay(2_000, generalTokenSource.Token);
            }
        }
        catch (OperationCanceledException) { /* swallow */ }
        catch (ThreadAbortException) { /* swallow */ }
        catch (ObjectDisposedException) { /* swallow */ }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"Pixoo Manager Playback Exception: {ex.GetType().Name}");
            errorHandler.LogMessageException(ex, "");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                emoteQueueWriter.TryComplete();

                //Wait for monitor
                monitorTask.Wait(2_000);

                generalTokenSource.Cancel();

                //Wait for monitor
                monitorTask.Wait(2_000);

                generalTokenSource.Dispose();

                monitorTask.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

internal record SendAnimationData(
    string Command,
    int PicNum,
    int PicWidth,
    int PicOffset,
    int PicID,
    int PicSpeed,
    string PicData);

internal record PlayGifData(
    string Command,
    int FileType,
    string FileName);

internal record SimpleCommand(
    string Command);