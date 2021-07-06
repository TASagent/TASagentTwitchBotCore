using System;
using System.IO;
using System.Text.Json;

namespace TASagentTwitchBot.Core.EmoteEffects
{
    public class EmoteEffectConfiguration
    {
        private static string ConfigFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "EmoteEffectsConfig.json");
        private static readonly object _lock = new object();

        public bool EnableBTTVEmotes { get; init; } = false;

        public static EmoteEffectConfiguration GetConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                //Load existing config
                return JsonSerializer.Deserialize<EmoteEffectConfiguration>(File.ReadAllText(ConfigFilePath));
            }
            else
            {
                EmoteEffectConfiguration config = new EmoteEffectConfiguration();

                lock (_lock)
                {
                    File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config));
                }

                return config;
            }
        }
    }
}
