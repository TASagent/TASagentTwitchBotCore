using System.Text;
using System.Text.Json;
using System.Net.WebSockets;
using BGC.Collections.Generic;

namespace TASagentTwitchBot.Core.PubSub;

public class PubSubClient : IStartupListener, IShutdownListener, IDisposable
{
    private readonly Config.BotConfiguration botConfig;
    private readonly ErrorHandler errorHandler;

    private readonly ICommunication communication;
    private readonly IRedemptionSystem redemptionHandler;
    private readonly API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator;

    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
    private readonly CancellationTokenSource disconnectTokenSource = new CancellationTokenSource();
    private CancellationTokenSource? readerTokenSource;

    private ClientWebSocket? clientWebSocket;

    private Task? connectionTask = null;
    private Task? disconnectionTask = null;
    private Task? handlePingsTask = null;
    private Task? readMessagesTask = null;

    private bool disposedValue = false;
    private bool breakConnection = false;

    private readonly Uri websocketServerURI = new Uri("wss://pubsub-edge.twitch.tv");
    private readonly BasicPubSubMessage pingMessage = new BasicPubSubMessage("PING");
    private readonly BasicPubSubMessage pongMessage = new BasicPubSubMessage("PONG");

    private readonly TimeSpan pingWaitTime = new TimeSpan(hours: 0, minutes: 4, seconds: 00);

    private readonly RingBuffer<PubSubMessage> sentMessages = new RingBuffer<PubSubMessage>(20);

    private readonly byte[] incomingData = new byte[4096];
    private bool pongReceived = false;

    public PubSubClient(
        Config.BotConfiguration botConfig,
        ApplicationManagement applicationManagement,
        ICommunication communication,
        IRedemptionSystem redemptionHandler,
        API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator,
        ErrorHandler errorHandler)
    {
        this.botConfig = botConfig;
        this.communication = communication;
        this.redemptionHandler = redemptionHandler;
        this.broadcasterTokenValidator = broadcasterTokenValidator;
        this.errorHandler = errorHandler;

        applicationManagement.RegisterShutdownListener(this);

        Task.Run(Launch);
    }

    private async void Launch()
    {
        bool tokenValidated = await broadcasterTokenValidator.WaitForValidationAsync(generalTokenSource.Token);

        if (!tokenValidated)
        {
            communication.SendErrorMessage("Unable to connect to PubSub - requires validated Broadcaster token. ABORTING.");
            throw new Exception("Unable to connect to PubSub - requires validated Broadcaster token. ABORTING.");
        }

        await redemptionHandler.Initialize();

        await Connect();

        handlePingsTask = HandlePings();
        readMessagesTask = ReadMessages();
    }

    private async Task Connect()
    {
        readerTokenSource = new CancellationTokenSource();
        clientWebSocket = new ClientWebSocket();

        await clientWebSocket.ConnectAsync(websocketServerURI, generalTokenSource.Token);

        if (clientWebSocket.State != WebSocketState.Open)
        {
            communication.SendErrorMessage("Unable to connect to PubSub. ABORTING.");
            throw new Exception("Unable to connect to PubSub. ABORTING.");
        }

        ListenMessage listenMessage = new ListenMessage(
            topics: new[] { $"channel-points-channel-v1.{botConfig.BroadcasterId}" },
            authToken: botConfig.BroadcasterAccessToken);

        await SendMessage(listenMessage);
    }

    private async Task HandleReconnect()
    {
        try
        {
            communication.SendDebugMessage("Attempting PubSub Reconnect");

            int timeout = 1000;

            while (true)
            {
                if (generalTokenSource.IsCancellationRequested)
                {
                    //We are quitting, breakout
                    return;
                }

                try
                {
                    //Ensure we are disconnected
                    await Disconnect();
                }
                catch (Exception) { /* swallow */ }

                try
                {
                    await Task.Delay(timeout, generalTokenSource.Token);

                    if (generalTokenSource.IsCancellationRequested)
                    {
                        //We are quitting, breakout
                        return;
                    }

                    await Connect();

                    //Successfully Reconnected?
                    break;
                }
                catch (Exception ex)
                {
                    errorHandler.LogSystemException(ex);
                }

                //Cap timeout at 2 minutes
                timeout = Math.Max(2 * timeout, 120_000);
            }

            communication.SendDebugMessage("PubSub Reconnect Success");
        }
        catch (OperationCanceledException) { /* swallow */ }
        catch (Exception ex)
        {
            errorHandler.LogSystemException(ex);
        }
    }

