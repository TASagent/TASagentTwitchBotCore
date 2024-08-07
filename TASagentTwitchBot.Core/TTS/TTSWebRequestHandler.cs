﻿using Microsoft.AspNetCore.SignalR.Client;

namespace TASagentTwitchBot.Core.TTS;

public class TTSWebRequestHandler : IDisposable
{
    private readonly TTSConfiguration ttsConfig;
    private readonly Config.ServerConfig serverConfig;

    private readonly ICommunication communication;

    private HubConnection? serverHubConnection;
    private readonly ErrorHandler errorHandler;

    private readonly Dictionary<string, OngoingDownload> ongoingDownloads = new Dictionary<string, OngoingDownload>();
    private readonly Dictionary<string, TaskCompletionSource<string?>> waitingDownloads = new Dictionary<string, TaskCompletionSource<string?>>();

    private bool Initialized { get; set; } = false;
    private Task<bool>? initializationTask = null;

    private bool disposedValue;
    private static string TTSFilesPath => BGC.IO.DataManagement.PathForDataDirectory("TTSFiles");

    public TTSWebRequestHandler(
        TTSConfiguration ttsConfig,
        Config.ServerConfig serverConfig,
        ICommunication communication,
        ErrorHandler errorHandler)
    {
        this.ttsConfig = ttsConfig;
        this.serverConfig = serverConfig;
        this.communication = communication;
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
            communication.SendErrorMessage($"TTSWebRenderer failed to initialize properly.");
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
                communication.SendErrorMessage($"TTSHub failed to connect. Make sure settings are correct. Message: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"TTSHub failed to connect. Make sure settings are correct.");
            errorHandler.LogSystemException(ex);
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

        await serverHubConnection!.InvokeCoreAsync("RequestNewTTS", new object?[] { request });
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
