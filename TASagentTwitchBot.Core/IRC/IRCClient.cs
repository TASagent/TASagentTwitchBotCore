using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Channels;

namespace TASagentTwitchBot.Core.IRC;

// Reference: https://www.youtube.com/watch?v=Ss-OzV9aUZg
public class IrcClient : IShutdownListener, IDisposable
{
    private readonly Config.BotConfiguration botConfig;
    private readonly ErrorHandler errorHandler;
    private readonly API.Twitch.IBotTokenValidator botTokenValidator;

    private readonly IIRCLogger ircLogger;
    private readonly Chat.IChatMessageHandler chatMessageHandler;
    private readonly INoticeHandler noticeHandler;
    private readonly ICommunication communication;

    private readonly string username;
    private readonly string channel;

    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
    private CancellationTokenSource? readerTokenSource;

    private Task? connectionTask = null;
    private Task? disconnectionTask = null;
    private Task? handleIncomingMessagesTask = null;
    private Task? handleOutgoingMessagesTask = null;
    private Task? handlePingsTask = null;

    private TcpClient? tcpClient = null;
    private SslStream? sslStream = null;
    private StreamReader? inputStream = null;
    private StreamWriter? outputStream = null;

    private readonly bool exhaustiveLogging;

    private const string TWITCH_IRC_SERVER = "irc.chat.twitch.tv";
    private const int TWITCH_IRC_PORT = 6697;

    private bool disposedValue = false;
    private bool breakConnection = false;

    private DateTime lastMessageTime;
    private readonly TimeSpan pingDelayTime = new TimeSpan(hours: 0, minutes: 2, seconds: 0);
    private bool pongReceived = false;

    private static string LogDateString => $"[{DateTime.Now:G}]".PadRight(24);

    private readonly ChannelReader<string> outgoingChatReader;
    private readonly ChannelWriter<string> outgoingChatWriter;

    public IrcClient(
        Config.BotConfiguration botConfig,
        ApplicationManagement applicationManagement,
        API.Twitch.IBotTokenValidator botTokenValidator,
        IIRCLogger ircLogger,
        Chat.IChatMessageHandler chatMessageHandler,
        INoticeHandler noticeHandler,
        ICommunication communication,
        ErrorHandler errorHandler)
    {
        this.botConfig = botConfig;
        this.errorHandler = errorHandler;
        this.botTokenValidator = botTokenValidator;

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

        applicationManagement.RegisterShutdownListener(this);

        if (botConfig.UseThreadedMonitors)
        {
            Task.Run(Start);
        }
        else
        {
            Start();
        }
    }

    private async void Start()
    {
        try
        {
            const int CONNECTION_ATTEMPT_MAX = 5;
            bool tokenValidated = await botTokenValidator.WaitForValidationAsync(generalTokenSource.Token);

            if (!tokenValidated)
            {
                communication.SendErrorMessage($"Bot token validation failed. Unable to connect with IRC.");
                return;
            }

            for (int connectionAttempt = 0; connectionAttempt < CONNECTION_ATTEMPT_MAX; connectionAttempt++)
            {
                if (await Connect())
                {
                    break;
                }

                if (connectionAttempt == CONNECTION_ATTEMPT_MAX - 1)
                {
                    throw new Exception($"Unable to connect to IRC after {CONNECTION_ATTEMPT_MAX} attempts.");
                }

                communication.SendWarningMessage($"Reattempting IRC Connection");

                try
                {
                    //Ensure we are disconnected
                    await Disconnect();
                }
                catch (Exception) { /* swallow */ }
            }

            //Kick off message reader
            handleIncomingMessagesTask = HandleIncomingMessages();

            //Kick off message sender
            handleOutgoingMessagesTask = HandleOutgoingMessages();

            //Kick off Ping listener
            handlePingsTask = HandlePings();
        }
        catch (Exception ex)
        {
            errorHandler.LogFatalException(ex);
        }
    }

