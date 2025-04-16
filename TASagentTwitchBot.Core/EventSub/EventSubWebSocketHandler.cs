using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using System.Text.Json.Serialization;
using static TASagentTwitchBot.Core.EventSub.WelcomePayload;

namespace TASagentTwitchBot.Core.EventSub;

public class EventSubWebSocketHandler : IStartupListener, IShutdownListener, IDisposable
{
    private readonly API.Twitch.HelixHelper helixHelper;

    private readonly ICommunication communication;
    private readonly API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator;

    private readonly ErrorHandler errorHandler;

    //Event handlers
    private readonly IEventSubSubscriber[] eventSubSubscribers;

    private readonly Dictionary<string, EventHandler> eventHandlers = new Dictionary<string, EventHandler>();

    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
    private readonly CancellationTokenSource disconnectTokenSource = new CancellationTokenSource();

    private readonly Uri websocketServerURI = new Uri("wss://eventsub.wss.twitch.tv/ws");
    private ClientWebSocket? clientWebSocket = null;
    private Uri? newWebsocketServerURI = null;
    private ClientWebSocket? tempClientWebSocket = null;

    private Task? readMessagesTask = null;
    private Task? disconnectionTask = null;

    private readonly byte[] incomingData = new byte[4096];

    private bool disposedValue = false;

    public EventSubWebSocketHandler(
        ApplicationManagement applicationManagement,
        API.Twitch.HelixHelper helixHelper,
        ICommunication communication,
        API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator,
        IEnumerable<IEventSubSubscriber> eventSubSubscribers,
        ErrorHandler errorHandler)
    {
        this.helixHelper = helixHelper;
        this.communication = communication;
        this.broadcasterTokenValidator = broadcasterTokenValidator;
        this.errorHandler = errorHandler;

        applicationManagement.RegisterShutdownListener(this);

        this.eventSubSubscribers = eventSubSubscribers.ToArray();

        if (this.eventSubSubscribers.Length == 0)
        {
            communication.SendDebugMessage($"No EventSub Listener registered. Skipping EventSub Websocket.");
            return;
        }

        //Set up websocket connection
        Task.Run(Launch);
    }

    private async Task Launch()
    {
        foreach (IEventSubSubscriber subscriber in this.eventSubSubscribers)
        {
            await subscriber.RegisterHandlers(eventHandlers);
        }

        bool tokenValidated = await broadcasterTokenValidator.WaitForValidationAsync(generalTokenSource.Token);

        if (!tokenValidated)
        {
            communication.SendErrorMessage("Unable to connect to EventSub - requires validated Broadcaster token. ABORTING.");
            throw new Exception("Unable to connect to EventSub - requires validated Broadcaster token. ABORTING.");
        }

        await Connect();

        readMessagesTask = ReadMessages();
    }

    private async Task Connect()
    {
        clientWebSocket = new ClientWebSocket();

        await clientWebSocket.ConnectAsync(newWebsocketServerURI ?? websocketServerURI, generalTokenSource.Token);

        if (clientWebSocket.State != WebSocketState.Open)
        {
            communication.SendErrorMessage("Unable to connect to EventSub. ABORTING.");
            throw new Exception("Unable to connect to EventSub. ABORTING.");
        }
    }

