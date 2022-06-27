
using TASagentTwitchBot.Core.Database;

namespace TASagentTwitchBot.Core.Commands;

public class PermissionSystem : ICommandContainer
{
    private readonly ICommunication communication;
    private readonly Notifications.IActivityDispatcher activityDispatcher;
    private readonly IServiceScopeFactory scopeFactory;

    public PermissionSystem(
        ICommunication communication,
        Notifications.IActivityDispatcher activityDispatcher,
        IServiceScopeFactory scopeFactory)
    {
        this.communication = communication;
        this.activityDispatcher = activityDispatcher;
        this.scopeFactory = scopeFactory;
    }

    public void RegisterCommands(ICommandRegistrar commandRegistrar)
    {
        commandRegistrar.RegisterGlobalCommand("mod", (chatter, remainingCommand) => ModUser(chatter, remainingCommand, true));
        commandRegistrar.RegisterGlobalCommand("unmod", (chatter, remainingCommand) => ModUser(chatter, remainingCommand, false));

        commandRegistrar.RegisterGlobalCommand("permit", (chatter, remainingCommand) => AdjustUser(chatter, remainingCommand, true));
        commandRegistrar.RegisterGlobalCommand("promote", (chatter, remainingCommand) => AdjustUser(chatter, remainingCommand, true));
        commandRegistrar.RegisterGlobalCommand("elevate", (chatter, remainingCommand) => AdjustUser(chatter, remainingCommand, true));

        commandRegistrar.RegisterGlobalCommand("revoke", (chatter, remainingCommand) => AdjustUser(chatter, remainingCommand, false));
        commandRegistrar.RegisterGlobalCommand("demote", (chatter, remainingCommand) => AdjustUser(chatter, remainingCommand, false));

        commandRegistrar.RegisterGlobalCommand("restrict", RestrictUser);
    }

    public IEnumerable<string> GetPublicCommands()
    {
        yield break;
    }

