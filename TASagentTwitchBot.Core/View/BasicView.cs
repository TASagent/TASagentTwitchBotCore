using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.View
{
    public class BasicView : IConsoleOutput, IDisposable
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly ICommunication communication;
        private readonly ApplicationManagement applicationManagement;

        private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
        private readonly CountdownEvent readers = new CountdownEvent(1);

        private readonly Channel<ConsoleKeyInfo> consoleChannel;

        private bool disposedValue = false;

        private void WriteErrorLine(string line) => DebugMessageHandler(line, MessageType.Error);

        public BasicView(
            Config.IBotConfigContainer botConfigContainer,
            ICommunication communication,
            ApplicationManagement applicationManagement)
        {
            botConfig = botConfigContainer.BotConfig;
            this.communication = communication;
            this.applicationManagement = applicationManagement;

            consoleChannel = Channel.CreateUnbounded<ConsoleKeyInfo>();

            LaunchListeners();

            communication.ReceivePendingNotificationHandlers += ReceivePendingNotification;
            communication.ReceiveEventHandlers += ReceiveEventHandler;
            communication.ReceiveMessageLoggers += ReceiveMessageHandler;
            communication.SendMessageHandlers += SendPublicChatHandler;
            communication.SendWhisperHandlers += SendWhisperHandler;
            communication.DebugMessageHandlers += DebugMessageHandler;
        }

        private void ReceiveEventHandler(string message)
        {
            Console.WriteLine($"Event   {message}");
        }

        private void ReceivePendingNotification(int id, string message)
        {
            Console.WriteLine($"Notice  Pending Notification {id}: {message}");
        }

        private void DebugMessageHandler(string message, MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Debug:
                    Console.WriteLine(message);
                    break;

                case MessageType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;

                case MessageType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;

                default:
                    throw new NotSupportedException($"Unexpected messageType: {messageType}");
            }
        }

        private void SendPublicChatHandler(string message)
        {
            Console.WriteLine($"Chat    {botConfig.BotName}: {message}");
        }

        private void SendWhisperHandler(string username, string message)
        {
            Console.WriteLine($"Chat    {botConfig.BotName} whispers {username}: {message}");
        }

        private void ReceiveMessageHandler(IRC.TwitchChatter chatter)
        {
            Console.WriteLine($"Chat    {chatter.User.TwitchUserName}: {chatter.Message}");
        }

        public void LaunchListeners()
        {
            ReadKeysHandler();
            HandleKeysLoop();
        }

        private async Task<ConsoleKeyInfo> WaitForConsoleKeyInfo()
        {
            ConsoleKeyInfo keyInfo = default;
            try
            {
                await Task.Run(() => keyInfo = Console.ReadKey(true));
            }
            catch (Exception ex)
            {
                WriteErrorLine($"Exception: {ex}");
            }

            return keyInfo;
        }

        private async void ReadKeysHandler()
        {
            try
            {
                readers.AddCount();

                while (true)
                {
                    ConsoleKeyInfo nextKey = await WaitForConsoleKeyInfo().WithCancellation(generalTokenSource.Token);

                    //Bail if we're trying to quit
                    if (generalTokenSource.IsCancellationRequested)
                    {
                        break;
                    }

                    await consoleChannel.Writer.WriteAsync(nextKey);
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
                //Log Error
                WriteErrorLine($"Exception: {ex}");
            }
            finally
            {
                readers.Signal();
            }
        }


        private async void HandleKeysLoop()
        {
            while (true)
            {
                bool handled = false;

                Console.CursorVisible = false;

                ConsoleKeyInfo input = await consoleChannel.Reader.ReadAsync();

                if (input.Key == ConsoleKey.Q && ((input.Modifiers & ConsoleModifiers.Control) != 0))
                {
                    applicationManagement.TriggerExit();
                    handled = true;
                }

                if (!handled)
                {
                    Console.Beep();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    communication.ReceiveEventHandlers -= ReceiveEventHandler;
                    communication.ReceiveMessageLoggers -= ReceiveMessageHandler;
                    communication.SendMessageHandlers -= SendPublicChatHandler;
                    communication.SendWhisperHandlers -= SendWhisperHandler;
                    communication.DebugMessageHandlers -= DebugMessageHandler;

                    generalTokenSource.Cancel();

                    readers.Signal();
                    readers.Wait();
                    readers.Dispose();

                    generalTokenSource.Dispose();
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
