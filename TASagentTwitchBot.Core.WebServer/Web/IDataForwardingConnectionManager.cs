using Microsoft.AspNetCore.SignalR;
using System.Diagnostics.CodeAnalysis;

namespace TASagentTwitchBot.Core.WebServer.Web;

[AutoRegister]
public interface IDataForwardingConnectionManager
{
    void AddConnection(string userName, string connectionId);
    bool RemoveConnection(string userName);

    bool TryGetConnectionId(string userName, [MaybeNullWhen(false)] out string connectionId);
}


public class DataForwardingConnectionManager : IDataForwardingConnectionManager
{
    private readonly Dictionary<string, string> connectionMapping = new Dictionary<string, string>();
    private readonly static object dictLock = new object();

    public DataForwardingConnectionManager()
    {

    }

    public void AddConnection(string userName, string connectionId)
    {
        lock (dictLock)
        {
            connectionMapping[userName.ToLower()] = connectionId;
        }
    }

    public bool RemoveConnection(string userName)
    {
        lock (dictLock)
        {
            return connectionMapping.Remove(userName.ToLower());
        }
    }

    public bool TryGetConnectionId(string userName, [MaybeNullWhen(false)] out string connectionId)
    {
        lock (dictLock)
        {
            return connectionMapping.TryGetValue(userName.ToLower(), out connectionId);
        }
    }
}

public static class DataForwardingConnectionManagerExtensions
{
    public static bool TryGetClient(
        this IDataForwardingConnectionManager dataForwardingConnectionManager,
        IHubContext<Hubs.BotDataForwardingHub> hubContext,
        string userName,
        [MaybeNullWhen(false)] out ISingleClientProxy client)
    {
        if (!dataForwardingConnectionManager.TryGetConnectionId(userName, out string? connectionId))
        {
            client = null;
            return false;
        }

        client = hubContext.Clients.Client(connectionId);
        return true;
    }
}