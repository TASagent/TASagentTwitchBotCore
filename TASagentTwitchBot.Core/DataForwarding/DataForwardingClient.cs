using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.StaticFiles;
using System.Linq;
using TASagentTwitchBot.Core.Audio;

namespace TASagentTwitchBot.Core.DataForwarding;

[AutoRegister]
public interface IDataForwardingContextHandler
{
    void Register(IDataForwardingRegistrar registrar);

    List<ServerDataFile> GetDataFileList(string context);
    string? GetDataFilePath(string dataFileAlias, string context);

    Task Initialize(IDataForwardingInitializer initializer);
}

public interface IDataForwardingRegistrar
{
    void RegisterHandler(string context, IDataForwardingContextHandler handler);
}

public interface IDataForwardingInitializer
{
    Task ClearServerFileList(string context);
    Task UpdateServerFileList(string context, List<ServerDataFile> dataFiles);
}

public class DataForwardingClient : IStartupListener, IDataForwardingRegistrar, IDataForwardingInitializer, IDisposable
{
    private readonly Config.ServerConfig serverConfig;

    private readonly ICommunication communication;

    private readonly Dictionary<string, IDataForwardingContextHandler> handlerMap = new Dictionary<string, IDataForwardingContextHandler>();

    private HubConnection? serverHubConnection;
    private readonly ErrorHandler errorHandler;

    private bool Initialized { get; set; } = false;
    private Task<bool>? initializationTask = null;

    private bool disposedValue;

    public DataForwardingClient(
        Config.ServerConfig serverConfig,
        ICommunication communication,
        ErrorHandler errorHandler,
        IEnumerable<IDataForwardingContextHandler> dataForwardingContextHandlers)
    {
        this.serverConfig = serverConfig;
        this.communication = communication;
        this.errorHandler = errorHandler;

        foreach (IDataForwardingContextHandler handler in dataForwardingContextHandlers)
        {
            handler.Register(this);
        }

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

        serverHubConnection.On<string, string, string>("RequestDataFile", RequestDataFile);

        try
        {
            await serverHubConnection!.StartAsync();
            Initialized = true;

            foreach (IDataForwardingContextHandler handler in handlerMap.Values.Distinct())
            {
                await handler.Initialize(this);
            }
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

    void IDataForwardingRegistrar.RegisterHandler(string context, IDataForwardingContextHandler handler) => handlerMap.Add(context.ToUpper(), handler);


    Task IDataForwardingInitializer.ClearServerFileList(string context) =>
        serverHubConnection!.InvokeCoreAsync("ClearFileList", new object?[] { context });

    async Task IDataForwardingInitializer.UpdateServerFileList(string context, List<ServerDataFile> dataFiles)
    {
        const int pageSize = 500;
        int start = 0;

        do
        {
            await serverHubConnection!.InvokeCoreAsync("AppendFileList", new object?[] { context, dataFiles.Skip(start).Take(pageSize).ToList() });

            start += pageSize;
        }
        while (start <= dataFiles.Count);
    }

    private async Task RequestDataFile(string dataFileAlias, string context, string requestIdentifier)
    {
        if (!handlerMap.TryGetValue(context, out IDataForwardingContextHandler? handler))
        {
            await serverHubConnection!.InvokeCoreAsync("CancelFileTransfer", new object?[] { requestIdentifier, $"Datafile Handler for context {context} does not exist" });
            return;
        }

        string? dataFilePath = handler.GetDataFilePath(dataFileAlias, context);

        if (string.IsNullOrEmpty(dataFilePath))
        {
            await serverHubConnection!.InvokeCoreAsync("CancelFileTransfer", new object?[] { requestIdentifier, $"Datafile {dataFileAlias} does not exist in context {context}" });
            return;
        }

        using FileStream file = new FileStream(dataFilePath, FileMode.Open);
        int totalData = (int)file.Length;

        new FileExtensionContentTypeProvider().TryGetContentType(dataFilePath, out string? contentType);

        await serverHubConnection!.InvokeCoreAsync(
            methodName: "UploadDataFileMetaData",
            args: new object?[] { requestIdentifier, contentType, totalData });

        int dataPacketSize = Math.Min(totalData, 1 << 13);
        byte[] dataPacket = new byte[dataPacketSize];

        int bytesReady;
        //Now stream the file back to the requester
        while ((bytesReady = await file.ReadAsync(dataPacket)) > 0)
        {
            await serverHubConnection!.InvokeCoreAsync(
                methodName: "UploadDataFileData",
                args: new object?[] { requestIdentifier, dataPacket, bytesReady });
        }

        file.Close();
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
