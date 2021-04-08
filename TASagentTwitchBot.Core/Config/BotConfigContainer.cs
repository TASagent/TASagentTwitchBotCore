using System;
using System.IO;
using System.Text.Json;

namespace TASagentTwitchBot.Core.Config
{
    public interface IBotConfigContainer
    {
        BotConfiguration BotConfig { get; }

        void Initialize();
        void SerializeData();
    }

    public class BotConfigContainer : IBotConfigContainer
    {
        private static string ConfigPath => BGC.IO.DataManagement.PathForDataFile("Config", "Config.json");
        private static readonly object fileLock = new object();
        public BotConfiguration BotConfig { get; init; }

        public BotConfigContainer()
        {
            if (File.Exists(ConfigPath))
            {
                BotConfig = JsonSerializer.Deserialize<BotConfiguration>(File.ReadAllText(ConfigPath));
            }
            else
            {
                BotConfig = new BotConfiguration();
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(BotConfig));
            }
        }

        public void Initialize()
        {
            BotConfig.AuthConfiguration.RegenerateAuthStrings();
            SerializeData();
        }

        public void SerializeData()
        {
            lock (fileLock)
            {
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(BotConfig));
            }
        }
    }
}
