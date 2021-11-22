
using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Commands;

namespace TASagentTwitchBot.Plugin.TTTAS;

public class TTTASCommandSystem : ICommandContainer
{
    private readonly ICommunication communication;
    private readonly ITTTASHandler tttasHandler;
    private readonly ITTTASProvider tttasProvider;

    private readonly TTTASConfiguration tttasConfig;

    public TTTASCommandSystem(
        ICommunication communication,
        ITTTASHandler tttasHandler,
        ITTTASProvider tttasProvider,
        TTTASConfiguration tttasConfig)
    {
        this.communication = communication;
        this.tttasHandler = tttasHandler;
        this.tttasProvider = tttasProvider;
        this.tttasConfig = tttasConfig;
    }

    public void RegisterCommands(
        Dictionary<string, CommandHandler> commands,
        Dictionary<string, HelpFunction> helpFunctions,
        Dictionary<string, SetFunction> setFunctions,
        Dictionary<string, GetFunction> getFunctions)
    {
        if (tttasConfig.Command.Enabled)
        {
            commands.Add(tttasConfig.Command.CommandName.ToLowerInvariant(), TriggerTTTAS);
        }

        commands.Add("rerecord", RerecordTTTAS);
    }

    public IEnumerable<string> GetPublicCommands()
    {
        yield break;
    }

    private Task TriggerTTTAS(Core.IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (!GetCanUseTTTAS(chatter.User.AuthorizationLevel))
        {
            communication.SendPublicChatMessage($"You are not authorized to use the {tttasConfig.FeatureName} System with chat commands, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        string text = string.Join(' ', remainingCommand);
        tttasHandler.HandleTTTAS(chatter.User, text, true);

        return Task.CompletedTask;
    }

    private bool GetCanUseTTTAS(AuthorizationLevel authorizationLevel)
    {
        switch (authorizationLevel)
        {
            case AuthorizationLevel.Admin: return true;
            case AuthorizationLevel.Moderator: return tttasConfig.Command.ModsCanUse;
            case AuthorizationLevel.Elevated: return tttasConfig.Command.ElevatedCanUse;
            case AuthorizationLevel.None: return tttasConfig.Command.RiffRaffCanUse;

            case AuthorizationLevel.Restricted: return false;

            default:
                communication.SendErrorMessage($"Unexpected AuthorizationLevel: {authorizationLevel}");
                return false;
        }
    }

    private Task RerecordTTTAS(Core.IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Admin)
        {
            communication.SendPublicChatMessage($"You are not authorized to initiate rerecording with the {tttasConfig.FeatureName} System, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        string text = string.Join(' ', remainingCommand);
        tttasProvider.Rerecord(text);

        return Task.CompletedTask;
    }
}
