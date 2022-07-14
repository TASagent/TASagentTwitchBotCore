using BGC.Collections.Generic;

namespace TASagentTwitchBot.Core.Notifications;

[AutoRegister]
public interface INotificationImageHelper
{
    string GetRandomDefaultImageURL();
    string GetRandomImageURL(string directory);
}

public class NotificationImageHelper : INotificationImageHelper
{
    private const string DEFAULT_ASSET_URL = "Images";

    private readonly string assetsPath;

    private readonly Dictionary<string, DepletableBag<string>> subdirectoryLookup = new Dictionary<string, DepletableBag<string>>();
    private readonly ICommunication communication;

    public NotificationImageHelper(
        ICommunication communication)
    {
        this.communication = communication;

        assetsPath = BGC.IO.DataManagement.PathForDataSubDirectory("wwwroot", "Assets");

        PopulateDirectoryLookup(DEFAULT_ASSET_URL);

        if (subdirectoryLookup[DEFAULT_ASSET_URL].Count == 0)
        {
            communication.SendWarningMessage($"Default notification image directory \"{Path.Combine(assetsPath, DEFAULT_ASSET_URL)}\" is empty. No default images can be provided.");
        }
    }

    public string GetRandomDefaultImageURL() => GetRandomImageURL(DEFAULT_ASSET_URL);
    public string GetRandomImageURL(string directory)
    {
        if (!subdirectoryLookup.ContainsKey(directory))
        {
            PopulateDirectoryLookup(directory);
        }

        if (subdirectoryLookup[directory].Count == 0)
        {
            communication.SendWarningMessage($"Requested empty image directory {directory}");
            return "";
        }

        return subdirectoryLookup[directory].PopNext()!;
    }

    private void PopulateDirectoryLookup(string directory)
    {
        if (subdirectoryLookup.ContainsKey(directory))
        {
            //Already populated
            return;
        }

        DepletableBag<string> imageURLs = new DepletableBag<string>(true);

        string assetPath = Path.Combine(assetsPath, directory);

        if (!Directory.Exists(assetPath))
        {
            Directory.CreateDirectory(assetPath);
        }

        foreach (string imagePath in Directory.GetFiles(Path.Combine(assetsPath, directory)))
        {
            imageURLs.Add($"/Assets/{directory}/{Path.GetFileName(imagePath)}");
        }

        subdirectoryLookup.Add(directory, imageURLs);
    }
}
