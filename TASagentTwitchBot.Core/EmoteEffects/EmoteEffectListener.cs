using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.API.BTTV;
using static TASagentTwitchBot.Core.IRC.TwitchChatter;

namespace TASagentTwitchBot.Core.EmoteEffects;

[AutoRegister]
public interface IEmoteEffectListener
{
    void RefreshEmotes();
    void IgnoreEmote(string emote);
    void UnignoreEmote(string emote);
}

[AutoRegister]
public interface IEmoteListener
{
    void NotifyEmotes(IEnumerable<string> urls);
}

public class EmoteEffectListener : IEmoteEffectListener, IDisposable
{
    private readonly Config.BotConfiguration botConfig;
    private readonly IHubContext<Web.Hubs.EmoteHub> emoteHubContext;
    private readonly IBTTVHelper bttvHelper;
    private readonly Bits.ICheerHelper cheerHelper;
    private readonly EmoteEffectConfiguration emoteEffectConfig;
    private readonly List<IEmoteListener> emoteListeners;

    private readonly Dictionary<string, string> externalEmoteLookup = new Dictionary<string, string>();

    private readonly SemaphoreSlim initSemaphore = new SemaphoreSlim(1);
    private static readonly Regex cheerFinder = new Regex(@"(?:\s|^)([a-zA-Z]+)(\d+)(?:\s|$)");

    private bool initialized = false;
    private bool disposedValue = false;

    public EmoteEffectListener(
        Config.BotConfiguration botConfig,
        ICommunication communication,
        IHubContext<Web.Hubs.EmoteHub> emoteHubContext,
        IBTTVHelper bttvHelper,
        Bits.ICheerHelper cheerHelper,
        EmoteEffectConfiguration emoteEffectConfig,
        IEnumerable<IEmoteListener> emoteListeners)
    {
        this.botConfig = botConfig;
        this.emoteHubContext = emoteHubContext;
        this.bttvHelper = bttvHelper;
        this.cheerHelper = cheerHelper;
        this.emoteEffectConfig = emoteEffectConfig;

        this.emoteListeners = emoteListeners.ToList();

        communication.ReceiveMessageHandlers += ReceiveMessageHandler;
    }

    public void RefreshEmotes() => initialized = false;

    public void IgnoreEmote(string emote) => emoteEffectConfig.IgnoreEmote(emote);

    public void UnignoreEmote(string emote) => emoteEffectConfig.UnignoreEmote(emote);

    private async Task InitializeAsync()
    {
        if (initialized)
        {
            return;
        }

        await initSemaphore.WaitAsync();

        if (initialized)
        {
            initSemaphore.Release();
            return;
        }

        await Initialize();

        initialized = true;
        initSemaphore.Release();
    }

    private async Task Initialize()
    {
        externalEmoteLookup.Clear();

        List<BTTVGlobalEmote> globalEmotes = await bttvHelper.GetGlobalEmotes() ?? throw new Exception($"Unable to get BTTV Global Emotes");
        BTTVChannelData channelData = await bttvHelper.GetChannelBTTVData(botConfig.BroadcasterId) ?? throw new Exception($"Unable to get BTTV Channel Emotes");
        List<FFZEmote> ffzEmotes = await bttvHelper.GetChannelFFZEmotes(botConfig.BroadcasterId) ?? throw new Exception($"Unable to get BTTV Channel FFZ Emotes");

        foreach (BTTVChannelEmote channelEmote in channelData.ChannelEmotes)
        {
            externalEmoteLookup.TryAdd(channelEmote.Code, channelEmote.GetLargeURL());
        }

        foreach (BTTVSharedEmote sharedEmote in channelData.SharedEmotes)
        {
            externalEmoteLookup.TryAdd(sharedEmote.Code, sharedEmote.GetLargeURL());
        }

        foreach (FFZEmote ffzEmote in ffzEmotes)
        {
            externalEmoteLookup.TryAdd(ffzEmote.Code, ffzEmote.GetLargeURL());
        }

        foreach (BTTVGlobalEmote globalEmote in globalEmotes)
        {
            externalEmoteLookup.TryAdd(globalEmote.Code, globalEmote.GetLargeURL());
        }
    }


