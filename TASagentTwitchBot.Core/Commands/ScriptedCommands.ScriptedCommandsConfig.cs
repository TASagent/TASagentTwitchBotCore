using System.Text.Json;
using System.Text.Json.Serialization;

using BGC.Scripting;
using TASagentTwitchBot.Core.Scripting;

namespace TASagentTwitchBot.Core.Commands;

public partial class ScriptedCommands
{
    public class ScriptedCommandsConfig
    {
        private static string ConfigFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "ScriptedCommandsConfig.json");
        private static readonly object _lock = new object();

        public List<ScriptedCommand> ScriptedCommands { get; init; } = new List<ScriptedCommand>();
        
        public static ScriptedCommandsConfig GetConfig()
        {
            ScriptedCommandsConfig config;
            if (File.Exists(ConfigFilePath))
            {
                //Load existing config
                config = JsonSerializer.Deserialize<ScriptedCommandsConfig>(File.ReadAllText(ConfigFilePath))!;
            }
            else
            {
                config = new ScriptedCommandsConfig();
            }

            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config));

            return config;
        }

        public void Serialize()
        {
            lock (_lock)
            {
                File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(this));
            }
        }

        public class ScriptedCommand
        {
            public string ScriptName { get; init; } = "";
            public bool Enabled { get; set; } = true;
            public bool Shown { get; set; } = true;
            public string ScriptText { get; set; } = DEFAULT_SCRIPT;

            [JsonIgnore]
            public Script? Script { get; set; } = null;
            [JsonIgnore]
            public ScriptRuntimeContext? ScriptContext { get; set; } = null;


            public void SetScriptText(string scriptText, GlobalRuntimeContext globalContext)
            {
                Script newScript = ScriptParser.LexAndParseScript(scriptText, commandFunctions);

                ScriptText = scriptText;
                Script = newScript;
                ScriptContext = newScript.PrepareScript(globalContext);
            }

            public Task Execute(ScriptingUser user, List<string> remainingCommand) =>
                Script!.ExecuteFunctionAsync("HandleMessage", 2_000, ScriptContext!, user, remainingCommand);
        }

        public static readonly FunctionSignature[] commandFunctions = new FunctionSignature[] {
            new FunctionSignature("HandleMessage", typeof(void),
                new VariableData("user", typeof(ScriptingUser)),
                new VariableData("remainingCommand", typeof(List<string>)))};

        public const string DEFAULT_SCRIPT = @"//Default Command Script
extern ICommunication communication;

void HandleMessage(User user, List<string> remainingCommand)
{
    communication.SendPublicChatMessage($""@{user.TwitchUserName}, Hello World"");
}";

    }
}
