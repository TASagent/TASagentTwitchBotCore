using System.Text.Json;

namespace TASagentTwitchBot.Core.Config;

public class ServerConfig
{
    private static string ConfigFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "ServerConfig.json");
    private static readonly object _lock = new object();

    public string ServerUserName { get; set; } = "";
    public string ServerAccessToken { get; set; } = "";
    public string ServerAddress { get; set; } = "https://server.tas.wtf";

    public static ServerConfig GetConfig()
    {
        ServerConfig config;
        if (File.Exists(ConfigFilePath))
        {
            //Load existing config
            config = JsonSerializer.Deserialize<ServerConfig>(File.ReadAllText(ConfigFilePath))!;
        }
        else
        {
            config = new ServerConfig();
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
