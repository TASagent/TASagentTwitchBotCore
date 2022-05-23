using Microsoft.AspNetCore.SignalR.Client;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.TTS.Parsing;

namespace TASagentTwitchBot.Core.TTS;

public class TTSWebRenderer : ITTSRenderer, IDisposable
{
    private readonly TTSConfiguration ttsConfig;
    private readonly Config.ServerConfig serverConfig;

    private readonly ICommunication communication;
    private readonly ISoundEffectSystem soundEffectSystem;

    private HubConnection? serverHubConnection;
    private readonly ErrorHandler errorHandler;

    private readonly Dictionary<string, OngoingDownload> ongoingDownloads = new Dictionary<string, OngoingDownload>();
    private readonly Dictionary<string, TaskCompletionSource<string?>> waitingDownloads = new Dictionary<string, TaskCompletionSource<string?>>();

    private bool Initialized { get; set; } = false;
    private Task<bool>? initializationTask = null;

    private bool disposedValue;
    private static string TTSFilesPath => BGC.IO.DataManagement.PathForDataDirectory("TTSFiles");

    public TTSWebRenderer(
        TTSConfiguration ttsConfig,
        Config.ServerConfig serverConfig,
        ICommunication communication,
        ISoundEffectSystem soundEffectSystem,
        ErrorHandler errorHandler)
    {
        this.ttsConfig = ttsConfig;
        this.serverConfig = serverConfig;
        this.communication = communication;
        this.soundEffectSystem = soundEffectSystem;
        this.errorHandler = errorHandler;

        if (ttsConfig.Enabled)
        {
            Task.Run(StartupInitialize);
        }
    }

    private async void StartupInitialize()
    {
        initializationTask = Task.Run(Initialize);

        if (!await initializationTask)
        {
            //Initialization Failed
            communication.SendErrorMessage($"TTSWebRenderer failed to initialize properly. Disabling TTS Service.");
            ttsConfig.Enabled = false;
        }
    }

    protected async Task<bool> Initialize()
    {
        if (Initialized)
        {
            return true;
        }

        if (string.IsNullOrEmpty(serverConfig.ServerAccessToken) ||
            string.IsNullOrEmpty(serverConfig.ServerAddress) ||
            string.IsNullOrEmpty(serverConfig.ServerUserName))
        {
            communication.SendErrorMessage($"TTSHub not configured. Register at https://server.tas.wtf/ and contact TASagent. " +
                $"Then update relevant details in Config/ServerConfig.json");

            return false;
        }

        serverHubConnection = new HubConnectionBuilder()
            .WithUrl($"{serverConfig.ServerAddress}/Hubs/BotTTSHub", options =>
            {
                options.Headers.Add("User-Id", serverConfig.ServerUserName);
                options.Headers.Add("Authorization", $"Bearer {serverConfig.ServerAccessToken}");
            })
            .WithAutomaticReconnect()
            .Build();

        serverHubConnection.Closed += ServerHubConnectionClosed;

        serverHubConnection.On<string>("ReceiveMessage", ReceiveMessage);
        serverHubConnection.On<string>("ReceiveWarning", ReceiveWarning);
        serverHubConnection.On<string>("ReceiveError", ReceiveError);

        serverHubConnection.On<string, byte[], int, int>("ReceiveData", ReceiveData);
        serverHubConnection.On<string, string>("CancelRequest", CancelRequest);

        try
        {
            await serverHubConnection!.StartAsync();
            Initialized = true;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                communication.SendErrorMessage($"TTSHub failed to connect due to permissions. Please contact TASagent to be given access.");
            }
            else
            {
                communication.SendErrorMessage($"TTSHub failed to connect. Make sure settings are correct.");
            }

            errorHandler.LogSystemException(ex);
        }
        catch (Exception ex)
        {
            errorHandler.LogSystemException(ex);
            communication.SendErrorMessage($"TTSHub failed to connect. Make sure settings are correct.");
        }

