using System.Threading.Channels;

namespace TASagentTwitchBot.Core.Chat;

public class ChatLogger : IStartupListener, IDisposable
{
    private readonly Config.BotConfiguration botConfig;
    private readonly Lazy<Logs.LocalLogger> chatLog = new Lazy<Logs.LocalLogger>(() => new Logs.LocalLogger("ChatLogs", "chat"));
    private readonly ChannelWriter<string> logWriterChannel;
    private readonly ChannelReader<string> logReaderChannel;

    private readonly bool logChat;
    private readonly Task logHandlerTask;

    private bool disposedValue;

    public ChatLogger(
        Config.BotConfiguration botConfig,
        ICommunication communication)
    {
        this.botConfig = botConfig;

        logChat = botConfig.LogChat;

        Channel<string> lineChannel = Channel.CreateUnbounded<string>();
        logWriterChannel = lineChannel.Writer;
        logReaderChannel = lineChannel.Reader;

        if (logChat)
        {
            communication.SendMessageHandlers += WriteOutgoingMessage;
            communication.SendWhisperHandlers += WriteOutgoingWhisper;
            communication.ReceiveMessageLoggers += WriteIncomingMessage;

            logHandlerTask = Task.Run(HandleLines);
        }
        else
        {
            logWriterChannel.TryComplete();
            logHandlerTask = Task.CompletedTask;
        }
    }

    private void WriteOutgoingMessage(string message) =>
        logWriterChannel.TryWrite($"[{DateTime.Now:G}] {botConfig.BotName}: {message}");

    private void WriteOutgoingWhisper(string username, string message) =>
        logWriterChannel.TryWrite($"[{DateTime.Now:G}] {botConfig.BotName} /w {username} : {message}");

    private void WriteIncomingMessage(IRC.TwitchChatter chatter) =>
        logWriterChannel.TryWrite(chatter.ToLogString());

    private async Task HandleLines()
    {
        await foreach (string line in logReaderChannel.ReadAllAsync())
        {
            chatLog.Value.PushLine(line);
        }
    }

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                logWriterChannel.TryComplete();

                logHandlerTask.Wait(2_000);

                if (chatLog.IsValueCreated)
                {
                    chatLog.Value.Dispose();
                }
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
