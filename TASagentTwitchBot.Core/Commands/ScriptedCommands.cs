using System.Text;
using System.Text.RegularExpressions;

using BGC.Scripting;
using BGC.Scripting.Parsing;
using TASagentTwitchBot.Core.Database;
using TASagentTwitchBot.Core.Scripting;

namespace TASagentTwitchBot.Core.Commands;

public partial class ScriptedCommands : ICommandContainer, IScriptedComponent
{
    private readonly ICommunication communication;

    private ICommandRegistrar? commandRegistrar = null;
    private IScriptRegistrar? scriptRegistrar = null;

    private readonly ScriptedCommandsConfig scriptedCommandsConfig;

    private GlobalRuntimeContext? globalRuntimeContext = null;

    public ScriptedCommands(
        ScriptedCommandsConfig scriptedCommandsConfig,
        ICommunication communication)
    {
        this.scriptedCommandsConfig = scriptedCommandsConfig;
        this.communication = communication;
    }

    public static void RegisterRequiredScriptingClasses()
    {
        ClassRegistrar.TryRegisterClass<ScriptingUser>("User");
        ClassRegistrar.TryRegisterClass<MessageData>();
    }

    public void RegisterCommands(ICommandRegistrar commandRegistrar)
    {
        commandRegistrar.RegisterGlobalCommand("addscript", AddScript);
        commandRegistrar.RegisterScopedCommand("add", "script", AddScript);
        commandRegistrar.RegisterScopedCommand("script", "add", AddScript);
        commandRegistrar.RegisterGlobalCommand("createscript", AddScript);
        commandRegistrar.RegisterScopedCommand("create", "script", AddScript);
        commandRegistrar.RegisterScopedCommand("script", "create", AddScript);

        commandRegistrar.RegisterGlobalCommand("removescript", RemoveScript);
        commandRegistrar.RegisterScopedCommand("remove", "script", RemoveScript);
        commandRegistrar.RegisterScopedCommand("script", "remove", RemoveScript);
        commandRegistrar.RegisterGlobalCommand("deletescript", RemoveScript);
        commandRegistrar.RegisterScopedCommand("delete", "script", RemoveScript);
        commandRegistrar.RegisterScopedCommand("script", "delete", RemoveScript);

        commandRegistrar.RegisterScopedCommand("set", "script", SetScript);
        commandRegistrar.RegisterScopedCommand("script", "set", SetScript);

        commandRegistrar.RegisterGlobalCommand("enablescript", (chatter, remainingCommand) => SetScriptState(chatter, remainingCommand, true));
        commandRegistrar.RegisterScopedCommand("enable", "script", (chatter, remainingCommand) => SetScriptState(chatter, remainingCommand, true));
        commandRegistrar.RegisterScopedCommand("script", "enable", (chatter, remainingCommand) => SetScriptState(chatter, remainingCommand, true));

        commandRegistrar.RegisterGlobalCommand("disablescript", (chatter, remainingCommand) => SetScriptState(chatter, remainingCommand, false));
        commandRegistrar.RegisterScopedCommand("disable", "script", (chatter, remainingCommand) => SetScriptState(chatter, remainingCommand, false));
        commandRegistrar.RegisterScopedCommand("script", "disable", (chatter, remainingCommand) => SetScriptState(chatter, remainingCommand, false));

        foreach (ScriptedCommandsConfig.ScriptedCommand scriptedCommand in scriptedCommandsConfig.ScriptedCommands.Where(x => x.Enabled))
        {
            commandRegistrar.RegisterCustomCommand(scriptedCommand.ScriptName, (chatter, remainingCommand) => HandleScript(chatter, remainingCommand, scriptedCommand.ScriptName));
        }

        //We cache a reference to the commandRegistrar to add commands later
        this.commandRegistrar = commandRegistrar;
    }