        return Initialized;
    }

    public async Task<bool> SetTTSEnabled(bool enabled)
    {
        if (enabled == ttsConfig.Enabled)
        {
            //Already set
            return true;
        }

        if (enabled && !Initialized)
        {
            //Turning it on, and not initialized
            if (initializationTask is null)
            {
                initializationTask = Initialize();
            }

            if (!await initializationTask)
            {
                //Failed to initialize
                communication.SendErrorMessage($"TTSWebRenderer failed to initialize properly. TTS will remain disabled.");
                return false;
            }
        }

        ttsConfig.Enabled = enabled;
        return true;
    }

    private Task ServerHubConnectionClosed(Exception? arg)
    {
        if (arg is not null)
        {
            errorHandler.LogSystemException(arg);
        }
        return Task.CompletedTask;
    }

    public async Task<AudioRequest?> TTSRequest(
        Commands.AuthorizationLevel authorizationLevel,
        TTSVoice voice,
        TTSPitch pitch,
        TTSSpeed speed,
        Effect effectsChain,
        string ttsText)
    {
        if (!ttsConfig.Enabled)
        {
            communication.SendDebugMessage($"TTS currently disabled - Rejecting request.");
            return null;
        }

        TTSService service = voice.GetTTSService();

        if (!ttsConfig.IsServiceSupported(service))
        {
            communication.SendWarningMessage($"TTS Service {service} unsupported.");

            service = ttsConfig.GetASupportedService();
            voice = TTSVoice.Unassigned;
        }

        //Make sure Neural Voices are allowed
        if (voice.IsNeuralVoice() && !ttsConfig.CanUseNeuralVoice(authorizationLevel))
        {
            communication.SendWarningMessage($"Neural voice {voice} disallowed.  Changing voice to service default.");
            voice = TTSVoice.Unassigned;
        }

        TTSSystemRenderer ttsSystemRenderer;

        switch (service)
        {
            case TTSService.Amazon:
                ttsSystemRenderer = new AmazonTTSWebRenderer(this, communication, voice, pitch, speed, effectsChain);
                break;

            case TTSService.Google:
                ttsSystemRenderer = new GoogleTTSWebRenderer(this, communication, voice, pitch, speed, effectsChain);
                break;

            case TTSService.Azure:
                ttsSystemRenderer = new AzureTTSWebRenderer(this, communication, voice, pitch, speed, effectsChain);
                break;

            default:
                communication.SendErrorMessage($"Unsupported TTSVoice for TTSService {service}");
                goto case TTSService.Google;
        }

        try
        {
            return await TTSParser.ParseTTS(ttsText, ttsSystemRenderer, soundEffectSystem);
        }
        catch (Exception ex)
        {
            errorHandler.LogCommandException(ex, $"!tts {ttsText}");
            return null;
        }
    }

    public void ReceiveMessage(string message) => communication.SendDebugMessage($"TTS WebServer Message: {message}");
    public void ReceiveWarning(string message) => communication.SendWarningMessage($"TTS WebServer Warning: {message}");
    public void ReceiveError(string message) => communication.SendErrorMessage($"TTS WebServer Error: {message}");


    public async Task ReceiveData(string requestIdentifier, byte[] data, int current, int total)
    {
        if (!ongoingDownloads.TryGetValue(requestIdentifier, out OngoingDownload? ongoingDownload))
        {
            if (!waitingDownloads.TryGetValue(requestIdentifier, out TaskCompletionSource<string?>? completionSource))
            {
                communication.SendWarningMessage($"Received unexpected TTS data for identifier: {requestIdentifier}");
                return;
            }

            waitingDownloads.Remove(requestIdentifier);

            if (total <= 0)
            {
                //Bad data, Clear it
                communication.SendWarningMessage($"Received empty TTS data for identifier: {requestIdentifier}");
                completionSource.SetResult(null);
                return;
            }

            ongoingDownload = new OngoingDownload()
            {
                Data = new byte[total],
                Downloaded = 0,
                CompletionSource = completionSource
            };

            ongoingDownloads.Add(requestIdentifier, ongoingDownload);
        }

        Array.Copy(
            sourceArray: data,
            sourceIndex: 0,
            destinationArray: ongoingDownload.Data!,
            destinationIndex: ongoingDownload.Downloaded,
            length: current);

        ongoingDownload.Downloaded += current;

        if (ongoingDownload.Downloaded == total)
        {
            //Report finished
            ongoingDownloads.Remove(requestIdentifier);

            string filepath = Path.Combine(TTSFilesPath, $"{Guid.NewGuid()}.mp3");

            using Stream file = new FileStream(filepath, FileMode.Create);
            await file.WriteAsync(ongoingDownload.Data);

            ongoingDownload.CompletionSource!.SetResult(filepath);
        }
    }

    public async Task<string?> SubmitTTSWebRequest(ServerTTSRequest request)
    {
        TaskCompletionSource<string?> completionSource = new TaskCompletionSource<string?>();
        waitingDownloads.Add(request.RequestIdentifier, completionSource);

        await serverHubConnection!.InvokeAsync("RequestTTS", request);
        return await completionSource.Task;
    }

    public void CancelRequest(string requestIdentifier, string reason)
    {
        if (ongoingDownloads.TryGetValue(requestIdentifier, out OngoingDownload? ongoingDownload))
        {
            ongoingDownload.CompletionSource?.SetResult(null);
            ongoingDownloads.Remove(requestIdentifier);
        }

        if (waitingDownloads.TryGetValue(requestIdentifier, out TaskCompletionSource<string?>? completionSource))
        {
            completionSource.SetResult(null);
            waitingDownloads.Remove(requestIdentifier);
        }

        communication.SendWarningMessage($"TTS request {requestIdentifier} cancelled: {reason}");
    }

    private class OngoingDownload
    {
        public byte[]? Data { get; init; }
        public int Downloaded { get; set; }
        public TaskCompletionSource<string?>? CompletionSource { get; init; }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (serverHubConnection is not null)
                {
                    if (serverHubConnection.State != HubConnectionState.Disconnected)
                    {
                        serverHubConnection.StopAsync().Wait();
                    }

                    serverHubConnection.DisposeAsync().AsTask().Wait();
                }
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
