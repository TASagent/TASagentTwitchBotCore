using System.Threading.Channels;

namespace TASagentTwitchBot.Core.View;

public class BasicView : IConsoleOutput, IShutdownListener, IDisposable
{
    private readonly Config.BotConfiguration botConfig;
    private readonly ICommunication communication;
    private readonly ApplicationManagement applicationManagement;

    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
    private Task? readHandlerTask;
    private Task? keysHandlerTask;

    private readonly ChannelWriter<ConsoleKeyInfo> consoleChannelWriter;
    private readonly ChannelReader<ConsoleKeyInfo> consoleChannelReader;

    private bool disposedValue = false;

    public BasicView(
        Config.BotConfiguration botConfig,
        ICommunication communication,
        ApplicationManagement applicationManagement)
    {
        this.botConfig = botConfig;
        this.communication = communication;
        this.applicationManagement = applicationManagement;

        Channel<ConsoleKeyInfo> consoleChannel = Channel.CreateUnbounded<ConsoleKeyInfo>();
        consoleChannelWriter = consoleChannel.Writer;
        consoleChannelReader = consoleChannel.Reader;

        applicationManagement.RegisterShutdownListener(this);

        if (botConfig.UseThreadedMonitors)
        {
            Task.Run(LaunchListeners);
        }
        else
        {
            LaunchListeners();
        }

        communication.ReceivePendingNotificationHandlers += ReceivePendingNotification;
        communication.ReceiveEventHandlers += ReceiveEventHandler;
        communication.ReceiveMessageLoggers += ReceiveMessageHandler;
        communication.SendMessageHandlers += SendPublicChatHandler;
        communication.SendWhisperHandlers += SendWhisperHandler;
        communication.DebugMessageHandlers += DebugMessageHandler;

        communication.SendDebugMessage("BasicView Connected.  Listening for Ctrl+Q to quit gracefully.\n");
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
        readHandlerTask = ReadKeysHandler();
        keysHandlerTask = HandleKeysLoop();
    }

    private async Task ReadKeysHandler()
    {
        try
        {
            while (true)
            {
                ConsoleKeyInfo nextKey = default;

                await Task.Run(() => nextKey = Console.ReadKey(true), generalTokenSource.Token);

                //Bail if we're trying to quit
                if (generalTokenSource.IsCancellationRequested)
                {
                    break;
                }

                await consoleChannelWriter.WriteAsync(nextKey);
            }
        }
        catch (TaskCanceledException) { /* swallow */ }
        catch (OperationCanceledException) { /* swallow */ }
        catch (Exception ex)
        {
            //Log Error
            communication.SendErrorMessage($"BasicView Exception: {ex}");
        }
    }

    private async Task HandleKeysLoop()
    {
        Console.CursorVisible = false;
        await foreach (ConsoleKeyInfo input in consoleChannelReader.ReadAllAsync())
        {
            Console.CursorVisible = false;
            if (input.Key == ConsoleKey.Q && ((input.Modifiers & ConsoleModifiers.Control) != 0))
            {
                applicationManagement.TriggerExit();
            }
        }
    }

    public void NotifyShuttingDown()
    {
        generalTokenSource.Cancel();

        consoleChannelWriter.TryComplete();
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

                consoleChannelWriter.TryComplete();

                readHandlerTask?.Wait(500);
                readHandlerTask?.Dispose();
                readHandlerTask = null;

                keysHandlerTask?.Wait(500);
                keysHandlerTask?.Dispose();
                keysHandlerTask = null;

                generalTokenSource.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
