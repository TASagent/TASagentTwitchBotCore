using System.Text.Json;

namespace TASagentTwitchBot.Core.API.Tiltify;

public class TiltifyConfiguration
{
    private static readonly object _lock = new object();
    private static string ConfigFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "Tiltify.json");

    public string User { get; set; } = "";
    public string UserId { get; set; } = "";

    public string ApplicationId { get; set; } = "";
    public string ApplicationSecret { get; set; } = "";

    //Populate this from https://dashboard.tiltify.com/
    public string ApplicationAccessToken { get; set; } = "";

    public int CampaignId { get; set; } = -1;
    public bool MonitorCampaign { get; set; } = false;

    public static TiltifyConfiguration GetConfig()
    {
        TiltifyConfiguration config;
        if (File.Exists(ConfigFilePath))
        {
            //Load existing config
            config = JsonSerializer.Deserialize<TiltifyConfiguration>(File.ReadAllText(ConfigFilePath))!;
        }
        else
        {
            config = new TiltifyConfiguration();
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
}
