using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.IO;

namespace TASagentTwitchBot.Core.WebServer.Config
{
    public class WebServerConfig
    {
        private static string ConfigFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "ServerConfig.json");
        private static readonly object _lock = new object();

        public string TwitchClientId { get; set; } = "";
        public string TwitchClientSecret { get; set; } = "";

        public string AppAccessToken { get; set; } = "";

        public string ExternalAddress { get; set; } = "https://server.tas.wtf";

        public static WebServerConfig GetConfig()
        {
            WebServerConfig config;
            if (File.Exists(ConfigFilePath))
            {
                //Load existing config
                config = JsonSerializer.Deserialize<WebServerConfig>(File.ReadAllText(ConfigFilePath));
            }
            else
            {
                config = new WebServerConfig();
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
