using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Commands;

namespace TASagentTwitchBot.Plugin.TTTAS;

public class TTTASCommandSystem : ICommandContainer
{
    private readonly ICommunication communication;
    private readonly Core.Credit.ICreditManager creditManager;
    private readonly ITTTASHandler tttasHandler;
    private readonly ITTTASProvider tttasProvider;

    private readonly TTTASConfiguration tttasConfig;

    public TTTASCommandSystem(
        ICommunication communication,
        Core.Credit.ICreditManager creditManager,
        ITTTASHandler tttasHandler,
        ITTTASProvider tttasProvider,
        TTTASConfiguration tttasConfig)
    {
        this.communication = communication;
        this.creditManager = creditManager;
        this.tttasHandler = tttasHandler;
        this.tttasProvider = tttasProvider;
        this.tttasConfig = tttasConfig;
    }

    public void RegisterCommands(ICommandRegistrar commandRegistrar)
    {
        if (tttasConfig.Command.Enabled)
        {
            commandRegistrar.RegisterGlobalCommand(tttasConfig.Command.CommandName.ToLowerInvariant(), TriggerTTTAS);
        }

        commandRegistrar.RegisterGlobalCommand("rerecord", RerecordTTTAS);
    }

    public IEnumerable<string> GetPublicCommands()
    {
        yield break;
    }

    private async Task TriggerTTTAS(Core.IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            if (tttasConfig.Command.AllowCreditRedemptions && creditManager.IsEnabled)
            {
                long credits = await creditManager.GetCredits(chatter.User, tttasConfig.Command.CreditName);
                long redemptions = credits / tttasConfig.Command.CreditCost;

                if (redemptions > 0)
                {
                    communication.SendPublicChatMessage(
                        $"@{chatter.User.TwitchUserName}, the {tttasConfig.FeatureName} System " +
                        $"has {tttasProvider.GetRecordingCount():N0} recordings in total. " +
                        $"You have {credits:N0} {tttasConfig.Command.CreditName} credits - enough for {redemptions:N0} redemptions.");
                }
                else
                {
                    communication.SendPublicChatMessage(
                        $"@{chatter.User.TwitchUserName}, the {tttasConfig.FeatureName} System " +
                        $"has {tttasProvider.GetRecordingCount():N0} recordings in total. " +
                        $"You have {credits:N0} {tttasConfig.Command.CreditName} credits - not enough for any redemptions.");
                }
            }
            else
            {
                communication.SendPublicChatMessage(
                    $"@{chatter.User.TwitchUserName}, the {tttasConfig.FeatureName} System has {tttasProvider.GetRecordingCount():N0} recordings in total.");
            }
            return;
        }

        if (!await GetCanUseTTTAS(chatter.User))
        {
            if (tttasConfig.Command.AllowCreditRedemptions && creditManager.IsEnabled)
            {
                long credits = await creditManager.GetCredits(chatter.User, tttasConfig.Command.CreditName);

                communication.SendPublicChatMessage(
                        $"@{chatter.User.TwitchUserName}, you only have {credits:N0} / {tttasConfig.Command.CreditCost:N0} {tttasConfig.Command.CreditName} credits.");
            }
            else
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, you are not authorized to use the {tttasConfig.FeatureName} System with chat commands.");
            }
            return;
        }

        tttasHandler.HandleTTTAS(
            user: chatter.User,
            message: string.Join(' ', remainingCommand),
            approved: true);

        return;
    }

    private async Task<bool> GetCanUseTTTAS(Core.Database.User user)
    {
        switch (user.AuthorizationLevel)
        {
            case AuthorizationLevel.Admin: return true;

            case AuthorizationLevel.Moderator:
                return tttasConfig.Command.ModsCanUse || (tttasConfig.Command.AllowCreditRedemptions && await TryDebit(user));
            case AuthorizationLevel.Elevated:
                return tttasConfig.Command.ElevatedCanUse || (tttasConfig.Command.AllowCreditRedemptions && await TryDebit(user));
            case AuthorizationLevel.None:
                return tttasConfig.Command.RiffRaffCanUse || (tttasConfig.Command.AllowCreditRedemptions && await TryDebit(user));

            case AuthorizationLevel.Restricted: return false;

            default:
                communication.SendErrorMessage($"Unexpected AuthorizationLevel: {user.AuthorizationLevel}");
                return false;
        }
    }

    private Task<bool> TryDebit(Core.Database.User user) =>
        creditManager.TryDebit(user, tttasConfig.Command.CreditName, tttasConfig.Command.CreditCost);

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