    private async Task Disconnect()
    {
        //Kill ongoing readers
        readerTokenSource?.Cancel();

        //Make sure readers move
        await Task.Delay(250);

        if (clientWebSocket is not null)
        {
            if (clientWebSocket.State == WebSocketState.Open)
            {
                await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", disconnectTokenSource.Token);
            }
            
            clientWebSocket.Dispose();
            clientWebSocket = null;
        }

        readerTokenSource?.Dispose();
        readerTokenSource = null;
    }

    public async Task SendMessage<T>(T message)
    {
        if (message is PubSubMessage pubSubMessage)
        {
            sentMessages.Add(pubSubMessage);
        }

        if (clientWebSocket is not null)
        {
            await clientWebSocket.SendAsync(
                buffer: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)),
                messageType: WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: generalTokenSource.Token);
        }
        else
        {
            communication.SendWarningMessage($"Tried to send PubSub message with no connection: {Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message))}");
        }
    }

    private async Task HandlePings()
    {
        try
        {
            while (true)
            {
                //Check if we're connected
                await CheckConnectionOrWait();

                //Reset pongReceived flag
                pongReceived = false;

                //Send the ping
                await SendMessage(pingMessage);

                //Wait 10 seconds
                await Task.Delay(10 * 1000, generalTokenSource.Token);

                //Bail if we're trying to quit
                if (generalTokenSource.IsCancellationRequested)
                {
                    break;
                }

                if (!pongReceived)
                {
                    communication.SendDebugMessage($"No PubSub Pong received in 10 seconds - Reconnecting");

                    //Reconnect
                    breakConnection = true;
                    await CheckConnectionOrWait();
                }

                await Task.Delay(pingWaitTime, generalTokenSource.Token);
            }
        }
        catch (OperationCanceledException) { /* swallow */ }
        catch (ThreadAbortException) { /* swallow */ }
        catch (ObjectDisposedException) { /* swallow */ }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"PubSub Server Pinger Exception: {ex.GetType().Name}");
            errorHandler.LogMessageException(ex, "");
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
                await CheckConnectionOrWait();
                bool readCompleted = false;

                try
                {
                    webSocketReceiveResult = await clientWebSocket!.ReceiveAsync(incomingData, readerTokenSource!.Token);
                    readCompleted = true;
                }
                catch (OperationCanceledException) { /* swallow */ }
                catch (ThreadAbortException) { /* swallow */ }
                catch (ObjectDisposedException) { /* swallow */ }
                catch (WebSocketException)
                {
                    communication.SendWarningMessage($"PubSub Websocket closed unexpectedly.");
                }
                catch (Exception ex)
                {
                    communication.SendErrorMessage($"PubSub Exception: {ex.GetType().Name}");
                    errorHandler.LogMessageException(ex, "");
                }

                if (generalTokenSource.IsCancellationRequested)
                {
                    //We are quitting
                    break;
                }

                if ((readerTokenSource?.IsCancellationRequested ?? true) || !readCompleted)
                {
                    //We are just restarting reader, since it was intercepted with an exception or the reader token source was cancelled
                    continue;
                }

                if (webSocketReceiveResult!.Count < 1)
                {
                    await Task.Delay(100, generalTokenSource.Token);
                    continue;
                }

                lastMessage = Encoding.UTF8.GetString(incomingData, 0, webSocketReceiveResult.Count);

                while (!webSocketReceiveResult.EndOfMessage)
                {
                    readCompleted = false;
                    try
                    {
                        webSocketReceiveResult = await clientWebSocket!.ReceiveAsync(incomingData, readerTokenSource.Token);
                        readCompleted = true;
                    }
                    catch (OperationCanceledException) { /* swallow */ }
                    catch (ThreadAbortException) { /* swallow */ }
                    catch (ObjectDisposedException) { /* swallow */ }
                    catch (WebSocketException)
                    {
                        communication.SendWarningMessage($"PubSub Websocket closed unexpectedly.");
                    }
                    catch (Exception ex)
                    {
                        communication.SendErrorMessage($"PubSub Exception: {ex.GetType().Name}");
                        errorHandler.LogMessageException(ex, "");
                    }

                    if (generalTokenSource.IsCancellationRequested)
                    {
                        //We are quitting
                        break;
                    }

                    if ((readerTokenSource?.IsCancellationRequested ?? true) || !readCompleted)
                    {
                        //We are just restarting reader, since it was intercepted with an exception or the reader token source was cancelled
                        break;
                    }

                    if (webSocketReceiveResult.Count < 1)
                    {
                        communication.SendWarningMessage($"WebSocketMessage returned no characters despite not being at end.  {lastMessage}");
                        break;
                    }

                    lastMessage += Encoding.UTF8.GetString(incomingData, 0, webSocketReceiveResult.Count);
                }


                if (generalTokenSource.IsCancellationRequested)
                {
                    //We are quitting
                    break;
                }

                if ((readerTokenSource?.IsCancellationRequested ?? true) || !readCompleted)
                {
                    //We are just restarting reader, since it was intercepted with an exception or the reader token source was cancelled
                    continue;
                }

                BasicPubSubMessage message = JsonSerializer.Deserialize<BasicPubSubMessage>(lastMessage)!;

                switch (message.TypeString)
                {
                    case "PONG":
                        pongReceived = true;
                        break;

                    case "PING":
                        await SendMessage(pongMessage);
                        break;

                    case "RECONNECT":
                        communication.SendDebugMessage($"PubSub Reconnect Message Received");
                        breakConnection = true;
                        break;

                    case "RESPONSE":
                        {
                            PubSubResponseMessage response = JsonSerializer.Deserialize<PubSubResponseMessage>(lastMessage)!;
                            PubSubMessage? sentMessage = sentMessages.Where(x => x.Nonce == response.Nonce).FirstOrDefault();

                            if (!string.IsNullOrEmpty(response.ErrorString))
                            {
                                if (sentMessage is not null)
                                {
                                    communication.SendErrorMessage($"Error with message {JsonSerializer.Serialize(sentMessage)}: {response.ErrorString}");
                                }
                                else
                                {
                                    communication.SendErrorMessage($"Error with message <Unable To Locate>: {response.ErrorString}");
                                }
                            }

                            if (sentMessage is not null)
                            {
                                sentMessages.Remove(sentMessage);
                            }
                        }
                        break;

                    case "MESSAGE":
                        {
                            ListenResponse listenResponse = JsonSerializer.Deserialize<ListenResponse>(lastMessage)!;

                            BaseMessageData messageData = JsonSerializer.Deserialize<BaseMessageData>(listenResponse.Data.Message)!;

                            switch (messageData.TypeString)
                            {
                                case "reward-redeemed":
                                    ChannelPointMessageData channelPointMessageData = JsonSerializer.Deserialize<ChannelPointMessageData>(listenResponse.Data.Message)!;
                                    redemptionHandler.HandleRedemption(channelPointMessageData.Data);
                                    break;

                                default:
                                    communication.SendErrorMessage($"Unsupported PubSub Message: {messageData.TypeString} - {listenResponse.Data.Message}");
                                    break;
                            }
                        }
                        break;

                    default:
                        communication.SendErrorMessage($"Unsupported PubSub Message Type: {message.TypeString} - {lastMessage}");
                        break;
                }
            }
        }
        catch (OperationCanceledException) { /* swallow */ }
        catch (ThreadAbortException) { /* swallow */ }
        catch (ObjectDisposedException) { /* swallow */ }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"PubSub Exception: {ex.GetType().Name}");
            if (lastMessage is not null)
            {
                communication.SendErrorMessage($"Last PubSub Message: {lastMessage}");
            }

            errorHandler.LogMessageException(ex, "");
        }
    }


    private async Task CheckConnectionOrWait()
    {
        if (generalTokenSource.IsCancellationRequested)
        {
            return;
        }

        if (connectionTask is not null)
        {
            await connectionTask;
        }
        else if (!IsWebsocketOpen() || breakConnection)
        {
            //Trigger a reconnect attempt if we're not currently
            breakConnection = false;
            connectionTask = HandleReconnect();

            await connectionTask;

            connectionTask = null;
        }
    }

    private bool IsWebsocketOpen()
    {
        if (clientWebSocket is null)
        {
            return false;
        }

        switch (clientWebSocket.State)
        {
            case WebSocketState.Open:
                return true;

            case WebSocketState.Connecting:
            case WebSocketState.None:
            case WebSocketState.CloseSent:
            case WebSocketState.CloseReceived:
            case WebSocketState.Closed:
            case WebSocketState.Aborted:
                return false;

            default:
                communication.SendDebugMessage($"Unexpected clientWebSocket State: {clientWebSocket.State}");
                return false;
        }
    }

    public void NotifyShuttingDown()
    {
        generalTokenSource.Cancel();

        disconnectionTask = Disconnect();
    }


    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                generalTokenSource.Cancel();
                readerTokenSource?.Cancel();

                if (disconnectionTask is null)
                {
                    disconnectionTask = Disconnect();
                }

                List<Task> tasks = new List<Task>() { disconnectionTask, handlePingsTask!, readMessagesTask!, connectionTask! };

                Task.WaitAll(tasks.Where(x => x is not null).ToArray(), 2_000);

                disconnectTokenSource.Cancel();

                handlePingsTask = null;
                readMessagesTask = null;
                connectionTask = null;
                disconnectionTask = null;

                generalTokenSource.Dispose();

                readerTokenSource?.Dispose();
                readerTokenSource = null;

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

    #endregion IDisposable
}
