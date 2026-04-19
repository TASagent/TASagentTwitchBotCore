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
using TASagentTwitchBot.Core.Database;
using TASagentTwitchBot.Core.Notifications;

namespace TASagentTwitchBot.Plugin.Vestaboard;

public class VestaboardManager : IStartupListener, ICheerHandler, IDisposable
{
    private readonly ICommunication communication;
    private readonly VestaboardConfiguration vestaboardConfiguration;
    private readonly ErrorHandler errorHandler;

    private readonly ChannelWriter<string> messageQueueWriter;
    private readonly ChannelReader<string> messageQueueReader;

    private readonly Message defaultMessage;

    private bool disposedValue;
    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
    private readonly Task monitorTask;

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
            new StoredMessage("default", [ "rrroooyyygggbbb", "rroooyyygggbbbv", "roooyyygggbbbvv"]);

        defaultMessage = VestaboardUtils.ConvertStored(defaultConfigMessage);

        Channel<string> queueChannel = Channel.CreateUnbounded<string>();

        messageQueueReader = queueChannel.Reader;
        messageQueueWriter = queueChannel.Writer;

        PushDefault();
        monitorTask = Task.Run(MonitorMessages);
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

    public void QueueMessage(string message)
    {
        messageQueueWriter.TryWrite(message);
    }

    void ICheerHandler.HandleCheer(User cheerer, string message, int quantity, bool meetsTTSThreshold, bool approved)
    {
        if (meetsTTSThreshold && approved)
        {
            //All caps because of Vestaboard limitations
            message = message.ToUpperInvariant();

            message = message.Replace("CHEER", "h");

            messageQueueWriter.TryWrite(message);
        }
    }

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


    private async Task MonitorMessages()
    {
        try
        {
            while (!generalTokenSource.IsCancellationRequested)
            {
                if (!await messageQueueReader.WaitToReadAsync(generalTokenSource.Token))
                {
                    break;
                }

                messageQueueReader.TryRead(out string? message);

                if (message != null)
                {
                    ImmediateSend(message);
                }

                await Task.Delay(30_000, generalTokenSource.Token);
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
                messageQueueWriter.TryComplete();

                ////Wait for monitor
                monitorTask.Wait(2_000);

                generalTokenSource.Cancel();

                //Wait for monitor
                monitorTask.Wait(2_000);

                generalTokenSource.Dispose();

                monitorTask.Dispose();
            }

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