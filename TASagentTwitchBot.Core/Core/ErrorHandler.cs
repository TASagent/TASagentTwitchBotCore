using System.Runtime.CompilerServices;

namespace TASagentTwitchBot.Core;

public class ErrorHandler : IDisposable
{
    private readonly Config.BotConfiguration botConfig;
    private readonly ICommunication communication;

    private static readonly object errorLock = new object();
    private static readonly Lazy<Logs.LocalLogger> errorLog = new Lazy<Logs.LocalLogger>(
        () => new Logs.LocalLogger("ErrorLogs", "errors"));


    private static readonly object exceptionLock = new object();
    private static readonly Lazy<Logs.LocalLogger> exceptionLog = new Lazy<Logs.LocalLogger>(
        () => new Logs.LocalLogger("ErrorLogs", "exceptions"));

    private bool disposedValue;

    public ErrorHandler(
        Config.BotConfiguration botConfig,
        ICommunication communication)
    {
        this.botConfig = botConfig;
        this.communication = communication;

        communication.DebugMessageHandlers += DebugMessageHandler;
    }

    private void DebugMessageHandler(string message, MessageType messageType)
    {
        if (messageType != MessageType.Error || !botConfig.LogAllErrors)
        {
            return;
        }

        try
        {
            lock (errorLock)
            {
                errorLog.Value.PushLine($"Error Log: {message}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"\nLogging error found: {e.Message}\nPlease inform author of this error!");
            Thread.Sleep(5000);
            Environment.Exit(1);
        }
    }

    public void LogGeneralErrorMessage(string message)
    {
        try
        {
            lock (errorLock)
            {
                errorLog.Value.PushLine($"General Error Message: {message}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"\nLogging error found: {e.Message}\nPlease inform author of this error!");
            Thread.Sleep(5000);
            Environment.Exit(1);
        }
    }

    public void LogExternalException(
        Exception ex,
        string filePath,
        string memberName,
        int lineNumber)
    {
        try
        {
            communication.SendErrorMessage($"External Exception: {ex}");
        }
        catch (Exception) { }

        try
        {
            lock (exceptionLock)
            {
                exceptionLog.Value.PushLine(
                    $"External Exception at {DateTime.UtcNow}\n" +
                    $"  Calling File: {filePath}:{lineNumber}\n" +
                    $"  Calling Member: {memberName}\n" +
                    $"  Exception: {ex}\n\n\n");
            }
        }
        catch (Exception e)
        {
            communication.SendErrorMessage($"\nLogging error found: {e.Message}\nPlease inform author of this error!");
            Thread.Sleep(5000);
            Environment.Exit(1);
        }
    }

    public void LogFatalException(
        Exception ex,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        try
        {
            communication.SendErrorMessage($"Fatal Exception: {ex}");
        }
        catch (Exception) { }

        try
        {

            lock (exceptionLock)
            {
                exceptionLog.Value.PushLine(
                    $"Fatal Exception at {DateTime.UtcNow}\n" +
                    $"  Calling File: {filePath}:{lineNumber}\n" +
                    $"  Calling Member: {memberName}\n" +
                    $"  Exception: {ex}\n\n\n");
            }

            communication.SendErrorMessage("\nShutting down now...");
            Thread.Sleep(3000);
            Environment.Exit(1);
        }
        catch (Exception e)
        {
            communication.SendErrorMessage($"\nLogging error found: {e.Message}. Please inform author of this error!");
            Thread.Sleep(5000);
            Environment.Exit(1);
        }
    }

    public void LogSystemException(
        Exception ex,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        try
        {
            communication.SendErrorMessage($"Exception: {ex}");
        }
        catch (Exception) { }

        try
        {
            lock (exceptionLock)
            {
                exceptionLog.Value.PushLine(
                    $"System Exception at {DateTime.UtcNow}\n" +
                    $"  Calling File: {filePath}:{lineNumber}\n" +
                    $"  Calling Member: {memberName}\n" +
                    $"  Exception: {ex}\n\n\n");
            }
        }
        catch (Exception e)
        {
            communication.SendErrorMessage($"\nLogging error found: {e.Message}. Please inform author of this error!");
            Thread.Sleep(5000);
            Environment.Exit(1);
        }
    }

    public void LogCommandException(
        Exception ex,
        string botCmd,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        try
        {
            communication.SendErrorMessage($"Command Exception: {ex}");
        }
        catch (Exception) { }

        try
        {
            lock (exceptionLock)
            {
                exceptionLog.Value.PushLine(
                    $"Command Exeption: {DateTime.UtcNow}\n" +
                    $"  Calling File: {filePath}:{lineNumber}\n" +
                    $"  Calling Member: {memberName}\n" +
                    $"  Command: {botCmd}\n" +
                    $"  Exception: {ex}\n\n\n");
            }
        }
        catch (Exception e)
        {
            communication.SendErrorMessage($"\nLogging error found: {e.Message}. Please inform author of this error!");
            Thread.Sleep(5000);
            Environment.Exit(1);
        }
    }

    public void LogMessageException(
        Exception ex,
        string chatMessage,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        try
        {
            communication.SendErrorMessage($"Message Exception: {ex}");
        }
        catch (Exception) { }

        try
        {
            lock (exceptionLock)
            {
                exceptionLog.Value.PushLine(
                    $"Message Exeption: {DateTime.UtcNow}\n" +
                    $"  Calling File: {filePath}:{lineNumber}\n" +
                    $"  Calling Member: {memberName}\n" +
                    $"  ChatMessage: {chatMessage}\n" +
                    $"  Exception: {ex}\n\n\n");
            }
        }
        catch (Exception e)
        {
            communication.SendErrorMessage($"\nLogging error found: {e.Message}. Please inform author of this error!");
            Thread.Sleep(5000);
            Environment.Exit(1);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                communication.DebugMessageHandlers -= DebugMessageHandler;

                if (errorLog.IsValueCreated)
                {
                    errorLog.Value.Dispose();
                }

                if (exceptionLog.IsValueCreated)
                {
                    exceptionLog.Value.Dispose();
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
