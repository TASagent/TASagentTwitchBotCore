using Amazon.Runtime.Internal.Transform;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.WebServer.Web;

namespace TASagentTwitchBot.Core.WebServer.TTS;

[AutoRegister]
public interface IServerDataForwardingHandler
{
    void AppendFileList(string userName, string context, List<ServerDataFile> dataFiles);
    void ClearFileList(string userName, string context);
    List<ServerDataFile> GetDataFileList(string userName, string context);
    Task<ServerFileData?> GetDataFileByAlias(string userName, string context, string dataFileAlias);
    void ReceiveFileMetaData(string requestIdentifier, string? contentType, int totalBytes);
    void ReceiveFileData(string requestIdentifier, byte[] data, int current);
    void CancelFileTransfer(string requestIdentifier, string reason);
}

public class ServerDataForwardingHandler : IServerDataForwardingHandler
{
    private readonly ILogger<ServerDataForwardingHandler> logger;
    private readonly IDataForwardingConnectionManager connectionManager;
    private readonly IHubContext<Web.Hubs.BotDataForwardingHub> botDataForwardingHub;

    private readonly Dictionary<string, StreamerDataSet> streamerDataSetLookup = new Dictionary<string, StreamerDataSet>();

    private readonly Dictionary<string, PendingDownload> waitingDownloads = new Dictionary<string, PendingDownload>();
    private readonly Dictionary<string, OngoingDownload> ongoingDownloads = new Dictionary<string, OngoingDownload>();


    public ServerDataForwardingHandler(
        ILogger<ServerDataForwardingHandler> logger,
        IDataForwardingConnectionManager connectionManager,
        IHubContext<Web.Hubs.BotDataForwardingHub> botDataForwardingHub)
    {
        this.logger = logger;
        this.connectionManager = connectionManager;
        this.botDataForwardingHub = botDataForwardingHub;
    }

    public List<ServerDataFile> GetDataFileList(string userName, string context)
    {
        userName = userName.ToUpper();
        context = context.ToUpper();

        if (streamerDataSetLookup.TryGetValue(userName, out StreamerDataSet? streamer))
        {
            return streamer.GetDataFileList(context);
        }

        return new List<ServerDataFile>();
    }

    public void AppendFileList(string userName, string context, List<ServerDataFile> dataFiles)
    {
        userName = userName.ToUpper();
        context = context.ToUpper();

        if (!streamerDataSetLookup.TryGetValue(userName, out StreamerDataSet? streamer))
        {
            streamer = new StreamerDataSet(userName);
            streamerDataSetLookup[userName] = streamer;
        }

        streamer.AppendFileList(context, dataFiles);
    }

    public void ClearFileList(string userName, string context)
    {
        userName = userName.ToUpper();
        context = context.ToUpper();

        if (streamerDataSetLookup.TryGetValue(userName, out StreamerDataSet? streamer))
        {
            streamer.ClearFileList(context);
        }
    }

    public async Task<ServerFileData?> GetDataFileByAlias(string userName, string context, string dataFileAlias)
    {
        userName = userName.ToUpper();
        context = context.ToUpper();

        if (!connectionManager.TryGetClient(botDataForwardingHub, userName, out ISingleClientProxy? client))
        {
            return null;
        }

        string requestIdentifier = Guid.NewGuid().ToString();

        TaskCompletionSource<ServerFileData?> completionSource =
            new TaskCompletionSource<ServerFileData?>();

        waitingDownloads.Add(requestIdentifier, new PendingDownload(
            CompletionSource: completionSource,
            RequestTime: DateTime.Now));

        await client.SendCoreAsync(
            method: "RequestDataFile",
            args: new object?[] { dataFileAlias, context, requestIdentifier });

        return await completionSource.Task;
    }

    public void ReceiveFileMetaData(string requestIdentifier, string? contentType, int totalBytes)
    {
        if (!waitingDownloads.TryGetValue(requestIdentifier, out PendingDownload? pendingDownload))
        {
            logger.LogWarning("Received unexpected DataFile data for identifier: {requestIdentifier}", requestIdentifier);
            return;
        }

        waitingDownloads.Remove(requestIdentifier);

        ongoingDownloads.Add(requestIdentifier, new OngoingDownload(totalBytes, contentType, pendingDownload.CompletionSource));
    }

    public void ReceiveFileData(string requestIdentifier, byte[] data, int current)
    {
        if (!ongoingDownloads.TryGetValue(requestIdentifier, out OngoingDownload? ongoingDownload))
        {
            logger.LogWarning("Received unexpected DataFile data for identifier: {requestIdentifier}", requestIdentifier);
            return;
        }

        Array.Copy(
            sourceArray: data,
            sourceIndex: 0,
            destinationArray: ongoingDownload.Data!,
            destinationIndex: ongoingDownload.Downloaded,
            length: current);

        ongoingDownload.Downloaded += current;

        if (ongoingDownload.Downloaded == ongoingDownload.Data.Length)
        {
            //Download finished
            ongoingDownloads.Remove(requestIdentifier);

            ongoingDownload.CompletionSource.SetResult(new ServerFileData(
                Data: ongoingDownload.Data,
                ContentType: ongoingDownload.ContentType));
        }
    }

    public void CancelFileTransfer(string requestIdentifier, string reason)
    {
        if (waitingDownloads.TryGetValue(requestIdentifier, out PendingDownload? pendingDownload))
        {
            pendingDownload.CompletionSource.SetResult(null);
            waitingDownloads.Remove(requestIdentifier);
        }

        logger.LogWarning("DataFile Download request {requestIdentifier} cancelled: {reason}", requestIdentifier, reason);
    }

    private class StreamerDataSet
    {
        public string UserName { get; }
        private Dictionary<string, List<ServerDataFile>> FileLookup { get; } = new Dictionary<string, List<ServerDataFile>>();

        public StreamerDataSet(string userName)
        {
            UserName = userName;
        }

        public List<ServerDataFile> GetDataFileList(string context)
        {
            if (FileLookup.TryGetValue(context.ToUpper(), out List<ServerDataFile>? dataFileList))
            {
                return dataFileList;
            }

            return new List<ServerDataFile>();
        }

        public void AppendFileList(string context, List<ServerDataFile> newDataFileList)
        {
            context = context.ToUpper();

            if (!FileLookup.TryGetValue(context, out List<ServerDataFile>? dataFileList))
            {
                dataFileList = new List<ServerDataFile>();
                FileLookup.Add(context, dataFileList);
            }

            dataFileList.AddRange(newDataFileList);
        }

        public void ClearFileList(string context)
        {
            context = context.ToUpper();

            if (FileLookup.TryGetValue(context, out List<ServerDataFile>? dataFileList))
            {
                dataFileList.Clear();
            }
        }
    }

    private record PendingDownload(
        TaskCompletionSource<ServerFileData?> CompletionSource,
        DateTime RequestTime);

    private class OngoingDownload
    {
        public byte[] Data { get; init; }
        public int Downloaded { get; set; } = 0;
        public TaskCompletionSource<ServerFileData?> CompletionSource { get; init; }
        public string? ContentType { get; init; }

        public OngoingDownload(
            int size,
            string? contentType,
            TaskCompletionSource<ServerFileData?> completionSource)
        {
            Data = new byte[size];
            ContentType = contentType;
            CompletionSource = completionSource;
        }
    }
}
