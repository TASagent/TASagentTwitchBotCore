using System;
using System.Threading;
using System.Runtime.CompilerServices;

namespace TASagentTwitchBot.Core
{
    public class ErrorHandler
    {
        private readonly ICommunication communication;

        private static readonly object locker = new object();

        private static readonly Lazy<Logs.LocalLogger> errorLog = new Lazy<Logs.LocalLogger>(
            () => new Logs.LocalLogger("ErrorLogs", "errors"));
        private static Logs.LocalLogger ErrorLog => errorLog.Value;

        public ErrorHandler(ICommunication communication)
        {
            this.communication = communication;
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
                lock (locker)
                {
                    ErrorLog.PushLine(
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

                lock (locker)
                {
                    ErrorLog.PushLine(
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
                lock (locker)
                {
                    ErrorLog.PushLine(
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
                lock (locker)
                {
                    ErrorLog.PushLine(
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
                lock (locker)
                {
                    ErrorLog.PushLine(
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
    }
}