    void IScriptedComponent.Initialize(IScriptRegistrar scriptRegistrar)
    {
        this.scriptRegistrar = scriptRegistrar;

        globalRuntimeContext = scriptRegistrar.GlobalSharedRuntimeContext;

        foreach (ScriptedCommandsConfig.ScriptedCommand? script in scriptedCommandsConfig.ScriptedCommands)
        {
            try
            {
                script.SetScriptText(script.ScriptText, globalRuntimeContext!);
            }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"Unable to compile Script {script.ScriptName}: {ex.Message}");
                script.Enabled = false;
            }
        }
    }

    public IEnumerable<string> GetScriptNames() => scriptedCommandsConfig.ScriptedCommands.Select(x => $"!{x.ScriptName}");

    public string? GetScript(string scriptName)
    {
        if (scriptName.StartsWith("!"))
        {
            scriptName = scriptName[1..];
        }

        ScriptedCommandsConfig.ScriptedCommand? script = scriptedCommandsConfig.ScriptedCommands
            .FirstOrDefault(x => string.Equals(x.ScriptName, scriptName, StringComparison.OrdinalIgnoreCase));

        if (script is null)
        {
            communication.SendErrorMessage($"Unexpected ScriptName: {scriptName}.");
            return null;
        }

        return script.ScriptText;
    }

    public string? GetDefaultScript(string scriptName) => ScriptedCommandsConfig.DEFAULT_SCRIPT;

    public bool SetScript(string scriptName, string scriptText)
    {
        if (scriptName.StartsWith("!"))
        {
            scriptName = scriptName[1..];
        }

        ScriptedCommandsConfig.ScriptedCommand? script = scriptedCommandsConfig.ScriptedCommands
            .FirstOrDefault(x => string.Equals(x.ScriptName, scriptName, StringComparison.OrdinalIgnoreCase));

        if (script is null)
        {
            communication.SendErrorMessage($"Unexpected ScriptName: {scriptName}.");
            return false;
        }

        try
        {
            script.SetScriptText(scriptText, globalRuntimeContext!);
            scriptedCommandsConfig.Serialize();
            return true;
        }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"Failed to parse {scriptName} script: {ex.Message}");
            return false;
        }
    }

    public IEnumerable<string> GetPublicCommands()
    {
        foreach (ScriptedCommandsConfig.ScriptedCommand scriptedCommand in scriptedCommandsConfig.ScriptedCommands.Where(x => x.Enabled))
        {
            yield return scriptedCommand.ScriptName;
        }
    }

    private async Task HandleScript(IRC.TwitchChatter chatter, string[] remainingCommand, string scriptName)
    {
        if (scriptName.StartsWith("!"))
        {
            scriptName = scriptName[1..];
        }

        ScriptedCommandsConfig.ScriptedCommand? script = scriptedCommandsConfig.ScriptedCommands
            .FirstOrDefault(x => string.Equals(x.ScriptName, scriptName, StringComparison.OrdinalIgnoreCase));

        if (script is null)
        {
            communication.SendErrorMessage($"Unexpected ScriptName: {scriptName}.");
            return;
        }

        try
        {
            MessageData data = await script.Execute(
                user: ScriptingUser.FromDB(chatter.User),
                remainingCommand: remainingCommand.ToList());

            if (!string.IsNullOrEmpty(data.ChatMessage))
            {
                communication.SendPublicChatMessage(data.ChatMessage);
            }
        }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"Script {scriptName} encountered an error: {ex.Message}.");
        }
    }

    private Task AddScript(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Only Admins and Mods can Add scripts
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (remainingCommand is null || remainingCommand.Length != 1)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, \"Add Script\" requires just a script name.");
            return Task.CompletedTask;
        }

        string scriptName = remainingCommand[0].ToLower();

        if (scriptName.StartsWith('!'))
        {
            //Remove leading Bang
            scriptName = scriptName[1..];
        }

        if (scriptedCommandsConfig.ScriptedCommands.Any(x => string.Equals(x.ScriptName, scriptName, StringComparison.OrdinalIgnoreCase)))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot add script !{scriptName} - already exists.");
            return Task.CompletedTask;
        }

        if (commandRegistrar!.ContainsGlobalCommand(scriptName))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot add script !{scriptName} - already exists as a global command.");
            return Task.CompletedTask;
        }

        if (commandRegistrar!.ContainsCustomCommand(scriptName))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot add script !{scriptName} - already exists as an enabled custom command.");
            return Task.CompletedTask;
        }

        ScriptedCommandsConfig.ScriptedCommand newScriptedCommand = new ScriptedCommandsConfig.ScriptedCommand()
        {
            ScriptName = scriptName,
            Enabled = true
        };

        newScriptedCommand.SetScriptText(ScriptedCommandsConfig.DEFAULT_SCRIPT, globalRuntimeContext!);

        scriptedCommandsConfig.ScriptedCommands.Add(newScriptedCommand);

        commandRegistrar.RegisterCustomCommand(scriptName, (chatter, remainingCommand) => HandleScript(chatter, remainingCommand, scriptName));
        scriptRegistrar!.RegisterNewScript(this, $"!{scriptName}");

        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, added !{scriptName} script.");

        scriptedCommandsConfig.Serialize();

        return Task.CompletedTask;
    }

    private Task RemoveScript(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Only Admins and Mods can remove scripts
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (remainingCommand is null || remainingCommand.Length != 1)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, \"Remove Script\" requires a script name.");
            return Task.CompletedTask;
        }

        string scriptName = remainingCommand[0].ToLowerInvariant();

        if (scriptName.StartsWith('!'))
        {
            //Remove leading Bang
            scriptName = scriptName[1..];
        }

        ScriptedCommandsConfig.ScriptedCommand? removedScript = scriptedCommandsConfig.ScriptedCommands.Where(x => string.Equals(x.ScriptName, scriptName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

        if (removedScript is null)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot remove script !{scriptName} - does not exists.");
            return Task.CompletedTask;
        }

        scriptedCommandsConfig.ScriptedCommands.Remove(removedScript);

        commandRegistrar!.RemoveCustomCommand(scriptName);
        scriptRegistrar!.UnregisterScript($"!{scriptName}");

        scriptedCommandsConfig.Serialize();

        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Removed !{scriptName} script.");

        return Task.CompletedTask;
    }

    private Task SetScript(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Only Admins and Mods can edit scripts
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (remainingCommand is null || remainingCommand.Length != 2)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, \"Set Script\" requires a valid state (\"Enabled\" or \"Disabled\") and a script name.");
            return Task.CompletedTask;
        }

        string state = remainingCommand[0].ToLowerInvariant();
        string scriptName = remainingCommand[1].ToLowerInvariant();
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
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, \"Set Script\" requires a valid state (\"Enabled\" or \"Disabled\") and a script name.");
                return Task.CompletedTask;
        }

        return SetScriptState(chatter, scriptName, enabled);
    }


    private Task SetScriptState(IRC.TwitchChatter chatter, string[] remainingCommand, bool enabled)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Only Admins and Mods can edit scripts
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (remainingCommand is null || remainingCommand.Length != 1)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, {(enabled ? "Enable Script" : "Disable Script")} requires a script name.");
            return Task.CompletedTask;
        }

        return SetScriptState(chatter, remainingCommand[0].ToLowerInvariant(), enabled);
    }

    private Task SetScriptState(IRC.TwitchChatter chatter, string scriptName, bool enabled)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            //Only Admins and Mods can edit scripts
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (scriptName.StartsWith('!'))
        {
            //Remove leading Bang
            scriptName = scriptName[1..];
        }

        ScriptedCommandsConfig.ScriptedCommand? editedScript = scriptedCommandsConfig.ScriptedCommands.Where(x => string.Equals(x.ScriptName, scriptName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

        if (editedScript is null)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, cannot {(enabled ? "enable" : "disable")} custom script !{scriptName} - does not exists.");
            return Task.CompletedTask;
        }

        if (editedScript.Enabled != enabled)
        {
            if (editedScript.Script is null)
            {
                //Compile it first - to make sure it wasn't disabled

                try
                {
                    editedScript.SetScriptText(editedScript.ScriptText, globalRuntimeContext!);
                }
                catch (Exception ex)
                {
                    communication.SendErrorMessage($"Failed to parse {scriptName} script: {ex.Message}");
                    return Task.CompletedTask;
                }
            }


            editedScript.Enabled = enabled;
            scriptedCommandsConfig.Serialize();

            if (enabled)
            {
                commandRegistrar!.RegisterCustomCommand(scriptName, (chatter, remainingCommand) => HandleScript(chatter, remainingCommand, editedScript.ScriptName));
            }
            else
            {
                commandRegistrar!.RemoveCustomCommand(scriptName);
            }

            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, {(enabled ? "Enabled" : "Disabled")} !{scriptName} script.");
        }
        else
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, !{scriptName} script already {(enabled ? "enabled" : "disabled")}.");
        }

        return Task.CompletedTask;
    }

    public class MessageData
    {
        [ScriptingAccess]
        public string ChatMessage { get; set; } = "";

        public MessageData()
        {

        }

        public MessageData(string message)
        {
            ChatMessage = message;
        }
    }
}
