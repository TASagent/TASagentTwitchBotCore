using System.Text.Json;

using TASagentTwitchBot.Core.API.Twitch;

namespace TASagentTwitchBot.Core.EventSub;

[AutoRegister]
public interface IStreamLiveListener
{
    void NotifyLiveStatus(bool isLive);
}

[AutoRegister]
public interface IStreamDetailListener
{
    void NotifyStreamDetailUpdate(StreamUpdateData streamData);
}

public class StreamChangeSubscriber : IEventSubSubscriber
{
    private readonly Config.BotConfiguration botConfig;
    private readonly HelixHelper helixHelper;

    private readonly IStreamLiveListener[] streamLiveListeners;
    private readonly IStreamDetailListener[] streamDetailListeners;

    private StreamUpdateData? currentStreamData = null;

    public StreamChangeSubscriber(
        Config.BotConfiguration botConfig,
        HelixHelper helixHelper,
        IEnumerable<IStreamLiveListener> streamLiveListeners,
        IEnumerable<IStreamDetailListener> streamDetailListeners)
    {
        this.botConfig = botConfig;

        this.helixHelper = helixHelper;

        this.streamLiveListeners = streamLiveListeners.ToArray();
        this.streamDetailListeners = streamDetailListeners.ToArray();

        Initialize();
    }

    public void RegisterHandlers(Dictionary<string, EventHandler> handlers)
    {
        if (streamDetailListeners.Length > 0)
        {
            handlers.Add("channel.update", ChannelUpdateHandler);
        }

        if (streamLiveListeners.Length > 0)
        {
            handlers.Add("stream.online", StreamOnlineHandler);
            handlers.Add("stream.offline", StreamOfflineHandler);
        }
    }

    private async void Initialize()
    {
        //Send initial updates

        TwitchStreams? streamData = await helixHelper.GetStreams(userIDs: new List<string>() { botConfig.BroadcasterId }) ??
            throw new Exception("Unable to query Stream");

        if (streamData.Data is null || streamData.Data.Count == 0)
        {
            currentStreamData = null;
        }
        else
        {
            currentStreamData = new StreamUpdateData(
                BroadcasterId: streamData.Data[0].UserID,
                BroadcasterUserName: streamData.Data[0].UserName,
                Title: streamData.Data[0].Title,
                CategoryId: streamData.Data[0].GameID,
                CategoryName: streamData.Data[0].GameName,
                IsMature: streamData.Data[0].IsMature);

            foreach (IStreamDetailListener detailListener in streamDetailListeners)
            {
                detailListener.NotifyStreamDetailUpdate(currentStreamData);
            }
        }

        //Update LiveListeners
        foreach (IStreamLiveListener liveListener in streamLiveListeners)
        {
            liveListener.NotifyLiveStatus(currentStreamData is not null);
        }
    }

    public Task StreamOnlineHandler(JsonElement eventData)
    {
        foreach (IStreamLiveListener liveListener in streamLiveListeners)
        {
            liveListener.NotifyLiveStatus(true);
        }

        return Task.CompletedTask;
    }

    public Task StreamOfflineHandler(JsonElement eventData)
    {
        foreach (IStreamLiveListener liveListener in streamLiveListeners)
        {
            liveListener.NotifyLiveStatus(false);
        }

        return Task.CompletedTask;
    }

    public Task ChannelUpdateHandler(JsonElement eventData)
    {
        StreamUpdateData newStreamData = FromEventUpdate(eventData);

        if (currentStreamData == newStreamData)
        {
            //No Change
            return Task.CompletedTask;
        }

        currentStreamData = newStreamData;

        foreach (IStreamDetailListener detailListener in streamDetailListeners)
        {
            detailListener.NotifyStreamDetailUpdate(currentStreamData);
        }

        return Task.CompletedTask;
    }

    private static StreamUpdateData FromEventUpdate(JsonElement eventData) =>
        new StreamUpdateData(
            BroadcasterId: eventData.GetProperty("broadcaster_user_id").GetString()!,
            BroadcasterUserName: eventData.GetProperty("broadcaster_user_name").GetString()!,
            Title: eventData.GetProperty("title").GetString()!,
            CategoryId: eventData.GetProperty("category_id").GetString()!,
            CategoryName: eventData.GetProperty("category_name").GetString()!,
            IsMature: eventData.GetProperty("is_mature").GetBoolean());
}

public record StreamUpdateData(
    string BroadcasterId,
    string BroadcasterUserName,
    string Title,
    string CategoryId,
    string CategoryName,
    bool IsMature);
