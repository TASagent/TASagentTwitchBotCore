using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR.Client;

namespace TASagentTwitchBot.Core.EventSub;

public delegate Task EventHandler(JsonElement twitchEvent);

[AutoRegister]
public interface IEventSubSubscriber
{
    void RegisterHandlers(Dictionary<string, EventHandler> handlers);
}

public class EventSubHandler : IStartupListener, IDisposable
{
    private readonly ICommunication communication;
    private readonly HubConnection? serverHubConnection;
    private readonly ErrorHandler errorHandler;

    //Event handlers
    private readonly IEventSubSubscriber[] eventSubSubscribers;

    private readonly Dictionary<string, EventHandler> eventHandlers = new Dictionary<string, EventHandler>();

    private bool disposedValue;

    public EventSubHandler(
        Config.ServerConfig serverConfig,
        ICommunication communication,
        ErrorHandler errorHandler,
        IEnumerable<IEventSubSubscriber> eventSubSubscribers)
    {
        this.communication = communication;
        this.errorHandler = errorHandler;

        this.eventSubSubscribers = eventSubSubscribers.ToArray();

        foreach (IEventSubSubscriber subscriber in this.eventSubSubscribers)
        {
            subscriber.RegisterHandlers(eventHandlers);
        }

        if (this.eventSubSubscribers.Length == 0)
        {
            communication.SendDebugMessage($"No EventSub Listener registered. Skipping EventSub Websocket.");
        }
        else if (!string.IsNullOrEmpty(serverConfig.ServerAccessToken) &&
            !string.IsNullOrEmpty(serverConfig.ServerAddress) &&
            !string.IsNullOrEmpty(serverConfig.ServerUserName))
        {
            serverHubConnection = new HubConnectionBuilder()
                .WithUrl($"{serverConfig.ServerAddress}/Hubs/BotEventSubHub", options =>
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
            serverHubConnection.On<EventSubPayload>("ReceiveEvent", ReceiveEvent);

            Initialize();
        }
        else
        {
            communication.SendErrorMessage($"EventSub not configured, skipping. Register at https://server.tas.wtf/ and contact TASagent. " +
                $"Then update relevant details in Config/ServerConfig.json");
        }
    }

    public async void Initialize()
    {
        try
        {
            await serverHubConnection!.StartAsync();
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                communication.SendErrorMessage($"EventSub failed to connect due to permissions. Please contact TASagent to be given access.");
            }
            else
            {
                communication.SendErrorMessage($"EventSub failed to connect. Make sure settings are correct.");
            }

            errorHandler.LogSystemException(ex);
        }
        catch (Exception ex)
        {
            errorHandler.LogSystemException(ex);
            communication.SendErrorMessage($"EventSub failed to connect. Make sure settings are correct.");
            return;
        }

        HashSet<string> requiredEvents = new HashSet<string>(eventHandlers.Keys);

        //Make sure the server subs to relevant events
        await serverHubConnection!.InvokeAsync("ReportDesiredEventSubs", requiredEvents);
    }

    private Task ServerHubConnectionClosed(Exception? arg)
    {
        if (arg is not null)
        {
            errorHandler.LogSystemException(arg);
        }

        return Task.CompletedTask;
    }

    public void ReceiveMessage(string message) => communication.SendDebugMessage($"EventSub WebServer Message: {message}");
    public void ReceiveWarning(string message) => communication.SendWarningMessage($"EventSub WebServer Warning: {message}");
    public void ReceiveError(string message) => communication.SendErrorMessage($"EventSub WebServer Error: {message}");

    public async Task ReceiveEvent(EventSubPayload eventSubPayload)
    {
        if (eventHandlers.ContainsKey(eventSubPayload.EventType))
        {
            await eventHandlers[eventSubPayload.EventType](eventSubPayload.TwitchEvent);
        }
        else
        {
            communication.SendWarningMessage($"Received undesired EventSub event: {JsonSerializer.Serialize(eventSubPayload)}");
            await serverHubConnection!.InvokeAsync("ReportUndesiredEventSub", eventSubPayload.SubId);
        }
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

    public static EventSubConditionType GetEventSubConditionType(string eventType)
    {
        switch (eventType)
        {
            case "channel.update":
            case "channel.follow":
            case "channel.subscribe":
            case "channel.subscription.end":
            case "channel.subscription.gift":
            case "channel.subscription.message":
            case "channel.cheer":
            case "channel.ban":
            case "channel.unban":
            case "channel.moderator.add":
            case "channel.moderator.remove":
            case "channel.channel_points_custom_reward.add":
            case "channel.poll.begin":
            case "channel.poll.progress":
            case "channel.poll.end":
            case "channel.prediction.begin":
            case "channel.prediction.progress":
            case "channel.prediction.lock":
            case "channel.prediction.end":
            case "channel.goal.begin":
            case "channel.goal.progress":
            case "channel.goal.end":
            case "channel.hype_train.begin":
            case "channel.hype_train.progress":
            case "channel.hype_train.end":
            case "stream.online":
            case "stream.offline":
                return EventSubConditionType.BroadcasterUserId;

            case "channel.raid":
                return EventSubConditionType.FromBroadcasterUserId | EventSubConditionType.ToBroadcasterUserId;

            case "channel.channel_points_custom_reward.update":
            case "channel.channel_points_custom_reward.remove":
            case "channel.channel_points_custom_reward_redemption.add":
            case "channel.channel_points_custom_reward_redemption.update":
                return EventSubConditionType.BroadcasterUserId | EventSubConditionType.RewardId;

            case "user.authorization.grant":
            case "user.authorization.revoke":
                return EventSubConditionType.ClientId;

            case "user.update":
                return EventSubConditionType.UserId;

            case "extension.bits_transaction.create":
                return EventSubConditionType.ExtensionClientId;

            default: throw new NotSupportedException($"Unrecognized eventType: {eventType}");
        }
    }
}

[Flags]
public enum EventSubConditionType
{
    None = 0,
    BroadcasterUserId = 1,
    ToBroadcasterUserId = 1 << 1,
    FromBroadcasterUserId = 1 << 2,
    RewardId = 1 << 3,
    ClientId = 1 << 4,
    UserId = 1 << 5,
    ExtensionClientId = 1 << 6
}

public record EventSubPayload(
    [property: JsonPropertyName("sub_id")] string SubId,
    [property: JsonPropertyName("event_type")] string EventType,
    [property: JsonPropertyName("event")] JsonElement TwitchEvent);