    private async void ReceiveMessageHandler(IRC.TwitchChatter chatter)
    {
        //Automatically ignore the emotes of restricted users
        if (chatter.User.AuthorizationLevel <= Commands.AuthorizationLevel.Restricted)
        {
            return;
        }

        if (emoteEffectConfig.EnableBTTVEmotes)
        {
            if (!initialized)
            {
                await InitializeAsync();
            }

            List<string> emotes;

            if (chatter.Emotes.Count > 0)
            {
                emotes = new List<string>(chatter.Emotes.Where(ShouldRender).Select(x => x.URL));
            }
            else
            {
                emotes = new List<string>();
            }

            int startEmoteSearchIndex = 0;

            for (int i = 0; i < chatter.Emotes.Count; i++)
            {
                int endEmoteSearchIndex = chatter.Emotes[i].StartIndex;

                if (endEmoteSearchIndex - startEmoteSearchIndex > 1)
                {
                    SearchSegment(chatter.Message[startEmoteSearchIndex..endEmoteSearchIndex].Trim(), emotes);
                }

                startEmoteSearchIndex = endEmoteSearchIndex + 1;
            }

            //Handle remainder
            if (chatter.Message.Length - startEmoteSearchIndex > 1)
            {
                SearchSegment(chatter.Message[startEmoteSearchIndex..].Trim(), emotes);
            }

            if (emotes.Count > 0)
            {
                await emoteHubContext.Clients.All.SendAsync("ReceiveEmotes", emotes);

                foreach (IEmoteListener listener in emoteListeners)
                {
                    listener.NotifyEmotes(emotes);
                }
            }
        }
        else
        {
            //No BTTV Emotes, use old method
            if (chatter.Emotes.Count > 0)
            {
                List<string> emotes = chatter.Emotes.Where(ShouldRender).Select(x => x.URL).ToList();

                await emoteHubContext.Clients.All.SendAsync(
                    "ReceiveEmotes",
                    emotes);

                foreach (IEmoteListener listener in emoteListeners)
                {
                    listener.NotifyEmotes(emotes);
                }
            }
        }

        if (chatter.Bits > 0)
        {
            //Handle Cheermotes
            Dictionary<string, int> cheerTypes = new Dictionary<string, int>();

            foreach (Match match in cheerFinder.Matches(chatter.Message))
            {
                string prefix = match.Groups[1].Value.ToLower();
                int quantity = int.Parse(match.Groups[2].Value);

                if (!cheerTypes.ContainsKey(prefix))
                {
                    cheerTypes[prefix] = quantity;
                }
                else
                {
                    cheerTypes[prefix] += quantity;
                }
            }

            foreach (KeyValuePair<string, int> kvp in cheerTypes)
            {
                string? url = await cheerHelper.GetCheermoteURL(kvp.Key);

                if (string.IsNullOrEmpty(url))
                {
                    continue;
                }

                await emoteHubContext.Clients.All.SendAsync(
                    "ReceiveCheermotes",
                    new CheermoteData(url, kvp.Value));
            }
        }
    }

    private bool ShouldRender(IRC.TwitchChatter.Emote emote) => !emoteEffectConfig.IgnoredEmotes.Contains(emote.Code);
    private bool ShouldRender(string emote) => !emoteEffectConfig.IgnoredEmotes.Contains(emote);

    //Searches a string segment for a BTTV emote
    private void SearchSegment(string searchSegment, List<string> emotes)
    {
        if (searchSegment.Length <= 1)
        {
            return;
        }

        //Split
        string[] splitSearchSegment = searchSegment.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        //Check each word against External Emotes
        foreach (string word in splitSearchSegment)
        {
            if (externalEmoteLookup.TryGetValue(word, out string? emote) && ShouldRender(word))
            {
                emotes.Add(emote);
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                initSemaphore.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public record CheermoteData(string Url, int Bits);
