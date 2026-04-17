using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using TASagentTwitchBot.Core;

namespace TASagentTwitchBot.Plugin.Vestaboard;

public class VestaboardManager : IStartupListener, IDisposable
{
    private readonly ICommunication communication;
    private readonly VestaboardConfiguration vestaboardConfiguration;
    private readonly ErrorHandler errorHandler;

    //private readonly ChannelWriter<string> emoteQueueWriter;
    //private readonly ChannelReader<string> emoteQueueReader;

    private readonly Message defaultMessage;

    private bool disposedValue;
    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
    //private readonly Task monitorTask;

    public VestaboardManager(
        ICommunication communication,
        VestaboardConfiguration vestaboardConfiguration,
        ErrorHandler errorHandler)
    {
        this.communication = communication;
        this.vestaboardConfiguration = vestaboardConfiguration;
        this.errorHandler = errorHandler;

        StoredMessage defaultConfigMessage =
            vestaboardConfiguration.Messages.FirstOrDefault(x => string.Compare(x.Name, "default", ignoreCase: true) == 0) ??
            new StoredMessage("default", [ "yyyvbvbbbvbvyyy", "ygggggghggggggy", "yyyvbvbbbvbvyyy" ]);

        defaultMessage = VestaboardUtils.ConvertStored(defaultConfigMessage);


        //emoteQueueReader = queueChannel.Reader;
        //emoteQueueWriter = queueChannel.Writer;

        PushDefault();
        //monitorTask = Task.Run(MonitorEmotes);
    }

    public async void PushDefault()
    {
        await ShowMessage(defaultMessage);
    }

    public async void ImmediateSend(string message)
    {
        List<string> messages = new();

        while (message.Length > 0)
        {
            messages.Add(message.Substring(0, Math.Min(message.Length, 15)));
            message = message.Substring(Math.Min(message.Length, 15));
        }

        Message finalMessage = new Message(VestaboardUtils.ConvertMessage(messages));

        await ShowMessage(finalMessage);
    }

    //private async Task UploadBlank()
    //{
    //    Bitmap square = new Bitmap(64, 64, PixelFormat.Format24bppRgb);

    //    using Graphics graphics = Graphics.FromImage(square);
    //    graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 0, 0)), 0, 0, 64, 64);
    //    graphics.DrawImage(square, 0, 0, 64, 64);

    //    int index = await UploadEmote("blank", square);
    //    //uploadMap.Add("blank", index);
    //}

    //public async void NotifyEmotes(IEnumerable<string> urls)
    //{
    //    foreach (string url in urls.Distinct())
    //    {
    //        await emoteQueueWriter.WriteAsync(url);
    //    }
    //}


    private async Task ShowMessage(Message message)
    {
        RestClient restClient = new RestClient($"http://{vestaboardConfiguration.IPAddress}:7000/local-api");
        RestRequest request = new RestRequest("message", Method.Post);

        request.AddHeader("X-Vestaboard-Local-Api-Key", vestaboardConfiguration.ApiKey);
        request.AddHeader("Content-Type", "application/json");

        string stringMessage = JsonSerializer.Serialize(message);

        request.AddBody(stringMessage);

        communication.SendDebugMessage($"Trying to Show: {stringMessage}");

        RestResponse response = await restClient.ExecuteAsync(request, generalTokenSource.Token);
    }

    //private async Task<int> UploadEmote(string url, Bitmap bitmap)
    //{
    //    var destRect = new Rectangle(0, 0, 64, 64);
    //    BitmapData bmpData = bitmap.LockBits(destRect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

    //    // Get the address of the first line.
    //    IntPtr ptr = bmpData.Scan0;

    //    // Declare an array to hold the bytes of the bitmap.
    //    int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
    //    byte[] rgbValues = new byte[bytes];

    //    // Copy the RGB values into the array.
    //    System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

    //    //Swap R and B
    //    for (int i = 0; i < rgbValues.Length / 3; i++)
    //    {
    //        (rgbValues[3 * i], rgbValues[3 * i + 2]) = (rgbValues[3 * i + 2], rgbValues[3 * i]);
    //    }

