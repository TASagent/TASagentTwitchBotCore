using System;

namespace TASagentTwitchBot.Core.WebServer
{
    public class WebServerCommunicationHandler : ICommunication
    {
        public event ICommunication.DebugMessageHandler DebugMessageHandlers;

        event ICommunication.SendMessageHandler ICommunication.SendMessageHandlers
        {
            add => throw new NotSupportedException();
            remove => throw new NotSupportedException();
        }

        event ICommunication.SendWhisperHandler ICommunication.SendWhisperHandlers
        {
            add => throw new NotSupportedException();
            remove => throw new NotSupportedException();
        }

        event ICommunication.ReceiveMessageHandler ICommunication.ReceiveMessageLoggers
        {
            add => throw new NotSupportedException();
            remove => throw new NotSupportedException();
        }

        event ICommunication.ReceiveMessageHandler ICommunication.ReceiveMessageHandlers
        {
            add => throw new NotSupportedException();
            remove => throw new NotSupportedException();
        }

        event ICommunication.ReceiveEventHandler ICommunication.ReceiveEventHandlers
        {
            add => throw new NotSupportedException();
            remove => throw new NotSupportedException();
        }

        event ICommunication.ReceiveNotificationHandler ICommunication.ReceiveNotificationHandlers
        {
            add => throw new NotSupportedException();
            remove => throw new NotSupportedException();
        }

        event ICommunication.ReceiveNotificationHandler ICommunication.ReceivePendingNotificationHandlers
        {
            add => throw new NotSupportedException();
            remove => throw new NotSupportedException();
        }

        public WebServerCommunicationHandler() { }

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

        public void SendPublicChatMessage(string message) => throw new NotSupportedException();
        public void SendChatWhisper(string username, string message) => throw new NotSupportedException();
        public void DispatchChatMessage(IRC.TwitchChatter chatter) => throw new NotSupportedException();
        public void NotifyEvent(string message) => throw new NotSupportedException();
        public void NotifyNotification(int id, string message) => throw new NotSupportedException();
        public void NotifyPendingNotification(int id, string message) => throw new NotSupportedException();
    }
}
