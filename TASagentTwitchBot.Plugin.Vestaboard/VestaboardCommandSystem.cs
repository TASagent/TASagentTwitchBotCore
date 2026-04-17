using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Commands;

namespace TASagentTwitchBot.Plugin.Vestaboard;

public class VestaboardCommandSystem : ICommandContainer
{
    private readonly ICommunication communication;
    private readonly Core.Credit.ICreditManager creditManager;
    private readonly VestaboardManager vestaboardManager;

    private readonly VestaboardConfiguration vestaboardConfig;

    public VestaboardCommandSystem(
        ICommunication communication,
        Core.Credit.ICreditManager creditManager,
        VestaboardManager vestaboardManager,
        VestaboardConfiguration vestaboardConfig)
    {
        this.communication = communication;
        this.creditManager = creditManager;
        this.vestaboardManager = vestaboardManager;
        this.vestaboardConfig = vestaboardConfig;
    }

    public void RegisterCommands(ICommandRegistrar commandRegistrar)
    {
        if (vestaboardConfig.Command.Enabled)
        {
            commandRegistrar.RegisterGlobalCommand(vestaboardConfig.Command.CommandName.ToLowerInvariant(), TriggerVestaboard);
        }

    }

    public IEnumerable<string> GetPublicCommands()
    {
        yield break;
    }

    private async Task TriggerVestaboard(Core.IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            if (vestaboardConfig.Command.AllowCreditRedemptions && creditManager.IsEnabled)
            {
                long credits = await creditManager.GetCredits(chatter.User, vestaboardConfig.Command.CreditName);
                long redemptions = credits / vestaboardConfig.Command.CreditCost;

                if (redemptions > 0)
                {
                    communication.SendPublicChatMessage(
                        $"@{chatter.User.TwitchUserName}, the Vestaboard System is active.  " +
                        $"You have {credits:N0} {vestaboardConfig.Command.CreditName} credits - enough for {redemptions:N0} redemptions.");
                }
                else
                {
                    communication.SendPublicChatMessage(
                        $"@{chatter.User.TwitchUserName}, the Vestaboard System is active.  " +
                        $"You have {credits:N0} {vestaboardConfig.Command.CreditName} credits - not enough for any redemptions.");
                }
            }
            else
            {
                communication.SendPublicChatMessage(
                    $"@{chatter.User.TwitchUserName}, the Vestaboard System is not active.");
            }
            return;
        }

        if (!await GetCanUseVestaboard(chatter.User))
        {
            if (vestaboardConfig.Command.AllowCreditRedemptions && creditManager.IsEnabled)
            {
                long credits = await creditManager.GetCredits(chatter.User, vestaboardConfig.Command.CreditName);

                communication.SendPublicChatMessage(
                        $"@{chatter.User.TwitchUserName}, you only have {credits:N0} / {vestaboardConfig.Command.CreditCost:N0} {vestaboardConfig.Command.CreditName} credits.");
            }
            else
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, you are not authorized to use the Vestaboard System with chat commands.");
            }
            return;
        }

        vestaboardManager.ImmediateSend(string.Join(' ', remainingCommand));

        return;
    }

    private async Task<bool> GetCanUseVestaboard(Core.Database.User user)
    {
        switch (user.AuthorizationLevel)
        {
            case AuthorizationLevel.Admin: return true;

            case AuthorizationLevel.Moderator:
                return vestaboardConfig.Command.ModsCanUse || (vestaboardConfig.Command.AllowCreditRedemptions && await TryDebit(user));
            case AuthorizationLevel.Elevated:
                return vestaboardConfig.Command.ElevatedCanUse || (vestaboardConfig.Command.AllowCreditRedemptions && await TryDebit(user));
            case AuthorizationLevel.None:
                return vestaboardConfig.Command.RiffRaffCanUse || (vestaboardConfig.Command.AllowCreditRedemptions && await TryDebit(user));

            case AuthorizationLevel.Restricted: return false;

            default:
                communication.SendErrorMessage($"Unexpected AuthorizationLevel: {user.AuthorizationLevel}");
                return false;
        }
    }

    private Task<bool> TryDebit(Core.Database.User user) =>
        creditManager.TryDebit(user, vestaboardConfig.Command.CreditName, vestaboardConfig.Command.CreditCost);
}
