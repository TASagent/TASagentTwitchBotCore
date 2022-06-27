using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Hubs;

public class MonitorHub : Hub
{
    private readonly Config.BotConfiguration botConfig;
    private readonly IMessageAccumulator messageAccumulator;

    public MonitorHub(
        Config.BotConfiguration botConfig,
        IMessageAccumulator messageAccumulator)
    {
        this.botConfig = botConfig;
        this.messageAccumulator = messageAccumulator;
    }

    public async Task<bool> Authenticate(string token)
    {
        AuthDegree attemptedAuth = botConfig.AuthConfiguration.CheckAuthString(token);

        if (!botConfig.AuthConfiguration.PublicAuthAllowed && attemptedAuth <= AuthDegree.Privileged)
        {
            return false;
        }

        if (attemptedAuth == AuthDegree.None)
        {
            return false;
        }

        messageAccumulator.AddAuthenticatedUser(Context.ConnectionId);

        await Groups.AddToGroupAsync(Context.ConnectionId, "Authenticated");

        return true;
    }

    public MessageBlock<SimpleMessage> RequestAllChats()
    {
        if (messageAccumulator.IsAuthenticatedUser(Context.ConnectionId))
        {
            return messageAccumulator.GetAllChats();
        }

        //Failed to authenticate
        return new MessageBlock<SimpleMessage>(new List<SimpleMessage>());
    }

    public MessageBlock<SimpleMessage> RequestAllEvents()
    {
        if (messageAccumulator.IsAuthenticatedUser(Context.ConnectionId))
        {
            return messageAccumulator.GetAllEvents();
        }

        //Failed to authenticate
        return new MessageBlock<SimpleMessage>(new List<SimpleMessage>());
    }

    public MessageBlock<SimpleMessage> RequestAllDebugs()
    {
        if (messageAccumulator.IsAuthenticatedUser(Context.ConnectionId))
        {
            return messageAccumulator.GetAllDebugs();
        }

        //Failed to authenticate
        return new MessageBlock<SimpleMessage>(new List<SimpleMessage>());
    }

    public MessageBlock<NotificationMessage> RequestAllNotifications()
    {
        if (messageAccumulator.IsAuthenticatedUser(Context.ConnectionId))
        {
            return messageAccumulator.GetAllNotifications();
        }

        //Failed to authenticate
        return new MessageBlock<NotificationMessage>(new List<NotificationMessage>());
    }

    public MessageBlock<NotificationMessage> RequestAllPendingNotifications()
    {
        if (messageAccumulator.IsAuthenticatedUser(Context.ConnectionId))
        {
            return messageAccumulator.GetAllPendingNotifications();
        }

        //Failed to authenticate
        return new MessageBlock<NotificationMessage>(new List<NotificationMessage>());
    }
}
