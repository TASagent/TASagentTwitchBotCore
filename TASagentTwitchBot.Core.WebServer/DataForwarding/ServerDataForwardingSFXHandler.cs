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
    void ReceiveSoundEffect(string requestIdentifier, string name, byte[] data, string? contentType);
    void CancelSoundEffect(string requestIdentifier, string reason);
}

public class ServerDataForwardingSFXHandler : IServerDataForwardingSFXHandler
{
    private readonly ILogger<ServerDataForwardingSFXHandler> logger;
    private readonly IDataForwardingConnectionManager connectionManager;
    private readonly IHubContext<Web.Hubs.BotDataForwardingHub> botDataForwardingHub;

    private readonly Dictionary<string, List<ServerSoundEffect>> soundEffectListLookup = new Dictionary<string, List<ServerSoundEffect>>();

    private readonly Dictionary<string, PendingDownload> waitingDownloads = new Dictionary<string, PendingDownload>();

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
            UserName: userName,
            SoundEffectAlias: soundEffectAlias,
            CompletionSource: completionSource,
            RequestTime: DateTime.Now));

        await client.SendAsync(
            method: "RequestSoundEffect",
            arg1: soundEffectAlias,
            arg2: requestIdentifier);

        return await completionSource.Task;
    }

    public void ReceiveSoundEffect(string requestIdentifier, string name, byte[] data, string? contentType)
    {
        if (!waitingDownloads.TryGetValue(requestIdentifier, out PendingDownload? pendingDownload))
        {
            logger.LogWarning("Received unexpected SoundEffect data for identifier: {requestIdentifier}", requestIdentifier);
            return;
        }

        waitingDownloads.Remove(requestIdentifier);

        ServerSoundEffectData soundEffectData = new ServerSoundEffectData(
            Name: name,
            Data: data,
            ContentType: contentType);

        pendingDownload.CompletionSource.SetResult(soundEffectData);
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
        string UserName,
        string SoundEffectAlias,
        TaskCompletionSource<ServerSoundEffectData?> CompletionSource,
        DateTime RequestTime);
}
