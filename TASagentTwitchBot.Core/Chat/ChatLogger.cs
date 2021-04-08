using System;
using System.Threading.Channels;

namespace TASagentTwitchBot.Core.Chat
{
    public class ChatLogger : IDisposable
    {
        private readonly Lazy<Logs.LocalLogger> chatLog;
        private readonly Channel<string> lineChannel;
        private bool disposedValue;

        public ChatLogger(
            ICommunication communication)
        {
            lineChannel = Channel.CreateUnbounded<string>();
            chatLog = new Lazy<Logs.LocalLogger>(() => new Logs.LocalLogger("ChatLogs", "chat"));

            communication.SendMessageHandlers += WriteOutgoingMessage;
            communication.SendWhisperHandlers += WriteOutgoingWhisper;
            communication.ReceiveMessageLoggers += WriteIncomingMessage;

            HandleLines();
        }

        private void WriteOutgoingMessage(string message)
        {
            lineChannel.Writer.TryWrite($"[{DateTime.Now:G}] TASagentPuppet: {message}");
        }

        private void WriteOutgoingWhisper(string username, string message)
        {
            lineChannel.Writer.TryWrite($"[{DateTime.Now:G}] TASagentPuppet /w {username} : {message}");
        }

        private void WriteIncomingMessage(IRC.TwitchChatter chatter)
        {
            lineChannel.Writer.TryWrite(chatter.ToLogString());
        }

        public async void HandleLines()
        {
            await foreach(string line in lineChannel.Reader.ReadAllAsync())
            {
                chatLog.Value.PushLine(line);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    lineChannel.Writer.TryComplete();

                    if (chatLog.IsValueCreated)
                    {
                        chatLog.Value.Dispose();
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
}
