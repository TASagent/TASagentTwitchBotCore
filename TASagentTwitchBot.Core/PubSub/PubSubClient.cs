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

        private readonly ClientWebSocket clientWebSocket;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        private readonly Uri websocketServerURI = new Uri("wss://pubsub-edge.twitch.tv");
        private readonly TimeSpan pingWaitTime = new TimeSpan(hours: 0, minutes: 4, seconds: 00);

        private readonly BasicPubSubMessage pingMessage;
        private readonly BasicPubSubMessage pongMessage;
        private readonly byte[] incomingData = new byte[16384];
        private bool pongReceived = false;

        private readonly RingBuffer<PubSubMessage> sentMessages = new RingBuffer<PubSubMessage>(20);

        public PubSubClient(
            Config.IBotConfigContainer botConfigContainer,
            ICommunication communication,
            IRedemptionSystem redemptionHandler,
            ErrorHandler errorHandler)
        {
            botConfig = botConfigContainer.BotConfig;
            this.communication = communication;
            this.errorHandler = errorHandler;
            this.redemptionHandler = redemptionHandler;

            clientWebSocket = new ClientWebSocket();
            pingMessage = new BasicPubSubMessage("PING");
            pongMessage = new BasicPubSubMessage("PONG");
        }

        private async Task Connect()
        {
            await clientWebSocket.ConnectAsync(websocketServerURI, cts.Token);

            if (clientWebSocket.State != WebSocketState.Open)
            {
                communication.SendErrorMessage("Unable to connect to PubSub. ABORTING.");
                throw new Exception($"Unable to connect to PubSub. ABORTING.");
            }

            ListenMessage listenMessage = new ListenMessage(
                topics: new[] { $"channel-points-channel-v1.{botConfig.BroadcasterId}" },
                authToken: botConfig.BroadcasterAccessToken);

            await SendMessage(listenMessage);
        }

        private async Task TryReconnect()
        {
            try
            {
                communication.SendDebugMessage("Attempting PubSub Reconnect");

                int timeout = 1000;

                while (true)
                {
                    if (cts.IsCancellationRequested)
                    {
                        //We are quitting, breakout
                        break;
                    }

                    try
                    {
                        await Task.Delay(timeout, cts.Token);

                        if (cts.IsCancellationRequested)
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
                        communication.SendErrorMessage($"Failed to reconnect to PubSub: {ex.Message}");
                    }

                    //Cap timeout at 2 minutes
                    timeout = Math.Max(2 * timeout, 120_000);
                }
            }
            catch (TaskCanceledException)
            {
                //Swallow
            }
            catch (OperationCanceledException)
            {
                //Swallow
            }
            catch (Exception ex)
            {
                errorHandler.LogSystemException(ex);
            }
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
                cancellationToken: cts.Token);
        }


        public async Task Launch()
        {
            await Connect();

            PingServer();

            ReadMessages();

            await redemptionHandler.Initialize();
        }

        private async void PingServer()
        {
            try
            {
                while (true)
                {
                    pongReceived = false;
                    await SendMessage(pingMessage);

                    //Wait 10 seconds
                    await Task.Delay(10 * 1000, cts.Token);

                    if (!pongReceived)
                    {
                        communication.SendDebugMessage($"No PubSub Pong received in 10 seconds - Reconnecting");

                        //Reconnect
                        await Connect();
                    }

                    await Task.Delay(pingWaitTime, cts.Token);
                }
            }
            catch (TaskCanceledException)
            {
                //Swallow
            }
            catch (ThreadAbortException)
            {
                //Swallow
            }
            catch (ObjectDisposedException)
            {
                //Swallow
            }
            catch (OperationCanceledException)
            {
                //Swallow
            }
        }

        private async void ReadMessages()
        {
            try
            {
                while (true)
                {
                    WebSocketReceiveResult webSocketReceiveResult = await clientWebSocket.ReceiveAsync(incomingData, cts.Token);

                    if (webSocketReceiveResult.Count < 1)
                    {
                        await Task.Delay(100, cts.Token);
                        continue;
                    }

                    string result = Encoding.UTF8.GetString(incomingData, 0, webSocketReceiveResult.Count);
                    BasicPubSubMessage message = JsonSerializer.Deserialize<BasicPubSubMessage>(result);

                    switch (message.TypeString)
                    {
                        case "PONG":
                            pongReceived = true;
                            break;

                        case "PING":
                            await SendMessage(pongMessage);
                            break;

                        case "RECONNECT":
                            await TryReconnect();
                            break;

                        case "RESPONSE":
                            {
                                PubSubResponseMessage response = JsonSerializer.Deserialize<PubSubResponseMessage>(result);
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
                                ListenResponse listenResponse = JsonSerializer.Deserialize<ListenResponse>(result);

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
                            communication.SendErrorMessage($"Unsupported PubSub Message Type: {message.TypeString} - {result}");
                            break;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                //Swallow
            }
            catch (ThreadAbortException)
            {
                //Swallow
            }
            catch (ObjectDisposedException)
            {
                //Swallow
            }
            catch (OperationCanceledException)
            {
                //Swallow
            }
        }


        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cts.Cancel();

                    clientWebSocket.Dispose();
                    cts.Dispose();
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
