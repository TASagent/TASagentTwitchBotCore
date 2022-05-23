namespace TASagentTwitchBot.Core;

public interface ICommunication
{
    public delegate void DebugMessageHandler(string message, MessageType messageType);
    public delegate void SendMessageHandler(string message);
    public delegate void SendWhisperHandler(string username, string message);
    public delegate void ReceiveMessageHandler(IRC.TwitchChatter chatter);
    public delegate void ReceiveEventHandler(string message);
    public delegate void ReceiveNotificationHandler(int id, string message);

    event DebugMessageHandler? DebugMessageHandlers;
    event SendMessageHandler? SendMessageHandlers;
    event SendWhisperHandler? SendWhisperHandlers;
    event ReceiveMessageHandler? ReceiveMessageLoggers;
    event ReceiveMessageHandler? ReceiveMessageHandlers;
    event ReceiveEventHandler? ReceiveEventHandlers;
    event ReceiveNotificationHandler? ReceiveNotificationHandlers;
    event ReceiveNotificationHandler? ReceivePendingNotificationHandlers;

    void SendPublicChatMessage(string message);
    void SendChatWhisper(string username, string message);
    void DispatchChatMessage(IRC.TwitchChatter chatter);

    void NotifyEvent(string message);
    void NotifyNotification(int id, string message);
    void NotifyPendingNotification(int id, string message);
    void SendDebugMessage(string message);
    void SendWarningMessage(string message);
    void SendErrorMessage(string message);
}

public enum MessageType
{
    Debug = 0,
    Warning,
    Error
}


public class CommunicationHandler : ICommunication
{
    public event ICommunication.DebugMessageHandler? DebugMessageHandlers;
    public event ICommunication.SendMessageHandler? SendMessageHandlers;
    public event ICommunication.SendWhisperHandler? SendWhisperHandlers;
    public event ICommunication.ReceiveMessageHandler? ReceiveMessageLoggers;
    public event ICommunication.ReceiveMessageHandler? ReceiveMessageHandlers;
    public event ICommunication.ReceiveEventHandler? ReceiveEventHandlers;
    public event ICommunication.ReceiveNotificationHandler? ReceiveNotificationHandlers;
    public event ICommunication.ReceiveNotificationHandler? ReceivePendingNotificationHandlers;

    public CommunicationHandler() { }

    public void SendPublicChatMessage(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            SendMessageHandlers?.Invoke(message);
        }
    }

    public void SendChatWhisper(string username, string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            SendWhisperHandlers?.Invoke(username, message);
        }
    }

    public void DispatchChatMessage(IRC.TwitchChatter chatter)
    {
        ReceiveMessageLoggers?.Invoke(chatter);
        ReceiveMessageHandlers?.Invoke(chatter);
    }

    public void SendDebugMessage(string message)
    {
        DebugMessageHandlers?.Invoke(message, MessageType.Debug);
    }

    public void SendWarningMessage(string message)
    {
        DebugMessageHandlers?.Invoke(message, MessageType.Warning);
    }

    public void SendErrorMessage(string message)
    {
        DebugMessageHandlers?.Invoke(message, MessageType.Error);
    }

    public void NotifyEvent(string message)
    {
        ReceiveEventHandlers?.Invoke(message);
    }

    public void NotifyNotification(int id, string message)
    {
        ReceiveNotificationHandlers?.Invoke(id, message);
    }

    public void NotifyPendingNotification(int id, string message)
    {
        ReceivePendingNotificationHandlers?.Invoke(id, message);
    }
}
