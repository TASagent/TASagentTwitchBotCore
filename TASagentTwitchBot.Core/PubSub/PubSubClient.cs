using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using BGC.Collections.Generic;

using TASagentTwitchBot.Core.API.Twitch;

namespace TASagentTwitchBot.Core.PubSub
{
    public class PubSubClient : IDisposable
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly ICommunication communication;
        private readonly ErrorHandler errorHandler;
        private readonly IRedemptionSystem redemptionHandler;

        private bool disposedValue = false;
        private bool breakConnection = false;

        private ClientWebSocket clientWebSocket;
        private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
        private readonly CountdownEvent readers = new CountdownEvent(1);

        private CancellationTokenSource readerTokenSource;

        private readonly object reconnectLock = new object();
        private readonly CountdownEvent reconnections = new CountdownEvent(1);

        private readonly Uri websocketServerURI = new Uri("wss://pubsub-edge.twitch.tv");
        private readonly TimeSpan pingWaitTime = new TimeSpan(hours: 0, minutes: 4, seconds: 00);

        private readonly BasicPubSubMessage pingMessage;
        private readonly BasicPubSubMessage pongMessage;
        private readonly byte[] incomingData = new byte[4096];
        private bool pongReceived = false;

        private readonly RingBuffer<PubSubMessage> sentMessages = new RingBuffer<PubSubMessage>(20);

        public PubSubClient(
            Config.BotConfiguration botConfig,
            ICommunication communication,
            IRedemptionSystem redemptionHandler,
            ErrorHandler errorHandler)
        {
            this.botConfig = botConfig;
            this.communication = communication;
            this.errorHandler = errorHandler;
            this.redemptionHandler = redemptionHandler;

            pingMessage = new BasicPubSubMessage("PING");
            pongMessage = new BasicPubSubMessage("PONG");
        }

        public async Task Launch()
        {
            await Connect();

            HandlePings();

            ReadMessages();

            await redemptionHandler.Initialize();
        }

        private async Task Connect()
        {
            readerTokenSource = new CancellationTokenSource();
            clientWebSocket = new ClientWebSocket();

            await clientWebSocket.ConnectAsync(websocketServerURI, generalTokenSource.Token);

            if (clientWebSocket.State != WebSocketState.Open)
            {
                communication.SendErrorMessage("Unable to connect to PubSub. ABORTING.");
                throw new Exception($"Unable to connect to PubSub. ABORTING.");
            }

            ListenMessage listenMessage = new ListenMessage(
                topics: new[] { $"channel-points-channel-v1.{botConfig.BroadcasterId}" },
                authToken: botConfig.BroadcasterAccessToken);

            await SendMessage(listenMessage);

            reconnections.Signal();
        }

        private async Task HandleReconnect()
        {
            try
            {
                communication.SendDebugMessage("Attempting PubSub Reconnect");

                readers.AddCount();
                int timeout = 1000;

                while (true)
                {
                    if (generalTokenSource.IsCancellationRequested)
                    {
                        //We are quitting, breakout
                        break;
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
                            break;
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
            catch (TaskCanceledException) { /* swallow */ }
            catch (OperationCanceledException) { /* swallow */ }
            catch (Exception ex)
            {
                errorHandler.LogSystemException(ex);
            }
            finally
            {
                readers.Signal();
            }
        }

        private async Task Disconnect()
        {
            reconnections.Reset(1);

            //Kill ongoing readers
            readerTokenSource?.Cancel();

            //Make sure reader moves to await reconnect
            await Task.Delay(250);

            await clientWebSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", generalTokenSource.Token);

            //We are disconnected - try to reconnect
            clientWebSocket?.Dispose();
            clientWebSocket = null;

            readerTokenSource?.Dispose();
            readerTokenSource = null;
        }

        public async Task SendMessage<T>(T message)
        {
            if (message is PubSubMessage pubSubMessage)
            {
                sentMessages.Add(pubSubMessage);
            }

            await clientWebSocket.SendAsync(
                buffer: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)),
                messageType: WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: generalTokenSource.Token);
        }

        private async void HandlePings()
        {
            try
            {
                readers.AddCount();

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
            catch (TaskCanceledException) { /* swallow */ }
            catch (ThreadAbortException) { /* swallow */ }
            catch (ObjectDisposedException) { /* swallow */ }
            catch (OperationCanceledException) { /* swallow */ }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"PubSub Server Pinger Exception: {ex.GetType().Name}");
                errorHandler.LogMessageException(ex, "");
            }
            finally
            {
                readers.Signal();
            }
        }

