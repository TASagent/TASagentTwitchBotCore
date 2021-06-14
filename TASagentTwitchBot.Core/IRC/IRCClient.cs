using System;
using System.Net.Security;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace TASagentTwitchBot.Core.IRC
{
    // Reference: https://www.youtube.com/watch?v=Ss-OzV9aUZg
    public class IrcClient : IDisposable
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly ErrorHandler errorHandler;

        private readonly IIRCLogger ircLogger;
        private readonly Chat.IChatMessageHandler chatMessageHandler;
        private readonly INoticeHandler noticeHandler;
        private readonly ICommunication communication;

        private readonly string username;
        private readonly string channel;

        private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
        private readonly CountdownEvent readers = new CountdownEvent(1);

        private readonly bool exhaustiveLogging;

        private readonly object reconnectLock = new object();
        private readonly CountdownEvent reconnections = new CountdownEvent(1);

        private const string TWITCH_IRC_SERVER = "irc.chat.twitch.tv";
        private const int TWITCH_IRC_PORT = 6697;

        private CancellationTokenSource readerTokenSource;

        private TcpClient tcpClient = null;
        private SslStream sslStream = null;
        private StreamReader inputStream = null;
        private StreamWriter outputStream = null;

        private bool disposedValue = false;
        private bool breakConnection = false;

        private DateTime lastMessageTime;
        private readonly TimeSpan pingDelayTime = new TimeSpan(hours: 0, minutes: 2, seconds: 0);
        private bool pongReceived = false;

        private static string LogDateString => $"[{DateTime.Now:G}]".PadRight(24);

        private readonly ChannelReader<string> outgoingChatReader = null;
        private readonly ChannelWriter<string> outgoingChatWriter = null;

        public IrcClient(
            Config.BotConfiguration botConfig,
            IIRCLogger ircLogger,
            Chat.IChatMessageHandler chatMessageHandler,
            INoticeHandler noticeHandler,
            ICommunication communication,
            ErrorHandler errorHandler)
        {
            this.botConfig = botConfig;
            this.errorHandler = errorHandler;

            this.ircLogger = ircLogger;
            this.chatMessageHandler = chatMessageHandler;
            this.noticeHandler = noticeHandler;
            this.communication = communication;

            username = botConfig.BotName.ToLower();
            channel = botConfig.Broadcaster.ToLower();

            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException("Bot Name cannot be empty.");
            }

            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentException("Bot Channel cannot be empty.");
            }

            Channel<string> chatChannel = Channel.CreateUnbounded<string>();
            outgoingChatWriter = chatChannel.Writer;
            outgoingChatReader = chatChannel.Reader;

            exhaustiveLogging = botConfig.ExhaustiveIRCLogging;

            communication.SendMessageHandlers += SendMessage;
            communication.SendWhisperHandlers += SendWhisper;
        }

        public async Task Start()
        {
            await Connect();

            //Kick off message reader
            HandleIncomingMessages();

            //Kick off message sender
            HandleOutgoingMessages();

            //Kick off Ping listener
            HandlePings();
        }

        private async Task Connect()
        {
            WriteToIRCLogRaw("");
            WriteToIRCLog("Connecting\n");

            readerTokenSource = new CancellationTokenSource();

            tcpClient = new TcpClient(TWITCH_IRC_SERVER, TWITCH_IRC_PORT);
            sslStream = new SslStream(tcpClient.GetStream());

            sslStream.AuthenticateAsClient(TWITCH_IRC_SERVER);

            inputStream = new StreamReader(sslStream);
            outputStream = new StreamWriter(sslStream)
            {
                AutoFlush = true
            };

            // Reference: https://dev.twitch.tv/docs/irc/tags/
            outputStream.WriteLine("CAP REQ :twitch.tv/tags");
            // Reference: https://dev.twitch.tv/docs/irc/commands/
            outputStream.WriteLine("CAP REQ :twitch.tv/commands");
            // Reference: https://dev.twitch.tv/docs/irc/membership/
            outputStream.WriteLine("CAP REQ :twitch.tv/membership");

            outputStream.WriteLine($"PASS oauth:{botConfig.BotAccessToken}");
            outputStream.WriteLine($"NICK {username}");
            outputStream.WriteLine($"USER {username} 8 * :{username}");
            outputStream.WriteLine($"JOIN #{channel}");

            //Log these messages
            LogIRCMessage($"CAP REQ :twitch.tv/tags", false);
            LogIRCMessage($"CAP REQ :twitch.tv/commands", false);
            LogIRCMessage($"CAP REQ :twitch.tv/membership", false);

            LogIRCMessage($"PASS oauth:---", false);
            LogIRCMessage($"NICK {username}", false);
            LogIRCMessage($"USER {username} 8 * :{username}", false);
            LogIRCMessage($"JOIN #{channel}", false);

            await outputStream.FlushAsync();

            reconnections.Signal();
        }

        private void WriteToIRCLogRaw(string message)
        {
            if (exhaustiveLogging)
            {
                lock (ircLogger)
                {
                    ircLogger.WriteLine(message);
                }
            }
        }

        private void WriteToIRCLog(string message)
        {
            if (exhaustiveLogging)
            {
                lock (ircLogger)
                {
                    ircLogger.WriteLine($"{LogDateString} {message}");
                }
            }
        }

        private void LogIRCMessage(string message, bool incoming)
        {
            if (exhaustiveLogging)
            {
                lock (ircLogger)
                {
                    ircLogger.WriteLine($"{LogDateString} {(incoming ? '<' : '>')} {message}");
                }
            }
        }

        private async Task Disconnect()
        {
            reconnections.Reset(1);

            //Kill ongoing readers
            readerTokenSource?.Cancel();

            //Make sure reader moves to await reconnect
            await Task.Delay(250);

            //We are disconnected - try to reconnect
            outputStream?.Dispose();
            outputStream = null;

            inputStream?.Dispose();
            inputStream = null;

            sslStream?.Dispose();
            sslStream = null;

            tcpClient?.Dispose();
            tcpClient = null;

            readerTokenSource?.Dispose();
            readerTokenSource = null;
        }

        public async Task ClearMessage(TwitchChatter chatter) =>
            await outgoingChatWriter.WriteAsync($"PRIVMSG #{channel} :/delete {chatter.MessageId}");

        public async Task SendChatTimeout(string offender, int timeout = 1) =>
            await outgoingChatWriter.WriteAsync($"PRIVMSG #{channel} :/timeout {offender} {timeout}");

        private async void HandlePings()
        {
            try
            {
                readers.AddCount();

                while (true)
                {
                    //Delay loop
                    while (true)
                    {
                        //Wait a minute at a time
                        await Task.Delay(pingDelayTime, generalTokenSource.Token);

                        //Bail if we're trying to quit
                        if (generalTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        if (DateTime.Now - lastMessageTime > pingDelayTime)
                        {
                            //Run a ping test if it's been too long since our last message
                            break;
                        }
                    }

                    //Check if we're connected
                    await CheckConnectionOrWait();

                    //Reset pongReceived flag
                    pongReceived = false;

                    //Send the ping
                    await outgoingChatWriter.WriteAsync($"PING :tmi.twitch.tv", generalTokenSource.Token);

                    //Wait up to two seconds for the pong
                    await Task.Delay(2000, generalTokenSource.Token);

                    //Bail if we're trying to quit
                    if (generalTokenSource.IsCancellationRequested)
                    {
                        break;
                    }

                    if (!pongReceived)
                    {
                        //Retry once
                        await outgoingChatWriter.WriteAsync($"PING :tmi.twitch.tv", generalTokenSource.Token);

                        //Wait up to two seconds for the pong
                        await Task.Delay(2000, generalTokenSource.Token);

                        //Bail if we're trying to quit
                        if (generalTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        if (!pongReceived)
                        {
                            WriteToIRCLog("Ping Listener received no response.  Initiating Reconnect.");
                            communication.SendDebugMessage("Ping Listener determined server stopped responding");
                            breakConnection = true;

                            await CheckConnectionOrWait();
                        }
                    }
                }
            }
            catch (TaskCanceledException) { /* swallow */ }
            catch (OperationCanceledException) { /* swallow */ }
            catch (Exception ex)
            {
                //Log Error
                errorHandler.LogSystemException(ex);
            }
            finally
            {
                readers.Signal();
            }
        }


        private async Task HandleReconnect()
        {
            try
            {
                communication.SendDebugMessage("Attempting IRC Reconnect");

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

                    //Cap timeout at 8 seconds
                    timeout = Math.Max(2 * timeout, 8000);
                }

                communication.SendDebugMessage("IRC Reconnect Success");
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

        private void SendMessageWithPrefix(string message, string prefix)
        {
            if (prefix == null)
            {
                prefix = "";
            }
            else if (prefix.Length > 0 && !prefix.EndsWith(' '))
            {
                prefix += ' ';
            }

            if (message.Length + prefix.Length <= 500)
            {
                outgoingChatWriter.TryWrite($"PRIVMSG #{channel} :{prefix}{message}");
            }
            else
            {
                while (message.Length + prefix.Length > 500)
                {
                    //Split message
                    int endMessage = 500 - prefix.Length;

                    //Find last space in first message to split
                    while (message[endMessage] != ' ' && endMessage > 0)
                    {
                        --endMessage;
                    }

                    //Fallback for space-less messages
                    if (endMessage == 0)
                    {
                        endMessage = 500 - prefix.Length;
                    }

                    outgoingChatWriter.TryWrite($"PRIVMSG #{channel} :{prefix}{message[0..endMessage]}");

                    //Keep rest of message
                    message = message[endMessage..].Trim();
                }

                if (message.Length > 0)
                {
                    //Send remainder
                    outgoingChatWriter.TryWrite($"PRIVMSG #{channel} :{prefix}{message}");
                }
            }
        }

        private void SendMessage(string message) => SendMessageWithPrefix(message, "");
        private void SendWhisper(string user, string message) => SendMessageWithPrefix(message, $"/w {username} ");

        private async void HandleOutgoingMessages()
        {
            await foreach (string newMessage in outgoingChatReader.ReadAllAsync())
            {
                if (string.IsNullOrEmpty(newMessage))
                {
                    communication.SendErrorMessage("Tried to write a null or empty message?");
                    await Task.Yield();
                    continue;
                }

                LogIRCMessage(newMessage, false);

                //Delay if we're not connected
                await CheckConnectionOrWait();

                //Bail if we're trying to quit
                if (generalTokenSource.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await outputStream.WriteLineAsync(newMessage);
                }
                catch (Exception ex)
                {
                    communication.SendErrorMessage($"IRC Send Exception {ex.GetType().Name}");
                    errorHandler.LogMessageException(ex, newMessage);
                }

                await Task.Delay(200);
            }
        }

        private async Task CheckConnectionOrWait()
        {
            if (generalTokenSource.IsCancellationRequested)
            {
                return;
            }

            if (!(tcpClient?.Connected ?? false) || breakConnection || !reconnections.IsSet)
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

        private async void HandleIncomingMessages()
        {
            string rawMessage = null;

            try
            {
                readers.AddCount();

                while (true)
                {
                    await CheckConnectionOrWait();

                    bool readCompleted = false;

                    try
                    {
                        rawMessage = await inputStream.ReadLineAsync().WithCancellation(readerTokenSource.Token);
                        readCompleted = true;

                        LogIRCMessage(rawMessage, true);
                        lastMessageTime = DateTime.Now;
                    }
                    catch (TaskCanceledException) { /* swallow */ }
                    catch (OperationCanceledException) { /* swallow */ }
                    catch (Exception ex)
                    {
                        communication.SendErrorMessage($"IRC Exception Type {ex.GetType().Name}");
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


                    if (string.IsNullOrEmpty(rawMessage))
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    IRCMessage newIRCMessage = new IRCMessage(rawMessage);

                    if (newIRCMessage.ircCommand == IRCCommand.Unknown)
                    {
                        communication.SendErrorMessage($"Unknown IRC Command: {rawMessage}");
                    }

                    switch (newIRCMessage.ircCommand)
                    {
                        case IRCCommand.PrivMsg:
                            //Pass on PRIVMSG
                            chatMessageHandler.HandleChatMessage(newIRCMessage);
                            break;

                        case IRCCommand.Notice:
                        case IRCCommand.UserNotice:
                            //Print out notices
                            if (newIRCMessage.message.Contains("Login authentication failed"))
                            {
                                communication.SendErrorMessage("------------> URGENT <------------");
                                communication.SendErrorMessage("Please check your credentials and try again.");
                                communication.SendErrorMessage("If this error persists, please check if you can access your channel's chat.");
                                communication.SendErrorMessage("If not, then contact Twitch support.");
                                communication.SendErrorMessage("Exiting bot application now...");
                                await Task.Delay(7500);
                                Environment.Exit(0);
                            }
                            else
                            {
                                noticeHandler.HandleIRCNotice(newIRCMessage);
                            }
                            break;

                        case IRCCommand.Ping:
                            //Handle Pongs
                            await outgoingChatWriter.WriteAsync("PONG :tmi.twitch.tv");
                            break;

                        case IRCCommand.Pong:
                            pongReceived = true;
                            break;

                        case IRCCommand.Reconnect:
                            //Reconnect
                            communication.SendDebugMessage($"IRC Reconnect Message");
                            WriteToIRCLog("Received Reconnect Message.  Initiating Reconnect.");
                            breakConnection = true;
                            break;

                        case IRCCommand.Join:
                            //await textOutput.UserStateChange(newIRCMessage.user, true);
                            break;

                        case IRCCommand.Part:
                            //await textOutput.UserStateChange(newIRCMessage.user, false);
                            break;

                        case IRCCommand.Whisper:
                            chatMessageHandler.HandleChatMessage(newIRCMessage);
                            break;

                        case IRCCommand.HostTarget:
                        case IRCCommand.ClearChat:
                        case IRCCommand.GlobalUserState:
                        case IRCCommand.Nick:
                        case IRCCommand.Pass:
                        case IRCCommand.Cap:
                        case IRCCommand.RoomState:
                        case IRCCommand.ServerChange:
                        case IRCCommand.Mode:
                            communication.SendDebugMessage($"  {newIRCMessage}");
                            break;

                        case IRCCommand.UserState:
                            //Suppress Output
                            //Console.WriteLine($"  {newIRCMessage}");
                            break;

                        case IRCCommand._001:
                        case IRCCommand._002:
                        case IRCCommand._003:
                        case IRCCommand._004:
                        case IRCCommand._353:
                        case IRCCommand._366:
                        case IRCCommand._372:
                        case IRCCommand._375:
                        case IRCCommand._376:
                            //Suppress
                            break;

                        case IRCCommand.Unknown:
                            communication.SendDebugMessage($"**{newIRCMessage}");
                            break;

                        default:
                            goto case IRCCommand.Unknown;
                    }
                }
            }
            catch (TaskCanceledException) { /* swallow */ }
            catch (OperationCanceledException) { /* swallow */ }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"IRC Exception Type {ex.GetType().Name}");
                errorHandler.LogMessageException(ex, (rawMessage ?? ""));
            }
            finally
            {
                readers.Signal();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    communication.SendMessageHandlers -= SendMessage;
                    communication.SendWhisperHandlers -= SendWhisper;

                    generalTokenSource.Cancel();
                    readerTokenSource?.Cancel();

                    outgoingChatWriter.TryComplete();

                    readers.Signal();
                    readers.Wait();
                    readers.Dispose();

                    generalTokenSource.Dispose();
                    readerTokenSource?.Dispose();
                    readerTokenSource = null;

                    outputStream?.Dispose();
                    outputStream = null;

                    inputStream?.Dispose();
                    inputStream = null;

                    sslStream?.Dispose();
                    sslStream = null;

                    tcpClient?.Dispose();
                    tcpClient = null;
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
}