    private async Task<bool> Connect()
    {
        WriteToIRCLogRaw("");
        WriteToIRCLog("Connecting\n");

        readerTokenSource = new CancellationTokenSource();

        tcpClient = new TcpClient(TWITCH_IRC_SERVER, TWITCH_IRC_PORT);
        sslStream = new SslStream(tcpClient.GetStream());

        await sslStream.AuthenticateAsClientAsync(TWITCH_IRC_SERVER);

        inputStream = new StreamReader(sslStream);
        outputStream = new StreamWriter(sslStream)
        {
            AutoFlush = true
        };

        // Reference: https://dev.twitch.tv/docs/irc/tags/
        await outputStream.WriteLineAsync("CAP REQ :twitch.tv/tags");
        // Reference: https://dev.twitch.tv/docs/irc/commands/
        await outputStream.WriteLineAsync("CAP REQ :twitch.tv/commands");
        // Reference: https://dev.twitch.tv/docs/irc/membership/
        await outputStream.WriteLineAsync("CAP REQ :twitch.tv/membership");

        await outputStream.WriteLineAsync($"PASS oauth:{botConfig.BotAccessToken}");
        await outputStream.WriteLineAsync($"NICK {username}");
        await outputStream.WriteLineAsync($"USER {username} 8 * :{username}");
        await outputStream.WriteLineAsync($"JOIN #{channel}");

        //Log these messages
        LogIRCMessage($"CAP REQ :twitch.tv/tags", false);
        LogIRCMessage($"CAP REQ :twitch.tv/commands", false);
        LogIRCMessage($"CAP REQ :twitch.tv/membership", false);

        LogIRCMessage($"PASS oauth:---", false);
        LogIRCMessage($"NICK {username}", false);
        LogIRCMessage($"USER {username} 8 * :{username}", false);
        LogIRCMessage($"JOIN #{channel}", false);

        await outputStream.FlushAsync();

        if (await GetNextLine() != ":tmi.twitch.tv CAP * ACK :twitch.tv/tags" ||
            await GetNextLine() != ":tmi.twitch.tv CAP * ACK :twitch.tv/commands" ||
            await GetNextLine() != ":tmi.twitch.tv CAP * ACK :twitch.tv/membership")
        {
            //Failed to get expected responses
            return false;
        }

        return true;
    }

    private async Task<string?> GetNextLine()
    {
        const int RESPONSE_WAIT_TIME = 5;

        try
        {
            Task<string?> nextLineTask = inputStream!.ReadLineAsync().WithCancellation(generalTokenSource.Token);
            await nextLineTask.WaitAsync(new TimeSpan(0, 0, RESPONSE_WAIT_TIME));

            if (!nextLineTask.IsCompleted)
            {
                communication.SendErrorMessage($"No IRC response received within {RESPONSE_WAIT_TIME} seconds.");
                return null;
            }

            if (string.IsNullOrEmpty(nextLineTask.Result))
            {
                communication.SendErrorMessage($"Null or empty IRC response received.");
                return null;
            }

            communication.SendDebugMessage($"  {nextLineTask.Result}");
            LogIRCMessage(nextLineTask.Result, true);

            return nextLineTask.Result;
        }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"Exception waiting for next line: {ex.Message}");
            return null;
        }
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
        //Kill ongoing readers
        readerTokenSource?.Cancel();

        //Make sure readers move
        await Task.Delay(250);

        //Clear streams
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

    private async Task HandlePings()
    {
        try
        {
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
    }


    private async Task HandleReconnect()
    {
        try
        {
            communication.SendDebugMessage("Attempting IRC Reconnect");

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

                    if (await Connect())
                    {
                        //Successfully Reconnected
                        break;
                    }
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
    }

    private void SendMessageWithPrefix(string message, string prefix)
    {
        if (prefix is null)
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

    private async Task HandleOutgoingMessages()
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
                await outputStream!.WriteLineAsync(newMessage);
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

        if (connectionTask is not null)
        {
            await connectionTask;
        }
        else if (!(tcpClient?.Connected ?? false) || breakConnection)
        {
            //Trigger a reconnect attempt if we're not currently
            breakConnection = false;
            connectionTask = HandleReconnect();

            await connectionTask;

            connectionTask = null;
        }
    }

    private async Task HandleIncomingMessages()
    {
        string? rawMessage = null;

        try
        {
            while (true)
            {
                await CheckConnectionOrWait();

                bool readCompleted = false;

                try
                {
                    rawMessage = await inputStream!.ReadLineAsync().WithCancellation(readerTokenSource!.Token);
                    readCompleted = true;

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

                LogIRCMessage(rawMessage, true);
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
    }

    public void NotifyShuttingDown()
    {
        generalTokenSource.Cancel();

        disconnectionTask = Disconnect();

        outgoingChatWriter.TryComplete();
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

                if (disconnectionTask is null)
                {
                    disconnectionTask = Disconnect();
                }

                outgoingChatWriter.TryComplete();


                List<Task> tasks = new List<Task>() { disconnectionTask, handleIncomingMessagesTask!, handleOutgoingMessagesTask!, handlePingsTask!, connectionTask! };

                Task.WaitAll(tasks.Where(x => x is not null).ToArray(), 1000);

                handlePingsTask?.Dispose();
                handlePingsTask = null;

                handleIncomingMessagesTask?.Dispose();
                handleIncomingMessagesTask = null;

                handleOutgoingMessagesTask?.Dispose();
                handleOutgoingMessagesTask = null;

                connectionTask?.Dispose();
                connectionTask = null;

                disconnectionTask?.Dispose();
                disconnectionTask = null;

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
