using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.StaticFiles;
using TASagentTwitchBot.Core.Audio;

namespace TASagentTwitchBot.Core.DataForwarding;

public class DataForwardingClient : IStartupListener, IDisposable
{
    private readonly Config.ServerConfig serverConfig;

    private readonly ICommunication communication;
    private readonly ISoundEffectSystem soundEffectSystem;

    private HubConnection? serverHubConnection;
    private readonly ErrorHandler errorHandler;

    private bool Initialized { get; set; } = false;
    private Task<bool>? initializationTask = null;

    private bool disposedValue;

    public DataForwardingClient(
        Config.ServerConfig serverConfig,
        ICommunication communication,
        ISoundEffectSystem soundEffectSystem,
        ErrorHandler errorHandler)
    {
        this.serverConfig = serverConfig;
        this.communication = communication;
        this.soundEffectSystem = soundEffectSystem;
        this.errorHandler = errorHandler;

        Task.Run(StartupInitialize);
    }

    private async void StartupInitialize()
    {
        initializationTask = Task.Run(Initialize);

        if (!await initializationTask)
        {
            //Initialization Failed
            communication.SendErrorMessage($"DataForwardingClient failed to initialize properly.");
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
            communication.SendErrorMessage($"DataForwardingClient not configured. Register at https://server.tas.wtf/ and contact TASagent. " +
                $"Then update relevant details in Config/ServerConfig.json");

            return false;
        }

        serverHubConnection = new HubConnectionBuilder()
            .WithUrl($"{serverConfig.ServerAddress}/Hubs/BotDataForwardingHub", options =>
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

        serverHubConnection.On<string, string>("RequestSoundEffect", RequestSoundEffect);

        try
        {
            await serverHubConnection!.StartAsync();
            Initialized = true;

            await serverHubConnection.InvokeCoreAsync("UpdateSoundEffects", new object?[] { GetServerSoundEffectList() });
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                communication.SendErrorMessage($"DataForwardingClientHub failed to connect due to permissions. Please contact TASagent to be given access.");
            }
            else
            {
                communication.SendErrorMessage($"DataForwardingClientHub failed to connect. Make sure settings are correct. Message: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"DataForwardingClientHub failed to connect. Make sure settings are correct.");
            errorHandler.LogSystemException(ex);
        }

        return Initialized;
    }

    private Task ServerHubConnectionClosed(Exception? arg)
    {
        if (arg is not null)
        {
            errorHandler.LogSystemException(arg);
        }
        return Task.CompletedTask;
    }

    public void ReceiveMessage(string message) => communication.SendDebugMessage($"DataForwarding WebServer Message: {message}");
    public void ReceiveWarning(string message) => communication.SendWarningMessage($"DataForwarding WebServer Warning: {message}");
    public void ReceiveError(string message) => communication.SendErrorMessage($"DataForwarding WebServer Error: {message}");

    private List<ServerSoundEffect> GetServerSoundEffectList() =>
        soundEffectSystem
        .GetAllSoundEffects()
        .Select(x => new ServerSoundEffect(x.Name, x.Aliases))
        .ToList();


    private async Task RequestSoundEffect(string soundEffectAlias, string requestIdentifier)
    {
        SoundEffect? soundEffect = soundEffectSystem.GetSoundEffectByAlias(soundEffectAlias);

        if (soundEffect is null)
        {
            await serverHubConnection!.InvokeCoreAsync("CancelSoundEffect", new object?[] { requestIdentifier, $"Sound effect {soundEffectAlias} does not exist" });
            return;
        }

        byte[] fileData = await File.ReadAllBytesAsync(soundEffect.FilePath);

        new FileExtensionContentTypeProvider().TryGetContentType(soundEffect.FilePath, out string? contentType);

        await serverHubConnection!.InvokeCoreAsync("UploadSoundEffect", new object?[] { requestIdentifier, soundEffect.Name, fileData, contentType });
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
