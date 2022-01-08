using Microsoft.EntityFrameworkCore;
using TASagentTwitchBot.Core.Commands;

namespace TASagentTwitchBot.Core.Credit;

public class BasicCreditCommandSystem : ICommandContainer
{
    private readonly ICommunication communication;
    private readonly ICreditManager creditManager;
    private readonly IServiceScopeFactory scopeFactory;

    public BasicCreditCommandSystem(
        ICommunication communication,
        ICreditManager creditManager,
        IServiceScopeFactory scopeFactory)
    {
        this.communication = communication;
        this.creditManager = creditManager;
        this.scopeFactory = scopeFactory;
    }

    void ICommandContainer.RegisterCommands(ICommandRegistrar commandRegistrar)
    {
        //Skip adding commands if the credit system is disabled
        if (!creditManager.IsEnabled)
        {
            return;
        }

        commandRegistrar.RegisterGlobalCommand("getcredits", GetCreditsHandler);
        commandRegistrar.RegisterScopedCommand("get", "credits", GetCreditsHandler);
        commandRegistrar.RegisterScopedCommand("get", "creditreport", GetCreditsHandler);
        commandRegistrar.RegisterScopedCommand("credits", "get", GetCreditsHandler);

        commandRegistrar.RegisterGlobalCommand("setcredits", SetCreditsHandler);
        commandRegistrar.RegisterScopedCommand("set", "credits", SetCreditsHandler);
        commandRegistrar.RegisterScopedCommand("credits", "set", SetCreditsHandler);

        commandRegistrar.RegisterGlobalCommand("givecredits", GiveCreditsHandler);
        commandRegistrar.RegisterScopedCommand("give", "credits", GiveCreditsHandler);
        commandRegistrar.RegisterScopedCommand("credits", "give", GiveCreditsHandler);
    }

    IEnumerable<string> ICommandContainer.GetPublicCommands()
    {
        if (!creditManager.IsEnabled)
        {
            yield break;
        }

        yield return "get credits";
    }

    private async Task GetCreditsHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            //Someone is requesting their own credits
            communication.SendPublicChatMessage(
                $"@{chatter.User.TwitchUserName}, your credit report: {await GetCreditReport(chatter.User)}");
            return;
        }

        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            communication.SendPublicChatMessage($"You are not authorized to check another user's credits, @{chatter.User.TwitchUserName}.");
            return;
        }

        if (remainingCommand.Length != 1 || remainingCommand[0].Length < 2)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, malformed \"Get Credits\" command. Expected: \"!get credits\" or \"!get credits @username\"");
        }

        //Try to find other user
        string userName = remainingCommand[0];

        //Strip off optional leading @
        if (userName.StartsWith('@'))
        {
            userName = userName[1..].ToLower();
        }

        string lowerUserName = userName.ToLower();
        using IServiceScope scope = scopeFactory.CreateScope();
        Database.BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<Database.BaseDatabaseContext>();
        Database.User? dbUser = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserName.ToLower() == lowerUserName);

        if (dbUser is null)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no user named \"{userName}\" found.");
            return;
        }

        communication.SendPublicChatMessage(
            $"@{chatter.User.TwitchUserName}, the credit report of @{dbUser.TwitchUserName}: {await GetCreditReport(dbUser)}");
    }

    private async Task SetCreditsHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand is null || remainingCommand.Length != 3)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, malformed \"Set Credits\" command. Expected: \"!set credits TYPE @USERNAME VALUE\"");
            return;
        }

        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Admin)
        {
            communication.SendPublicChatMessage($"You are not authorized to adjust another user's credits, @{chatter.User.TwitchUserName}.");
            return;
        }

        //Try to find other user
        string creditType = remainingCommand[0];
        string userName = remainingCommand[1];

        //Strip off optional leading @
        if (userName.StartsWith('@'))
        {
            userName = userName[1..].ToLower();
        }

        if (userName.Length < 3)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, malformed \"Set Credits\" command. Expected: \"!set credits TYPE @USERNAME VALUE\": Invalid Username.");
            return;
        }

        if (!long.TryParse(remainingCommand[2], out long newCreditValue))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, malformed \"Set Credits\" command. Expected: \"!set credits TYPE @USERNAME VALUE\": Unable to parse Value.");
            return;
        }


        string lowerUserName = userName.ToLower();
        using IServiceScope scope = scopeFactory.CreateScope();
        Database.BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<Database.BaseDatabaseContext>();
        Database.User? dbUser = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserName.ToLower() == lowerUserName);

        if (dbUser is null)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no user named \"{userName}\" found.");
            return;
        }

        long oldCreditValue = await creditManager.GetCredits(dbUser, creditType);

        if (oldCreditValue == newCreditValue)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user \"{userName}\" already had {oldCreditValue:N0} {creditType} credits.");
            return;
        }

        await creditManager.SetCredits(dbUser, creditType, newCreditValue);

        communication.SendPublicChatMessage(
            $"@{chatter.User.TwitchUserName}, the {creditType} credits of @{dbUser.TwitchUserName} have been updated from {oldCreditValue:N0} to {newCreditValue:N0}");
    }

    private async Task GiveCreditsHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand is null || remainingCommand.Length != 3)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, malformed \"Give Credits\" command. Expected: \"!give credits TYPE @USERNAME VALUE\"");
            return;
        }

        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Admin)
        {
            communication.SendPublicChatMessage($"You are not authorized to adjust another user's credits, @{chatter.User.TwitchUserName}.");
            return;
        }

        //Try to find other user
        string creditType = remainingCommand[0];
        string userName = remainingCommand[1];

        //Strip off optional leading @
        if (userName.StartsWith('@'))
        {
            userName = userName[1..].ToLower();
        }

        if (userName.Length < 3)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, malformed \"Give Credits\" command. Expected: \"!give credits TYPE @USERNAME VALUE\": Invalid Username.");
            return;
        }

        if (!long.TryParse(remainingCommand[2], out long deltaCredits))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, malformed \"Give Credits\" command. Expected: \"!give credits TYPE @USERNAME VALUE\": Unable to parse Value.");
            return;
        }

        string lowerUserName = userName.ToLower();
        using IServiceScope scope = scopeFactory.CreateScope();
        Database.BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<Database.BaseDatabaseContext>();
        Database.User? dbUser = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserName.ToLower() == lowerUserName);

        if (dbUser is null)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no user named \"{userName}\" found.");
            return;
        }

        if (deltaCredits == 0)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Cannot adjust user \"{userName}\" {creditType} credits by 0.");
            return;
        }

        long oldCreditValue = await creditManager.GetCredits(dbUser, creditType);

        await creditManager.AdjustCredits(dbUser, creditType, deltaCredits);

        communication.SendPublicChatMessage(
            $"@{chatter.User.TwitchUserName}, the {creditType} credits of @{dbUser.TwitchUserName} have been updated from {oldCreditValue:N0} to {oldCreditValue + deltaCredits:N0}");
    }

    private async Task<string> GetCreditReport(Database.User user)
    {
        if (user is null)
        {
            return string.Empty;
        }

        IEnumerable<(string creditType, long value)> credits = await creditManager.GetAllCredits(user);

        return string.Join(", ", credits.Select(x => $"{x.creditType}: {x.value:N0}"));
    }
}
