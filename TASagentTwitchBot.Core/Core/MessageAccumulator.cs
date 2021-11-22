using System.Web;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.Web.Hubs;

namespace TASagentTwitchBot.Core;

public interface IMessageAccumulator
{
    MessageBlock<SimpleMessage> GetAllEvents();
    MessageBlock<SimpleMessage> GetAllChats();
    MessageBlock<SimpleMessage> GetAllDebugs();
    MessageBlock<NotificationMessage> GetAllNotifications();
    MessageBlock<NotificationMessage> GetAllPendingNotifications();

    bool IsAuthenticatedUser(string connectionId);
    void AddAuthenticatedUser(string connectionId);
    void ClearAuthenticatedUsers();

    bool RemovePendingNotification(int index);
}

public class MessageAccumulator : IMessageAccumulator, IDisposable
{
    private readonly Config.BotConfiguration botConfig;
    private readonly IHubContext<MonitorHub> monitorHubContext;

    private readonly HashSet<string> authenticatedUsers = new HashSet<string>();

    private readonly MessageBuffer<SimpleMessage> eventBuffer = new MessageBuffer<SimpleMessage>(1000);
    private readonly MessageBuffer<SimpleMessage> chatBuffer = new MessageBuffer<SimpleMessage>(1000);
    private readonly MessageBuffer<SimpleMessage> debugBuffer = new MessageBuffer<SimpleMessage>(1000);
    private readonly MessageBuffer<NotificationMessage> notificationBuffer = new MessageBuffer<NotificationMessage>(200);
    private readonly MessageBuffer<NotificationMessage> pendingNotificationBuffer = new MessageBuffer<NotificationMessage>(200);

    private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private readonly Task monitorTask;
    private bool disposedValue;

    public MessageAccumulator(
        Config.BotConfiguration botConfig,
        ICommunication communication,
        IHubContext<MonitorHub> monitorHubContext)
    {
        this.botConfig = botConfig;
        this.monitorHubContext = monitorHubContext;

        communication.ReceiveEventHandlers += ReceiveEvent;
        communication.ReceiveMessageLoggers += ReceiveChatter;
        communication.DebugMessageHandlers += ReceiveDebugMessage;
        communication.SendWhisperHandlers += ReceiveWhisperSent;
        communication.SendMessageHandlers += ReceiveMessageSent;
        communication.ReceiveNotificationHandlers += ReceiveNotification;
        communication.ReceivePendingNotificationHandlers += ReceivePendingNotification;

        if (botConfig.UseThreadedMonitors)
        {
            monitorTask = Task.Run(MonitorMessages);
        }
        else
        {
            monitorTask = MonitorMessages();
        }
    }

    private async Task MonitorMessages()
    {
        try
        {
            while (true)
            {
                if (eventBuffer.PendingMessages > 0)
                {
                    //Handle
                    await monitorHubContext.Clients.Group("Authenticated").SendAsync(
                        "ReceiveNewEvents", eventBuffer.GetPendingMessages());
                }

                if (chatBuffer.PendingMessages > 0)
                {
                    //Handle
                    await monitorHubContext.Clients.Group("Authenticated").SendAsync(
                        "ReceiveNewChats", chatBuffer.GetPendingMessages());
                }

                if (debugBuffer.PendingMessages > 0)
                {
                    //Handle
                    await monitorHubContext.Clients.Group("Authenticated").SendAsync(
                        "ReceiveNewDebugs", debugBuffer.GetPendingMessages());
                }

                if (notificationBuffer.PendingMessages > 0)
                {
                    //Handle
                    await monitorHubContext.Clients.Group("Authenticated").SendAsync(
                        "ReceiveNewNotifications", notificationBuffer.GetPendingMessages());
                }

                if (pendingNotificationBuffer.PendingMessages > 0)
                {
                    //Handle
                    await monitorHubContext.Clients.Group("Authenticated").SendAsync(
                        "ReceiveNewPendingNotifications", pendingNotificationBuffer.GetPendingMessages());
                }

                await Task.Delay(1000, cancellationTokenSource.Token);
            }
        }
        catch (TaskCanceledException) { /* swallow */ }
        catch (ThreadAbortException) { /* swallow */ }
        catch (ObjectDisposedException) { /* swallow */ }
    }

    public MessageBlock<SimpleMessage> GetAllEvents() => eventBuffer.GetAllMessages();
    public MessageBlock<SimpleMessage> GetAllChats() => chatBuffer.GetAllMessages();
    public MessageBlock<SimpleMessage> GetAllDebugs() => debugBuffer.GetAllMessages();
    public MessageBlock<NotificationMessage> GetAllNotifications() => notificationBuffer.GetAllMessages();
    public MessageBlock<NotificationMessage> GetAllPendingNotifications() => pendingNotificationBuffer.GetAllMessages();

