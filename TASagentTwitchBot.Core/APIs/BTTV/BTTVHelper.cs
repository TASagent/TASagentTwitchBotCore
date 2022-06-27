using System.Text.Json;
using RestSharp;

namespace TASagentTwitchBot.Core.API.BTTV;

[AutoRegister]
public interface IBTTVHelper
{
    Task<BTTVChannelData?> GetChannelBTTVData(string userId);
    Task<List<FFZEmote>?> GetChannelFFZEmotes(string userId);
    Task<List<BTTVGlobalEmote>?> GetGlobalEmotes();
}

public class BTTVHelper : IBTTVHelper
{
    private static readonly Uri BTTVAPIURI = new Uri("https://api.betterttv.net/3");

    public BTTVHelper() { }

    public async Task<List<BTTVGlobalEmote>?> GetGlobalEmotes()
    {
        RestClient restClient = new RestClient(BTTVAPIURI);
        RestRequest request = new RestRequest("cached/emotes/global", Method.Get);

        RestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<BTTVGlobalEmote>>(response.Content!);
    }

    public async Task<BTTVChannelData?> GetChannelBTTVData(string userId)
    {
        RestClient restClient = new RestClient(BTTVAPIURI);
        RestRequest request = new RestRequest($"cached/users/twitch/{userId}", Method.Get);

        RestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        return JsonSerializer.Deserialize<BTTVChannelData>(response.Content!);
    }

    public async Task<List<FFZEmote>?> GetChannelFFZEmotes(string userId)
    {
        RestClient restClient = new RestClient(BTTVAPIURI);
        RestRequest request = new RestRequest($"cached/frankerfacez/users/twitch/{userId}", Method.Get);

        RestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<FFZEmote>>(response.Content!);
    }
}
