using Amazon.Runtime.Internal.Transform;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.WebServer.Web;

namespace TASagentTwitchBot.Core.WebServer.TTS;

[AutoRegister]
public interface IServerDataForwardingSFXHandler
{
    void UpdateSoundEffectList(string userName, List<ServerSoundEffect> soundEffects);
    List<ServerSoundEffect> GetSoundEffectList(string userName);
    Task<ServerSoundEffectData?> GetSoundEffectByAlias(string userName, string soundEffectString);
    void ReceiveSoundEffectMetaData(string requestIdentifier, string? contentType, int totalBytes);
    void ReceiveSoundEffectData(string requestIdentifier, byte[] data, int current);
    void CancelSoundEffect(string requestIdentifier, string reason);
}

public class ServerDataForwardingSFXHandler : IServerDataForwardingSFXHandler
{
    private readonly ILogger<ServerDataForwardingSFXHandler> logger;
    private readonly IDataForwardingConnectionManager connectionManager;
    private readonly IHubContext<Web.Hubs.BotDataForwardingHub> botDataForwardingHub;

    private readonly Dictionary<string, List<ServerSoundEffect>> soundEffectListLookup = new Dictionary<string, List<ServerSoundEffect>>();

    private readonly Dictionary<string, PendingDownload> waitingDownloads = new Dictionary<string, PendingDownload>();
    private readonly Dictionary<string, OngoingDownload> ongoingDownloads = new Dictionary<string, OngoingDownload>();


    public ServerDataForwardingSFXHandler(
        ILogger<ServerDataForwardingSFXHandler> logger,
        IDataForwardingConnectionManager connectionManager,
        IHubContext<Web.Hubs.BotDataForwardingHub> botDataForwardingHub)
    {
        this.logger = logger;
        this.connectionManager = connectionManager;
        this.botDataForwardingHub = botDataForwardingHub;
    }

    public List<ServerSoundEffect> GetSoundEffectList(string userName)
    {
        userName = userName.ToLower();

        if (!soundEffectListLookup.TryGetValue(userName, out List<ServerSoundEffect>? value))
        {
            value = new List<ServerSoundEffect>();
        }

        return value;
    }

    public void UpdateSoundEffectList(string userName, List<ServerSoundEffect> soundEffects) =>
        soundEffectListLookup[userName.ToLower()] = soundEffects;

    public async Task<ServerSoundEffectData?> GetSoundEffectByAlias(string userName, string soundEffectAlias)
    {
        if (!connectionManager.TryGetClient(botDataForwardingHub, userName, out ISingleClientProxy? client))
        {
            return null;
        }

        string requestIdentifier = Guid.NewGuid().ToString();

        TaskCompletionSource<ServerSoundEffectData?> completionSource =
            new TaskCompletionSource<ServerSoundEffectData?>();

        waitingDownloads.Add(requestIdentifier, new PendingDownload(
            CompletionSource: completionSource,
            RequestTime: DateTime.Now));

        await client.SendAsync(
            method: "RequestSoundEffect",
            arg1: soundEffectAlias,
            arg2: requestIdentifier);

        ServerSoundEffectData? data = await completionSource.Task;

        logger.LogWarning("Received sound effect {name} data of {bytes} bytes. Replying", soundEffectAlias, data?.Data?.Length ?? 0);

        return data;
    }

    public void ReceiveSoundEffectMetaData(string requestIdentifier, string? contentType, int totalBytes)
    {
        if (!waitingDownloads.TryGetValue(requestIdentifier, out PendingDownload? pendingDownload))
        {
            logger.LogWarning("Received unexpected SoundEffect data for identifier: {requestIdentifier}", requestIdentifier);
            return;
        }

        waitingDownloads.Remove(requestIdentifier);

        ongoingDownloads.Add(requestIdentifier, new OngoingDownload(totalBytes, contentType, pendingDownload.CompletionSource));
    }

    public void ReceiveSoundEffectData(string requestIdentifier, byte[] data, int current)
    {
        if (!ongoingDownloads.TryGetValue(requestIdentifier, out OngoingDownload? ongoingDownload))
        {
            logger.LogWarning("Received unexpected SoundEffect data for identifier: {requestIdentifier}", requestIdentifier);
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

            ongoingDownload.CompletionSource.SetResult(new ServerSoundEffectData(
                Data: ongoingDownload.Data,
                ContentType: ongoingDownload.ContentType));
        }
    }

    public void CancelSoundEffect(string requestIdentifier, string reason)
    {
        if (waitingDownloads.TryGetValue(requestIdentifier, out PendingDownload? pendingDownload))
        {
            pendingDownload.CompletionSource.SetResult(null);
            waitingDownloads.Remove(requestIdentifier);
        }

        logger.LogWarning("SoundEffect Download request {requestIdentifier} cancelled: {reason}", requestIdentifier, reason);
    }

    private record PendingDownload(
        TaskCompletionSource<ServerSoundEffectData?> CompletionSource,
        DateTime RequestTime);

    private class OngoingDownload
    {
        public byte[] Data { get; init; }
        public int Downloaded { get; set; } = 0;
        public TaskCompletionSource<ServerSoundEffectData?> CompletionSource { get; init; }
        public string? ContentType { get; init; }

        public OngoingDownload(
            int size,
            string? contentType,
            TaskCompletionSource<ServerSoundEffectData?> completionSource)
        {
            Data = new byte[size];
            ContentType = contentType;
            CompletionSource = completionSource;
        }
    }
}
