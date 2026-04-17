using System.Text.Json;
using System.Text.Json.Serialization;

namespace TASagentTwitchBot.Plugin.Vestaboard;

public class VestaboardConfiguration
{
    private static string ConfigFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "VestaboardConfig.json");
    private static readonly object _lock = new object();

    private const int CURRENT_VERSION = 2;

    public int Version { get; private set; } = 0;
    public string ApiKey { get; init; } = "";
    public string IPAddress { get; init; } = "127.0.0.1";
    public List<StoredMessage> Messages { get; init; } =
        new List<StoredMessage>() { new StoredMessage("default", ["yyyvbvbbbvbvyyy", "ygggggghggggggy", "yyyvbvbbbvbvyyy"]) };

    public CommandConfiguration Command { get; init; } = new CommandConfiguration();

    public static VestaboardConfiguration GetConfig()
    {
        if (File.Exists(ConfigFilePath))
        {
            //Load existing config
            VestaboardConfiguration config = JsonSerializer.Deserialize<VestaboardConfiguration>(File.ReadAllText(ConfigFilePath))!;

            if (config.Version < CURRENT_VERSION)
            {
                //Update and reserialize
                config.Version = CURRENT_VERSION;
                config.Serialize();
            }

            return config;
        }
        else
        {
            VestaboardConfiguration config = new VestaboardConfiguration
            {
                Version = CURRENT_VERSION
            };

            config.Serialize();

            return config;
        }
    }

    private void Serialize()
    {
        lock (_lock)
        {
            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(this));
        }
    }

    public class CommandConfiguration
    {
        public bool Enabled { get; init; } = true;
        public string CommandName { get; init; } = "vesta";
        //Admins can always use it, if it's enabled
        public bool ModsCanUse { get; init; } = false;
        public bool ElevatedCanUse { get; init; } = false;
        public bool RiffRaffCanUse { get; init; } = false;
        public bool AllowCreditRedemptions { get; init; } = false;
        public string CreditName { get; init; } = "TTTAS";
        public long CreditCost { get; init; } = 4;
    }
}