    private async Task AdjustUser(IRC.TwitchChatter chatter, string[] remainingCommand, bool elevate)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Moderators and Admins can adjust users
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return;
        }

        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, {(elevate ? "Elevate" : "Demote")} who?");
            return;
        }

        string lowerUser = remainingCommand[0].ToLower();

        if (string.IsNullOrWhiteSpace(lowerUser))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, {(elevate ? "Elevate" : "Demote")} who?");
            return;
        }

        if (lowerUser.StartsWith('@') && lowerUser.Length > 1)
        {
            lowerUser = lowerUser[1..];
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        IEnumerable<User> matchingUsers = db.Users.Where(x => x.TwitchUserName.ToLower() == lowerUser);

        if (!matchingUsers.Any())
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {remainingCommand[0]} not found.");
            return;
        }

        if (matchingUsers.Count() > 1)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {remainingCommand[0]} found multiple times.");
            return;
        }

        User matchingUser = matchingUsers.First();

        if (elevate)
        {
            if (matchingUser.AuthorizationLevel == AuthorizationLevel.Restricted)
            {
                //Unrestricting a user
                matchingUser.AuthorizationLevel = AuthorizationLevel.None;
                await db.SaveChangesAsync();
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {matchingUser.TwitchUserName} no longer restricted.");
            }
            else if (matchingUser.AuthorizationLevel == AuthorizationLevel.None)
            {
                matchingUser.AuthorizationLevel = AuthorizationLevel.Elevated;
                await db.SaveChangesAsync();
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {matchingUser.TwitchUserName} has been elevated!");
                activityDispatcher.UpdateAllRequests(matchingUser.TwitchUserId, true);
            }
            else
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {matchingUser.TwitchUserName} is already fully elevated.");
            }
        }
        else
        {
            if (matchingUser.AuthorizationLevel == AuthorizationLevel.Elevated)
            {
                matchingUser.AuthorizationLevel = AuthorizationLevel.None;
                await db.SaveChangesAsync();
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {matchingUser.TwitchUserName} has been demoted!");
                activityDispatcher.UpdateAllRequests(matchingUser.TwitchUserId, false);
            }
            else if (matchingUser.AuthorizationLevel == AuthorizationLevel.None)
            {
                matchingUser.AuthorizationLevel = AuthorizationLevel.Restricted;
                await db.SaveChangesAsync();
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {matchingUser.TwitchUserName} has been restricted!");
                activityDispatcher.UpdateAllRequests(matchingUser.TwitchUserId, false);
            }
            else
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {matchingUser.TwitchUserName} can't be further demoted.");
            }
        }
    }

    private async Task ModUser(IRC.TwitchChatter chatter, string[] remainingCommand, bool mod)
    {
        if (chatter.User.AuthorizationLevel != AuthorizationLevel.Admin)
        {
            //Only Admins can mod users
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return;
        }

        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, {(mod ? "Mod" : "Unmod")} who?");
            return;
        }

        string lowerUser = remainingCommand[0].ToLower();

        if (string.IsNullOrWhiteSpace(lowerUser))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, {(mod ? "Mod" : "Unmod")} who?");
            return;
        }

        if (lowerUser.StartsWith('@') && lowerUser.Length > 1)
        {
            lowerUser = lowerUser[1..];
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        IEnumerable<User> matchingUsers = db.Users.Where(x => x.TwitchUserName.ToLower() == lowerUser);

        if (!matchingUsers.Any())
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {remainingCommand[0]} not found.");
            return;
        }

        if (matchingUsers.Count() > 1)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {remainingCommand[0]} found multiple times.");
            return;
        }

        User matchingUser = matchingUsers.First();

        if (mod)
        {
            if (matchingUser.AuthorizationLevel < AuthorizationLevel.Moderator)
            {
                matchingUser.AuthorizationLevel = AuthorizationLevel.Moderator;
                await db.SaveChangesAsync();
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {matchingUser.TwitchUserName} has been modded!");
            }
            else
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {matchingUser.TwitchUserName} is already too powerful.");
            }
        }
        else
        {
            if (matchingUser.AuthorizationLevel == AuthorizationLevel.Moderator)
            {
                matchingUser.AuthorizationLevel = AuthorizationLevel.None;
                await db.SaveChangesAsync();
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {matchingUser.TwitchUserName} has been unmodded!");
            }
            else if (matchingUser.AuthorizationLevel < AuthorizationLevel.Moderator)
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {matchingUser.TwitchUserName} is already not a mod.  Mission accomplished?");
            }
            else
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {matchingUser.TwitchUserName} cannot be demoded.");
            }
        }
    }

    private async Task RestrictUser(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Only Admins and Mods and restrict users
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return;
        }

        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Restrict who?");
            return;
        }

        string lowerUser = remainingCommand[0].ToLower();

        if (string.IsNullOrWhiteSpace(lowerUser))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Restrict who?");
            return;
        }

        if (lowerUser.StartsWith('@') && lowerUser.Length > 1)
        {
            lowerUser = lowerUser[1..];
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        IEnumerable<User> matchingUsers = db.Users.Where(x => x.TwitchUserName.ToLower() == lowerUser);

        if (!matchingUsers.Any())
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {remainingCommand[0]} not found.");
            return;
        }

        if (matchingUsers.Count() > 1)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {remainingCommand[0]} found multiple times.");
            return;
        }

        User matchingUser = matchingUsers.First();

        if (matchingUser.AuthorizationLevel == AuthorizationLevel.Admin)
        {
            //Failed - No one can restrict admins
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no one can restrict the admin!");
        }
        else if (matchingUser.AuthorizationLevel == AuthorizationLevel.Moderator)
        {
            //Attempt to restrict a mod
            if (chatter.User.AuthorizationLevel == AuthorizationLevel.Admin)
            {
                //Only admins can restrict mods
                matchingUser.AuthorizationLevel = AuthorizationLevel.Restricted;
                await db.SaveChangesAsync();
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {matchingUser.TwitchUserName} has been restricted!");
                activityDispatcher.UpdateAllRequests(matchingUser.TwitchUserId, false);
            }
            else
            {
                //Failed - Mods can't restrict other mods
                communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            }
        }
        else
        {
            //Restriction of normal user
            matchingUser.AuthorizationLevel = AuthorizationLevel.Restricted;
            await db.SaveChangesAsync();
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, user {matchingUser.TwitchUserName} has been restricted!");
            activityDispatcher.UpdateAllRequests(matchingUser.TwitchUserId, false);
        }
    }
}