        private async void ReadMessages()
        {
            WebSocketReceiveResult webSocketReceiveResult = null;
            string lastMessage = null;

            try
            {
                readers.AddCount();

                while (true)
                {
                    await CheckConnectionOrWait();
                    bool readCompleted = false;

                    try
                    {
                        webSocketReceiveResult = await clientWebSocket.ReceiveAsync(incomingData, readerTokenSource.Token);
                        readCompleted = true;
                    }
                    catch (TaskCanceledException) { /* swallow */ }
                    catch (ThreadAbortException) { /* swallow */ }
                    catch (ObjectDisposedException) { /* swallow */ }
                    catch (OperationCanceledException) { /* swallow */ }
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

                    if (webSocketReceiveResult.Count < 1)
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
                            webSocketReceiveResult = await clientWebSocket.ReceiveAsync(incomingData, readerTokenSource.Token);
                            readCompleted = true;
                        }
                        catch (TaskCanceledException) { /* swallow */ }
                        catch (ThreadAbortException) { /* swallow */ }
                        catch (ObjectDisposedException) { /* swallow */ }
                        catch (OperationCanceledException) { /* swallow */ }
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

                    BasicPubSubMessage message = JsonSerializer.Deserialize<BasicPubSubMessage>(lastMessage);

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
                                PubSubResponseMessage response = JsonSerializer.Deserialize<PubSubResponseMessage>(lastMessage);
                                PubSubMessage sentMessage = sentMessages.Where(x => x.Nonce == response.Nonce).FirstOrDefault();

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
                                ListenResponse listenResponse = JsonSerializer.Deserialize<ListenResponse>(lastMessage);

                                BaseMessageData messageData = JsonSerializer.Deserialize<BaseMessageData>(listenResponse.Data.Message);

                                switch (messageData.TypeString)
                                {
                                    case "reward-redeemed":
                                        ChannelPointMessageData channelPointMessageData = JsonSerializer.Deserialize<ChannelPointMessageData>(listenResponse.Data.Message);
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
            catch (TaskCanceledException) { /* swallow */ }
            catch (ThreadAbortException) { /* swallow */ }
            catch (ObjectDisposedException) { /* swallow */ }
            catch (OperationCanceledException) { /* swallow */ }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"PubSub Exception: {ex.GetType().Name}");
                if (lastMessage is not null)
                {
                    communication.SendErrorMessage($"Last PubSub Message: {lastMessage}");
                }

                errorHandler.LogMessageException(ex, "");
            }
            finally
            {
                readers.Signal();
            }
        }