    private async Task ReadMessages()
    {
        WebSocketReceiveResult? webSocketReceiveResult = null;
        string? lastMessage = null;

        try
        {
            while (true)
            {
                if (generalTokenSource.IsCancellationRequested)
                {
                    //We are quitting
                    break;
                }

                if (clientWebSocket!.State != WebSocketState.Open)
                {
                    //We've disconnected
                    await Connect();
                }

                try
                {
                    webSocketReceiveResult = await clientWebSocket!.ReceiveAsync(incomingData, generalTokenSource.Token);
                }
                catch (OperationCanceledException) { /* swallow */ }
                catch (ThreadAbortException) { /* swallow */ }
                catch (ObjectDisposedException) { /* swallow */ }
                catch (WebSocketException)
                {
                    communication.SendWarningMessage($"EventSub Websocket closed unexpectedly.");
                    continue;
                }
                catch (Exception ex)
                {
                    communication.SendErrorMessage($"EventSub Exception: {ex.GetType().Name}");
                    errorHandler.LogMessageException(ex, "");
                    continue;
                }

                if (generalTokenSource.IsCancellationRequested)
                {
                    //We are quitting
                    break;
                }

                if (webSocketReceiveResult!.Count < 1)
                {
                    await Task.Delay(100, generalTokenSource.Token);
                    continue;
                }

                lastMessage = Encoding.UTF8.GetString(incomingData, 0, webSocketReceiveResult.Count);
                bool readSuccess = true;

                while (!webSocketReceiveResult.EndOfMessage)
                {
                    try
                    {
                        webSocketReceiveResult = await clientWebSocket!.ReceiveAsync(incomingData, generalTokenSource.Token);
                    }
                    catch (OperationCanceledException) { /* swallow */ }
                    catch (ThreadAbortException) { /* swallow */ }
                    catch (ObjectDisposedException) { /* swallow */ }
                    catch (WebSocketException)
                    {
                        communication.SendWarningMessage($"EventSub Websocket closed unexpectedly.");
                        readSuccess = false;
                        break;
                    }
                    catch (Exception ex)
                    {
                        communication.SendErrorMessage($"EventSub Exception: {ex.GetType().Name}");
                        errorHandler.LogMessageException(ex, "");
                        readSuccess = false;
                        break;
                    }

                    if (generalTokenSource.IsCancellationRequested)
                    {
                        //We are quitting
                        break;
                    }

                    if (webSocketReceiveResult.Count < 1)
                    {
                        communication.SendWarningMessage($"WebSocketMessage returned no characters despite not being at end.  {lastMessage}");
                        break;
                    }

                    lastMessage += Encoding.UTF8.GetString(incomingData, 0, webSocketReceiveResult.Count);
                }

                if (!readSuccess)
                {
                    continue;
                }

                if (generalTokenSource.IsCancellationRequested)
                {
                    //We are quitting
                    break;
                }

                BasicEventSubMessage message = JsonSerializer.Deserialize<BasicEventSubMessage>(lastMessage)!;

                switch (message.MetaData.MessageType)
                {
                    case "session_welcome":
                        {
                            WelcomePayload? welcomePayload = message.Payload.Deserialize<WelcomePayload>();

                            if (welcomePayload is null)
                            {
                                communication.SendErrorMessage($"EventSub error decoding welcome payload: {lastMessage}");
                                break;
                            }

                            if (tempClientWebSocket is not null)
                            {
                                if (tempClientWebSocket.State == WebSocketState.Open)
                                {
                                    await tempClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", disconnectTokenSource.Token);
                                }

                                tempClientWebSocket.Dispose();
                                tempClientWebSocket = null;
                                newWebsocketServerURI = null;

                                //Skip subscribing when we reconnect
                                break;
                            }

                            string sessionId = welcomePayload.Session.Id;

                            foreach (string subType in eventHandlers.Keys)
                            {
                                API.Twitch.TwitchSubscribeResponse? response = await helixHelper.EventSubSubscribe(
                                    subscriptionType: subType,
                                    sessionId: sessionId);

                                if (response is null)
                                {
                                    communication.SendErrorMessage($"EventSub error subscribing to {subType}.");
                                }
                            }
                        }
                        break;

                    case "session_reconnect":
                        {
                            communication.SendDebugMessage($"EventSub Reconnect Message Received");

                            WelcomePayload? welcomePayload = message.Payload.Deserialize<WelcomePayload>();

                            if (welcomePayload is null)
                            {
                                communication.SendErrorMessage($"EventSub error decoding welcome payload: {lastMessage}");
                                break;
                            }

                            //Cache new URI
                            newWebsocketServerURI = new Uri(welcomePayload!.Session.ReconnectURL!);
                            //Cache current connection
                            tempClientWebSocket = clientWebSocket;

                            await Connect();
                        }
                        break;

                    case "notification":
                        {
                            NotificationPayload? payload = message.Payload.Deserialize<NotificationPayload>();

                            if (payload is null)
                            {
                                communication.SendErrorMessage($"EventSub error decoding payload: {lastMessage}");
                                break;
                            }

                            if (eventHandlers.TryGetValue(payload.Subscription.Type, out EventHandler? handler))
                            {
                                handler?.Invoke(payload.Event);
                            }
                        }
                        break;

                    case "session_keepalive":
                        {
                            //Do nothing - for now!
                        }
                        break;

                    default:
                        communication.SendErrorMessage($"Unsupported EventSub Message Type: {message.MetaData.MessageType} - {lastMessage}");
                        break;
                }
            }
        }
        catch (OperationCanceledException) { /* swallow */ }
        catch (ThreadAbortException) { /* swallow */ }
        catch (ObjectDisposedException) { /* swallow */ }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"EventSub Exception: {ex.GetType().Name}");
            if (lastMessage is not null)
            {
                communication.SendErrorMessage($"Last EventSub Message: {lastMessage}");
            }

            errorHandler.LogMessageException(ex, "");
        }
    }

    public void NotifyShuttingDown()
    {
        generalTokenSource.Cancel();

        disconnectionTask = Disconnect();
    }

    private async Task Disconnect()
    {
        if (clientWebSocket is not null)
        {
            if (clientWebSocket.State == WebSocketState.Open)
            {
                await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", disconnectTokenSource.Token);
            }

            clientWebSocket.Dispose();
            clientWebSocket = null;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                generalTokenSource.Cancel();

                if (disconnectionTask is null)
                {
                    disconnectionTask = Disconnect();
                }

                List<Task> tasks = new List<Task>() { disconnectionTask, readMessagesTask! };

                Task.WaitAll(tasks.Where(x => x is not null).ToArray(), 2_000);

                disconnectTokenSource.Cancel();

                disconnectionTask = null;
                readMessagesTask = null;

                generalTokenSource.Dispose();

                disconnectTokenSource.Dispose();

                clientWebSocket?.Dispose();
                clientWebSocket = null;
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

public record EventSubMetadata(
    [property: JsonPropertyName("message_id")] string MessageId,
    [property: JsonPropertyName("message_type")] string MessageType,
    [property: JsonPropertyName("message_timestamp")] DateTime MessageTimestamp);

public record BasicEventSubMessage(
    [property: JsonPropertyName("metadata")] EventSubMetadata MetaData,
    [property: JsonPropertyName("payload")] JsonElement Payload);

public record NotificationPayload(
    [property: JsonPropertyName("subscription")] NotificationPayload.SubscriptionDatum Subscription,
    [property: JsonPropertyName("event")] JsonElement Event)
{
    public record SubscriptionDatum(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("verson")] string Version,
        [property: JsonPropertyName("cost")] int Cost,
        [property: JsonPropertyName("condition")] JsonElement Condition,
        [property: JsonPropertyName("transport")] JsonElement Transport,
        [property: JsonPropertyName("created_at")] DateTime CreatedAt);
};

public record WelcomePayload(
    [property: JsonPropertyName("session")] SessionData Session)
{
    public record SessionData(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("connected_at")] DateTime ConnectedAt,
        [property: JsonPropertyName("keepalive_timeout_seconds")] int? KeepAliveTime = null,
        [property: JsonPropertyName("reconnect_url")] string? ReconnectURL = null);
};