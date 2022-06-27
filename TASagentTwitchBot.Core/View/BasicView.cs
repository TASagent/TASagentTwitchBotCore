using System.Threading.Channels;

namespace TASagentTwitchBot.Core.View;

public class BasicView : IConsoleOutput, IShutdownListener, IStartupListener, IDisposable
{
    private readonly Config.BotConfiguration botConfig;
    private readonly ICommunication communication;
    private readonly ApplicationManagement applicationManagement;

    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
    private Task? readHandlerTask = null;
    private Task? keysHandlerTask = null;

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

        communication.ReceivePendingNotificationHandlers += ReceivePendingNotification;
        communication.ReceiveEventHandlers += ReceiveEventHandler;
        communication.ReceiveMessageLoggers += ReceiveMessageHandler;
        communication.SendMessageHandlers += SendPublicChatHandler;
        communication.SendWhisperHandlers += SendWhisperHandler;
        communication.DebugMessageHandlers += DebugMessageHandler;

        communication.SendDebugMessage("BasicView Connected.  Listening for Ctrl+Q to quit gracefully.\n");
    }

    public virtual void NotifyStartup()
    {
        readHandlerTask = Task.Run(ReadKeysHandler);
        keysHandlerTask = Task.Run(HandleKeysLoop);
    }

    protected virtual void ReceiveEventHandler(string message)
    {
        Console.WriteLine($"Event   {message}");
    }

    protected virtual void ReceivePendingNotification(int id, string message)
    {
        Console.WriteLine($"Notice  Pending Notification {id}: {message}");
    }

    protected virtual void DebugMessageHandler(string message, MessageType messageType)
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

    protected virtual void SendPublicChatHandler(string message)
    {
        Console.WriteLine($"Chat    {botConfig.BotName}: {message}");
    }

    protected virtual void SendWhisperHandler(string username, string message)
    {
        Console.WriteLine($"Chat    {botConfig.BotName} whispers {username}: {message}");
    }

    protected virtual void ReceiveMessageHandler(IRC.TwitchChatter chatter)
    {
        Console.WriteLine($"Chat    {chatter.User.TwitchUserName}: {chatter.Message}");
    }


    private async Task ReadKeysHandler()
    {
        try
        {
            while (true)
            {
                ConsoleKeyInfo nextKey = default;

                //await Task.Run(() => nextKey = Console.ReadKey(true), generalTokenSource.Token);
                await Task.Run(() => nextKey = Console.ReadKey(true)).WithCancellation(generalTokenSource.Token);

                //Bail if we're trying to quit
                if (generalTokenSource.IsCancellationRequested)
                {
                    break;
                }

                await consoleChannelWriter.WriteAsync(nextKey);
            }
        }
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
            else
            {
                HandleKeys(input);
            }
        }
    }

    protected virtual void HandleKeys(in ConsoleKeyInfo input) { }

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

                readHandlerTask?.Wait(2_000);
                keysHandlerTask?.Wait(2_000);

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