        private async Task CheckConnectionOrWait()
        {
            if (generalTokenSource.IsCancellationRequested)
            {
                return;
            }

            if (!IsWebsocketOpen() || breakConnection || !reconnections.IsSet)
            {
                breakConnection = false;

                //Trigger a reconnect attempt if we're not currently
                lock (reconnectLock)
                {
                    if (reconnections.IsSet)
                    {
                        reconnections.Reset(1);
                        Task reconnect = Task.Run(HandleReconnect);
                    }
                }

                await Task.Run(() => reconnections.Wait(), generalTokenSource.Token);
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


        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    generalTokenSource.Cancel();
                    //Kill readers
                    readerTokenSource?.Cancel();

                    //Wait for readers
                    readers.Signal();
                    readers.Wait();
                    readers.Dispose();

                    generalTokenSource.Dispose();
                    readerTokenSource?.Dispose();
                    readerTokenSource = null;

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

    public record BasicPubSubMessage(
        [property: JsonPropertyName("type")] string TypeString);

    public record PubSubMessage(
        string TypeString,
        [property: JsonPropertyName("nonce")] string Nonce) : BasicPubSubMessage(TypeString);

    public record PubSubResponseMessage(
        string TypeString,
        [property: JsonPropertyName("error")] string ErrorString) : PubSubMessage(TypeString, Guid.NewGuid().ToString());

    public record ListenResponse(
        string TypeString,
        [property: JsonPropertyName("data")] ListenResponse.Datum Data) : BasicPubSubMessage(TypeString)
    {
        public record Datum(
            [property: JsonPropertyName("topic")] string Topic,
            [property: JsonPropertyName("message")] string Message);
    }

    public record ListenMessage : PubSubMessage
    {
        public ListenMessage() : base("LISTEN", Guid.NewGuid().ToString()) { }

        public ListenMessage(Datum data)
            : base("LISTEN", Guid.NewGuid().ToString())
        {
            Data = data;
        }

        public ListenMessage(IEnumerable<string> topics, string authToken)
            : base("LISTEN", Guid.NewGuid().ToString())
        {
            Data = new Datum(topics.ToList(), authToken);
        }

        [JsonPropertyName("data")]
        public Datum Data { get; init; }

        public record Datum(
            [property: JsonPropertyName("topics")] List<string> Topics,
            [property: JsonPropertyName("auth_token")] string AuthToken);
    }

    public record BaseMessageData(
        [property: JsonPropertyName("type")] string TypeString);

    public record ChannelPointMessageData(
        [property: JsonPropertyName("data")] ChannelPointMessageData.Datum Data) : BaseMessageData("reward-redeemed")
    {
        public record Datum(
            [property: JsonPropertyName("timestamp")] DateTime Timestamp,
            [property: JsonPropertyName("redemption")] Datum.RedemptionData Redemption)
        {
            public record RedemptionData(
                [property: JsonPropertyName("id")] string Id,
                [property: JsonPropertyName("user")] RedemptionData.UserData User,
                [property: JsonPropertyName("channel_id")] string ChannelId,
                [property: JsonPropertyName("redeemed_at")] DateTime RedeemedAt,
                [property: JsonPropertyName("reward")] RedemptionData.RewardData Reward,
                [property: JsonPropertyName("user_input")] string UserInput,
                [property: JsonPropertyName("status")] string Status)
            {
                public record UserData(
                    [property: JsonPropertyName("id")] string Id,
                    [property: JsonPropertyName("login")] string Login,
                    [property: JsonPropertyName("display_name")] string DisplayName);

                public record RewardData(
                    [property: JsonPropertyName("id")] string Id,
                    [property: JsonPropertyName("channel_id")] string ChannelId,
                    [property: JsonPropertyName("title")] string Title,
                    [property: JsonPropertyName("prompt")] string Prompt,
                    [property: JsonPropertyName("cost")] int Cost,
                    [property: JsonPropertyName("is_user_input_required")] bool IsUserInputRequired,
                    [property: JsonPropertyName("is_sub_only")] bool IsSubOnly,
                    [property: JsonPropertyName("image")] TwitchCustomReward.Datum.ImageData Image,
                    [property: JsonPropertyName("default_image")] TwitchCustomReward.Datum.ImageData DefaultImage,
                    [property: JsonPropertyName("background_color")] string BackgroundColor,
                    [property: JsonPropertyName("is_enabled")] bool IsEnabled,
                    [property: JsonPropertyName("is_paused")] bool IsPaused,
                    [property: JsonPropertyName("is_in_stock")] bool IsInStock,
                    [property: JsonPropertyName("max_per_stream")] TwitchCustomReward.Datum.StreamMax MaxPerStream,
                    [property: JsonPropertyName("should_redemptions_skip_request_queue")] bool ShouldRedemptionsSkipRequestQueue);
            }
        }
    }
}
