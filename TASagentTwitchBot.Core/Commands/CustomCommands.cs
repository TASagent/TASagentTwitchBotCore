using System.Text;
using System.Text.RegularExpressions;

using TASagentTwitchBot.Core.Database;

namespace TASagentTwitchBot.Core.Commands;

public class CustomCommands : ICommandContainer
{
    private readonly ICommunication communication;
    private readonly IServiceScopeFactory scopeFactory;

    private ICommandRegistrar? commandRegistrar = null;

    public CustomCommands(
        ICommunication communication,
        IServiceScopeFactory scopeFactory)
    {
        this.communication = communication;
        this.scopeFactory = scopeFactory;
    }

    public void RegisterCommands(ICommandRegistrar commandRegistrar)
    {
        commandRegistrar.RegisterGlobalCommand("addcommand", AddCommand);
        commandRegistrar.RegisterScopedCommand("add", "command", AddCommand);
        commandRegistrar.RegisterScopedCommand("command", "add", AddCommand);
        commandRegistrar.RegisterGlobalCommand("createcommand", AddCommand);
        commandRegistrar.RegisterScopedCommand("create", "command", AddCommand);
        commandRegistrar.RegisterScopedCommand("command", "create", AddCommand);

        commandRegistrar.RegisterGlobalCommand("removecommand", RemoveCommand);
        commandRegistrar.RegisterScopedCommand("remove", "command", RemoveCommand);
        commandRegistrar.RegisterScopedCommand("command", "remove", RemoveCommand);
        commandRegistrar.RegisterGlobalCommand("deletecommand", RemoveCommand);
        commandRegistrar.RegisterScopedCommand("delete", "command", RemoveCommand);
        commandRegistrar.RegisterScopedCommand("command", "delete", RemoveCommand);

        commandRegistrar.RegisterGlobalCommand("editcommand", EditCommand);
        commandRegistrar.RegisterScopedCommand("edit", "command", EditCommand);
        commandRegistrar.RegisterScopedCommand("command", "edit", EditCommand);
        commandRegistrar.RegisterGlobalCommand("updatecommand", EditCommand);
        commandRegistrar.RegisterScopedCommand("update", "command", EditCommand);
        commandRegistrar.RegisterScopedCommand("command", "update", EditCommand);

        commandRegistrar.RegisterScopedCommand("set", "command", SetCommand);
        commandRegistrar.RegisterScopedCommand("command", "set", SetCommand);

        commandRegistrar.RegisterGlobalCommand("enablecommand", (chatter, remainingCommand) => SetCommandState(chatter, remainingCommand, true));
        commandRegistrar.RegisterScopedCommand("enable", "command", (chatter, remainingCommand) => SetCommandState(chatter, remainingCommand, true));
        commandRegistrar.RegisterScopedCommand("command", "enable", (chatter, remainingCommand) => SetCommandState(chatter, remainingCommand, true));

        commandRegistrar.RegisterGlobalCommand("disablecommand", (chatter, remainingCommand) => SetCommandState(chatter, remainingCommand, false));
        commandRegistrar.RegisterScopedCommand("disable", "command", (chatter, remainingCommand) => SetCommandState(chatter, remainingCommand, false));
        commandRegistrar.RegisterScopedCommand("command", "disable", (chatter, remainingCommand) => SetCommandState(chatter, remainingCommand, false));

        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        foreach (CustomTextCommand customTextCommand in db.CustomTextCommands.Where(x => x.Enabled))
        {
            commandRegistrar.RegisterCustomCommand(customTextCommand.Command, (chatter, remainingCommand) => HandleCommand(chatter, remainingCommand, customTextCommand.Text));
        }

        //We cache a reference to the commandRegistrar to add commands later
        this.commandRegistrar = commandRegistrar;
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

    private Task HandleCommand(IRC.TwitchChatter chatter, string[] remainingCommand, string output)
    {
        if (!output.Contains('$'))
        {
            //Simple Command
            communication.SendPublicChatMessage(output);
            return Task.CompletedTask;
        }

        output = output.Replace("${0}", chatter.User.TwitchUserName);

        for (int i = 0; i < remainingCommand.Length; i++)
        {
            output = output.Replace($"${{{i + 1}}}", remainingCommand[i]);
        }

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
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, \"Add Command\" requires a command followed by text.");
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

        if (commandRegistrar!.ContainsGlobalCommand(command))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot add command !{command} - already exists as a global command.");
            return;
        }

        if (commandRegistrar!.ContainsCustomCommand(command))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot add command !{command} - already exists as a custom command.");
            return;
        }

        CustomTextCommand newTextCommand = new CustomTextCommand()
        {
            Command = command,
            Text = message,
            Enabled = true
        };

        db.CustomTextCommands.Add(newTextCommand);

        commandRegistrar.RegisterCustomCommand(command, (chatter, remainingCommand) => HandleCommand(chatter, remainingCommand, message));

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
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, \"Remove Command\" requires a command.");
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

        commandRegistrar!.RemoveCustomCommand(command);

        await db.SaveChangesAsync();

        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Removed !{command} command.");
    }

    private Task SetCommand(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Only Admins and Mods can Add commands
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (remainingCommand is null || remainingCommand.Length != 2)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, \"Set Commmand\" requires a valid state (\"Enabled\" or \"Disabled\") and a command.");
            return Task.CompletedTask;
        }

        string state = remainingCommand[0].ToLowerInvariant();
        string command = remainingCommand[1].ToLowerInvariant();
        bool enabled;

        switch (state)
        {
            case "enable":
            case "enabled":
            case "engaged":
            case "on":
                enabled = true;
                break;

            case "disable":
            case "disabled":
            case "disengaged":
            case "off":
                enabled = false;
                break;

            default:
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, \"Set Commmand\" requires a valid state (\"Enabled\" or \"Disabled\") and a command.");
                return Task.CompletedTask;
        }

        return SetCommandState(chatter, command, enabled);
    }


    private Task SetCommandState(IRC.TwitchChatter chatter, string[] remainingCommand, bool enabled)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Only Admins and Mods can Add commands
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (remainingCommand is null || remainingCommand.Length != 1)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, {(enabled ? "Enable Command" : "Disable Command")} requires a command.");
            return Task.CompletedTask;
        }

        return SetCommandState(chatter, remainingCommand[0].ToLowerInvariant(), enabled);
    }

    private async Task SetCommandState(IRC.TwitchChatter chatter, string command, bool enabled)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Only Admins and Mods can Add commands
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return;
        }

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
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot {(enabled ? "enable" : "disable")} custom command !{command} - does not exists.");
            return;
        }

        if (editedTextCommand.Enabled != enabled)
        {
            editedTextCommand.Enabled = enabled;
            await db.SaveChangesAsync();

            if (enabled)
            {
                commandRegistrar!.RegisterCustomCommand(command, (chatter, remainingCommand) => HandleCommand(chatter, remainingCommand, editedTextCommand.Text));
            }
            else
            {
                commandRegistrar!.RemoveCustomCommand(command);
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
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, \"Edit Command\" requires a command followed by text.");
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
            commandRegistrar!.RemoveCustomCommand(command);
            commandRegistrar!.RegisterCustomCommand(command, (chatter, remainingCommand) => HandleCommand(chatter, remainingCommand, editedTextCommand.Text));
        }

        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Edited !{command} command.");
    }
}
