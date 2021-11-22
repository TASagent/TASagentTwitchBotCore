namespace TASagentTwitchBot.Core.Chat;

public interface IChatMessageHandler
{
    void HandleChatMessage(IRC.IRCMessage message);
}

public class ChatMessageHandler : IChatMessageHandler
{

    private readonly ICommunication communication;
    private readonly IServiceScopeFactory scopeFactory;

    public ChatMessageHandler(
        ICommunication communication,
        IServiceScopeFactory scopeFactory)
    {
        this.communication = communication;
        this.scopeFactory = scopeFactory;
    }

    public virtual async void HandleChatMessage(IRC.IRCMessage message)
    {
        if (message.ircCommand != IRC.IRCCommand.PrivMsg && message.ircCommand != IRC.IRCCommand.Whisper)
        {
            communication.SendDebugMessage($"Error: Passing forward non-chat message:\n    {message}");
            return;
        }

        IRC.TwitchChatter? chatter = await IRC.TwitchChatter.FromIRCMessage(message, communication, scopeFactory);

        if (chatter is null)
        {
            return;
        }

        communication.DispatchChatMessage(chatter);
    }
}
