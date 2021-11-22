
using TASagentTwitchBot.Core.Database;

namespace TASagentTwitchBot.Core.Commands;

public class CustomSimpleCommands : ICommandContainer
{
    private readonly ICommunication communication;
    private readonly IServiceScopeFactory scopeFactory;

    private Dictionary<string, CommandHandler>? commandsCache = null;

    public CustomSimpleCommands(
        ICommunication communication,
        IServiceScopeFactory scopeFactory)
    {
        this.communication = communication;
        this.scopeFactory = scopeFactory;
    }

    public void RegisterCommands(
        Dictionary<string, CommandHandler> commands,
        Dictionary<string, HelpFunction> helpFunctions,
        Dictionary<string, SetFunction> setFunctions,
        Dictionary<string, GetFunction> getFunctions)
    {
        commands.Add("add", AddCommand);
        commands.Add("remove", RemoveCommand);
        commands.Add("edit", EditCommand);
        commands.Add("enable", async (chatter, remainingCommand) => await SetCommandState(chatter, remainingCommand, true));
        commands.Add("disable", async (chatter, remainingCommand) => await SetCommandState(chatter, remainingCommand, false));

        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();


        foreach (CustomTextCommand customTextCommand in db.CustomTextCommands.Where(x => x.Enabled))
        {
            commands.Add(customTextCommand.Command, async (chatter, remainingCommand) => await HandleCommand(customTextCommand.Text));
        }

        //We cache a reference to the commands dictionary to add to it later:
        commandsCache = commands;
    }

    public IEnumerable<string> GetPublicCommands()
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        foreach (CustomTextCommand customTextCommand in db.CustomTextCommands.Where(x => x.Enabled))
        {
            yield return customTextCommand.Command;
        }
    }

    private Task HandleCommand(string output)
    {
        communication.SendPublicChatMessage(output);

        return Task.CompletedTask;
    }

    private async Task AddCommand(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Only Admins and Mods can Add commands
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return;
        }

        if (remainingCommand is null || remainingCommand.Length < 2)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Add requires a command followed by text.");
            return;
        }

        string command = remainingCommand[0].ToLowerInvariant();
        string message = string.Join(' ', remainingCommand[1..]).Trim();

        if (command.StartsWith('!'))
        {
            //Remove leading Bang
            command = command[1..];
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        if (db.CustomTextCommands.Any(x => x.Command == command))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot add command !{command} - already exists.");
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot add empty message for a command.");
            return;
        }

        if (commandsCache!.ContainsKey(command))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot add command !{command} - already exists.");
            return;
        }

        CustomTextCommand newTextCommand = new CustomTextCommand()
        {
            Command = command,
            Text = message,
            Enabled = true
        };

        db.CustomTextCommands.Add(newTextCommand);

        commandsCache.Add(command, async (chatter, remainingCommand) => await HandleCommand(message));

        await db.SaveChangesAsync();

        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, added !{command} command.");
    }

    private async Task RemoveCommand(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Only Admins and Mods can Add commands
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return;
        }

        if (remainingCommand is null || remainingCommand.Length != 1)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Remove requires a command.");
            return;
        }

        string command = remainingCommand[0].ToLowerInvariant();

        if (command.StartsWith('!'))
        {
            //Remove leading Bang
            command = command[1..];
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        CustomTextCommand? removedTextCommand = db.CustomTextCommands.Where(x => x.Command == command).FirstOrDefault();

        if (removedTextCommand is null)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot remove command !{command} - does not exists.");
            return;
        }

        db.CustomTextCommands.Remove(removedTextCommand);

        commandsCache!.Remove(command);

        await db.SaveChangesAsync();

        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Removed !{command} command.");
    }

    private async Task SetCommandState(IRC.TwitchChatter chatter, string[] remainingCommand, bool enabled)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Only Admins and Mods can Add commands
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return;
        }

        if (remainingCommand is null || remainingCommand.Length != 1)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, {(enabled ? "Enable" : "Disable")} requires a command.");
            return;
        }

        string command = remainingCommand[0].ToLowerInvariant();

        if (command.StartsWith('!'))
        {
            //Remove leading Bang
            command = command[1..];
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        CustomTextCommand? editedTextCommand = db.CustomTextCommands.Where(x => x.Command == command).FirstOrDefault();

        if (editedTextCommand is null)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot {(enabled ? "enable" : "disable")} command !{command} - does not exists.");
            return;
        }

        if (editedTextCommand.Enabled != enabled)
        {
            editedTextCommand.Enabled = enabled;
            await db.SaveChangesAsync();

            if (enabled)
            {
                commandsCache!.Add(command, async (chatter, remainingCommand) => await HandleCommand(editedTextCommand.Text));
            }
            else
            {
                commandsCache!.Remove(command);
            }

            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, {(enabled ? "Enabled" : "Disabled")} !{command} command.");
        }
        else
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, !{command} command already {(enabled ? "enabled" : "disabled")}.");
        }
    }

    private async Task EditCommand(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Only Admins and Mods can Add commands
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return;
        }

        if (remainingCommand is null || remainingCommand.Length < 2)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Edit requires a command followed by text.");
            return;
        }

        string command = remainingCommand[0].ToLowerInvariant();
        string message = string.Join(' ', remainingCommand[1..]).Trim();

        if (command.StartsWith('!'))
        {
            //Remove leading Bang
            command = command[1..];
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        CustomTextCommand? editedTextCommand = db.CustomTextCommands.Where(x => x.Command == command).FirstOrDefault();

        if (editedTextCommand is null)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot Edit command !{command} - does not exists.");
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot set empty message for a command.");
            return;
        }

        editedTextCommand.Text = message;
        await db.SaveChangesAsync();


        if (editedTextCommand.Enabled)
        {
            commandsCache![command] = async (chatter, remainingCommand) => await HandleCommand(editedTextCommand.Text);
        }

        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Edited !{command} command.");
    }
}
