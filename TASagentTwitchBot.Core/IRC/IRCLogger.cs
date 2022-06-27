using System.Threading.Channels;

using TASagentTwitchBot.Core.Logs;

namespace TASagentTwitchBot.Core.IRC;

[AutoRegister]
public interface IIRCLogger
{
    void WriteLine(string line);
}

public class IRCLogger : IIRCLogger, IDisposable
{
    private readonly Lazy<LocalLogger> ircLog = new Lazy<LocalLogger>(() => new LocalLogger("IRCLogs", "irc"));
    private readonly ChannelWriter<string> logWriterChannel;
    private readonly ChannelReader<string> logReaderChannel;

    private readonly Task logHandlerTask;

    private bool disposedValue;

    public IRCLogger(
        Config.BotConfiguration botConfig)
    {
        Channel<string> lineChannel = Channel.CreateUnbounded<string>();
        logWriterChannel = lineChannel.Writer;
        logReaderChannel = lineChannel.Reader;

        if (botConfig.ExhaustiveIRCLogging)
        {
            if (botConfig.UseThreadedMonitors)
            {
                logHandlerTask = Task.Run(HandleLines);
            }
            else
            {
                logHandlerTask = HandleLines();
            }
        }
        else
        {
            logWriterChannel.TryComplete();
            logHandlerTask = Task.CompletedTask;
        }
    }

    public void WriteLine(string line) => logWriterChannel.TryWrite(line);

    private async Task HandleLines()
    {
        await foreach (string line in logReaderChannel.ReadAllAsync())
        {
            ircLog.Value.PushLine(line);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                logWriterChannel.TryComplete();

                logHandlerTask.Wait(2_000);

                if (ircLog.IsValueCreated)
                {
                    ircLog.Value.Dispose();
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
}
