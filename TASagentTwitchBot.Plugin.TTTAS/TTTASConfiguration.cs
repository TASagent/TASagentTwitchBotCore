using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

namespace TASagentTwitchBot.Plugin.TTTAS
{
    public class TTTASConfiguration
    {
        private static string ConfigFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "TTTAS", "TTTASConfig.json");
        private static object _lock = new object();

        public string FeatureName { get; init; } = "Text-To-TAS";
        public string FeatureNameBrief { get; init; } = "TTTAS";

        public string SoundEffect { get; init; } = "FF7 Notification";

        public RedemptionConfiguration Redemption { get; init; } = new RedemptionConfiguration();
        public CommandConfiguration Command { get; init; } = new CommandConfiguration();

        public static TTTASConfiguration GetConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                //Load existing config
                return JsonSerializer.Deserialize<TTTASConfiguration>(File.ReadAllText(ConfigFilePath));
            }
            else
            {
                TTTASConfiguration config = new TTTASConfiguration();

                lock (_lock)
                {
                    File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config));
                }

                return config;
            }
        }

        public void UpdateRedemptionID(string newID)
        {
            Redemption.RedemptionID = newID;

            lock (_lock)
            {
                File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(this));
            }
        }

        public class CommandConfiguration
        {
            public bool Enabled { get; init; } = true;
            public string CommandName { get; init; } = "tttas";
            //Admins can always use it, if it's enabled
            public bool ModsCanUse { get; init; } = false;
            public bool ElevatedCanUse { get; init; } = false;
            public bool RiffRaffCanUse { get; init; } = false;
        }

        public class RedemptionConfiguration
        {
            public bool Enabled { get; init; } = true;
            public string RedemptionID { get; set; } = null;
            public string Name { get; init; } = "Text-To-TAS";
            public string Description { get; init; } = "Use the automated TTTAS system to put words in the streamer's mouth.";
            public string BackgroundColor { get; init; } = "#56BDE6";
            public int Cost { get; init; } = 1_000;
            public bool AutoApprove { get; init; } = true;
        }
    }
}
