using System.IO;
using System.Text.Json;

namespace TASagentTwitchBot.Core.EventSub
{
    public class EventSubConfig
    {
        private static string ConfigFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "EventSubConfig.json");
        private static readonly object _lock = new object();

        public string ServerUserName { get; set; } = "";
        public string ServerAccessToken { get; set; } = "";
        public string ServerAddress { get; set; } = "https://server.tas.wtf";

        public static EventSubConfig GetConfig()
        {
            EventSubConfig config;
            if (File.Exists(ConfigFilePath))
            {
                //Load existing config
                config = JsonSerializer.Deserialize<EventSubConfig>(File.ReadAllText(ConfigFilePath));
            }
            else
            {
                config = new EventSubConfig();
            }

            config.Serialize();

            return config;
        }

        public void Serialize()
        {
            lock (_lock)
            {
                File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(this));
            }
        }
    }

}
