using System.Threading.Channels;

using TASagentTwitchBot.Core.Logs;

namespace TASagentTwitchBot.Core.IRC;

public interface IIRCLogger
{
    void WriteLine(string line);
}

public class IRCLogger : IIRCLogger, IDisposable
{
    private readonly Lazy<LocalLogger> ircLog;
    private readonly Channel<string> lineChannel;
    private bool disposedValue;

    public IRCLogger()
    {
        lineChannel = Channel.CreateUnbounded<string>();
        ircLog = new Lazy<LocalLogger>(() => new LocalLogger("IRCLogs", "irc"));

        HandleLines();
    }

    public void WriteLine(string line) => lineChannel.Writer.TryWrite(line);

    public async void HandleLines()
    {
        await foreach (string line in lineChannel.Reader.ReadAllAsync())
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
                lineChannel.Writer.TryComplete();

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