    //    // Copy the RGB values back to the bitmap
    //    System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

    //    bitmap.UnlockBits(bmpData);

    //    RestClient restClient = new RestClient("http://10.0.0.106");
    //    RestRequest request = new RestRequest("post", Method.Post);
    //    request.AddBody(new SendAnimationData(
    //        Command: "Draw/SendHttpGif",
    //        PicNum: 1,
    //        PicWidth: 64,
    //        PicOffset: 0,
    //        PicID: nextImage,
    //        PicSpeed: 1000,
    //        PicData: Convert.ToBase64String(rgbValues)));

    //    RestResponse response = await restClient.ExecuteAsync(request, generalTokenSource.Token);

    //    //if (response?.Content is not null)
    //    //{
    //    //    communication.SendDebugMessage($"Show: {response.Content}");
    //    //}

    //    return nextImage++;
    //}


    //private async Task MonitorEmotes()
    //{
    //    try
    //    {
    //        while (!generalTokenSource.IsCancellationRequested)
    //        {
    //            if (!await emoteQueueReader.WaitToReadAsync(generalTokenSource.Token))
    //            {
    //                break;
    //            }

    //            emoteQueueReader.TryRead(out string emoteURL);

    //            {
    //                Bitmap bitmap = ResizeAndReformatImage(
    //                image: await emoteCacher.GetEmoteBitmap(emoteURL!),
    //                width: 64,
    //                height: 64);

    //                await UploadEmote(emoteURL, bitmap);
    //            }

    //            await Task.Delay(2_000, generalTokenSource.Token);
    //        }
    //    }
    //    catch (OperationCanceledException) { /* swallow */ }
    //    catch (ThreadAbortException) { /* swallow */ }
    //    catch (ObjectDisposedException) { /* swallow */ }
    //    catch (Exception ex)
    //    {
    //        communication.SendErrorMessage($"Pixoo Manager Playback Exception: {ex.GetType().Name}");
    //        errorHandler.LogMessageException(ex, "");
    //    }
    //}

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                //emoteQueueWriter.TryComplete();

                ////Wait for monitor
                //monitorTask.Wait(2_000);

                generalTokenSource.Cancel();

                //Wait for monitor
                //monitorTask.Wait(2_000);

                generalTokenSource.Dispose();

                //monitorTask.Dispose();
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

public record StoredMessage(
    string Name,
    string[] Messages);

internal record Message(
    [property: JsonPropertyName("characters")]
    List<List<int>> Characters,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("strategy")]
    string? Strategy = null,

    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("step_interval_ms")]
    int? StepIntervalMS = null,

    [property : JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("step_size")]
    int? StepSize = null);


internal static class VestaboardUtils
{
    internal static Message ConvertStored(StoredMessage message) => new Message(Characters: ConvertMessage(message.Messages));

    internal static List<List<int>> ConvertMessage(params string[] lines) => lines.Select(LineMapper).ToList();
    internal static List<List<int>> ConvertMessage(IEnumerable<string> lines) => lines.Select(LineMapper).ToList();

    internal static List<int> LineMapper(string line) => line.Select(CharMapper).ToList();

    internal static int CharMapper(char c) =>
        c switch
        {
            >= 'A' and <= 'Z' => c - 'A' + 1,  //letters

            >= '1' and <= '9' => c - '1' + 27, //numbers
            '0' => 36, //black

            '!' => 37,
            '@' => 38,
            '#' => 39,
            '$' => 40,

            '(' => 41,
            ')' => 42,

            '-' => 44,
            '+' => 46,
            '&' => 47,
            '=' => 48,
            ';' => 49,
            ':' => 50,
            '\'' => 52,
            '"' => 53,
            '%' => 54,
            ',' => 55,
            '.' => 56,
            '/' => 59,
            '?' => 60,
            'h' => 62, //heart

            'r' => 63, //red
            'o' => 64, //orange
            'y' => 65, //yellow
            'g' => 66, //green
            'b' => 67, //blue
            'v' => 68, //violet

            'w' => 69, //white
            'k' => 70, //black
            ' ' => 0,  //black 
            '_' => 0,  //black

            _ => 0
        };
}