    private void ReceiveMessageSent(string message)
    {
        chatBuffer.AddMessage(new SimpleMessage($"<span style=\"color: #FF0000\">{botConfig.BotName}</span>:  {HttpUtility.HtmlEncode(message)}"));
    }

    private void ReceiveWhisperSent(string username, string message)
    {
        chatBuffer.AddMessage(new SimpleMessage($"<span style=\"color: #FF0000\">{botConfig.BotName}</span>:  {HttpUtility.HtmlEncode($"/w {username} {message}")}"));
    }

    private void ReceiveDebugMessage(string message, MessageType messageType)
    {
        switch (messageType)
        {
            case MessageType.Debug:
                debugBuffer.AddMessage(new SimpleMessage(HttpUtility.HtmlEncode(message)));
                break;

            case MessageType.Warning:
                debugBuffer.AddMessage(new SimpleMessage($"<span style=\"color: #FFFF00\">{HttpUtility.HtmlEncode(message)}</span>"));
                break;

            case MessageType.Error:
                debugBuffer.AddMessage(new SimpleMessage($"<span style=\"color: #FF0000\">{HttpUtility.HtmlEncode(message)}</span>"));
                break;

            default:
                throw new Exception($"Unexpected MessageType: {message}");
        }
    }

    private void ReceiveChatter(IRC.TwitchChatter chatter)
    {
        chatBuffer.AddMessage(new SimpleMessage($"<span style=\"color: {chatter.User.Color}\">{HttpUtility.HtmlEncode(chatter.User.TwitchUserName)}</span>:  {HttpUtility.HtmlEncode(chatter.Message)}"));
    }

    private void ReceiveEvent(string message)
    {
        eventBuffer.AddMessage(new SimpleMessage(message));
    }

    private void ReceiveNotification(int id, string message)
    {
        notificationBuffer.AddMessage(new NotificationMessage(id, message));
    }

    private void ReceivePendingNotification(int id, string message)
    {
        pendingNotificationBuffer.AddMessage(new NotificationMessage(id, message));
    }

    public bool IsAuthenticatedUser(string connectionId) => authenticatedUsers.Contains(connectionId);

    public void AddAuthenticatedUser(string connectionId) => authenticatedUsers.Add(connectionId);

    public void ClearAuthenticatedUsers()
    {
        foreach (string user in authenticatedUsers)
        {
            monitorHubContext.Groups.RemoveFromGroupAsync(user, "Authenticated");
        }

        authenticatedUsers.Clear();
    }

    public bool RemovePendingNotification(int index) => pendingNotificationBuffer.RemoveFirst(x => x.Id == index);

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                cancellationTokenSource.Cancel();

                monitorTask.Wait(1000);
                monitorTask.Dispose();

                cancellationTokenSource.Dispose();
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

public class MessageBuffer<T>
{
    private readonly Dictionary<int, T> messageDict = new Dictionary<int, T>();

    private readonly int capacity;

    private int oldestIndex = 0;
    private int currentIndex = 0;
    private int lastOutputIndex = 0;

    public int PendingMessages => currentIndex - lastOutputIndex;

    public MessageBuffer(int capacity)
    {
        this.capacity = capacity;
    }

    public bool RemoveFirst(Func<T, bool> selector)
    {
        if (selector is null)
        {
            return false;
        }

        int key = -1;
        foreach (var pair in messageDict)
        {
            if (selector.Invoke(pair.Value))
            {
                key = pair.Key;
                break;
            }
        }

        if (key == -1)
        {
            return false;
        }

        messageDict.Remove(key);
        return true;
    }

    public void Clear()
    {
        messageDict.Clear();
        oldestIndex = 0;
        currentIndex = 0;
        lastOutputIndex = 0;
    }

    public void AddMessage(T newMessage)
    {
        messageDict.Add(currentIndex++, newMessage);

        while (messageDict.Count > capacity)
        {
            messageDict.Remove(oldestIndex++);
        }
    }

    public MessageBlock<T> GetAllMessages()
    {
        return new MessageBlock<T>(messageDict.Values.ToList());
    }

    public MessageBlock<T> GetPendingMessages()
    {
        List<T> newMessages = new List<T>(PendingMessages);

        for (int i = lastOutputIndex; i < currentIndex; i++)
        {
            if (messageDict.ContainsKey(i))
            {
                newMessages.Add(messageDict[i]);
            }
        }

        lastOutputIndex = currentIndex;

        return new MessageBlock<T>(newMessages);
    }

}

public record MessageBlock<T>(List<T> Messages);
public record SimpleMessage(string Message);
public record NotificationMessage(int Id, string Message);
