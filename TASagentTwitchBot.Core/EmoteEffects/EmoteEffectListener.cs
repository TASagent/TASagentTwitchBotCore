using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.API.BTTV;

namespace TASagentTwitchBot.Core.EmoteEffects
{
    public interface IEmoteEffectListener
    {
        void RefreshEmotes();
    }


    public class EmoteEffectListener : IEmoteEffectListener, IDisposable
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly IHubContext<Web.Hubs.EmoteHub> emoteHubContext;
        private readonly BTTVHelper bttvHelper;
        private readonly EmoteEffectConfiguration emoteEffectConfig;

        private static readonly object fetchingLock = new object();
        private readonly Dictionary<string, string> externalEmoteLookup = new Dictionary<string, string>();

        private readonly SemaphoreSlim initSemaphore = new SemaphoreSlim(1);

        private bool initialized = false;
        private bool disposedValue = false;

        public EmoteEffectListener(
            Config.BotConfiguration botConfig,
            ICommunication communication,
            IHubContext<Web.Hubs.EmoteHub> emoteHubContext,
            BTTVHelper bttvHelper,
            EmoteEffectConfiguration emoteEffectConfig)
        {
            this.botConfig = botConfig;
            this.emoteHubContext = emoteHubContext;
            this.bttvHelper = bttvHelper;
            this.emoteEffectConfig = emoteEffectConfig;

            communication.ReceiveMessageHandlers += ReceiveMessageHandler;
        }

        public void RefreshEmotes()
        {
            lock (fetchingLock)
            {
                initialized = false;
            }
        }

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

            List<BTTVGlobalEmote> globalEmotes = await bttvHelper.GetGlobalEmotes();
            BTTVChannelData channelData = await bttvHelper.GetChannelBTTVData(botConfig.BroadcasterId);
            List<FFZEmote> ffzEmotes = await bttvHelper.GetChannelFFZEmotes(botConfig.BroadcasterId);

            foreach (BTTVChannelEmote channelEmote in channelData.ChannelEmotes)
            {
                if (!externalEmoteLookup.ContainsKey(channelEmote.Code))
                {
                    externalEmoteLookup.Add(channelEmote.Code, channelEmote.GetLargeURL());
                }
            }

            foreach (BTTVSharedEmote sharedEmote in channelData.SharedEmotes)
            {
                if (!externalEmoteLookup.ContainsKey(sharedEmote.Code))
                {
                    externalEmoteLookup.Add(sharedEmote.Code, sharedEmote.GetLargeURL());
                }
            }

            foreach (FFZEmote ffzEmote in ffzEmotes)
            {
                if (!externalEmoteLookup.ContainsKey(ffzEmote.Code))
                {
                    externalEmoteLookup.Add(ffzEmote.Code, ffzEmote.GetLargeURL());
                }
            }

            foreach (BTTVGlobalEmote globalEmote in globalEmotes)
            {
                if (!externalEmoteLookup.ContainsKey(globalEmote.Code))
                {
                    externalEmoteLookup.Add(globalEmote.Code, globalEmote.GetLargeURL());
                }
            }
        }


        private async void ReceiveMessageHandler(IRC.TwitchChatter chatter)
        {
            if (emoteEffectConfig.EnableBTTVEmotes)
            {
                if (!initialized)
                {
                    await InitializeAsync();
                }

                List<string> emotes = null;

                if (chatter.Emotes.Count > 0)
                {
                    emotes = new List<string>(chatter.Emotes.Select(x => x.URL));
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
                }
            }
            else
            {
                //No BTTV Emotes, use old method
                if (chatter.Emotes.Count > 0)
                {
                    await emoteHubContext.Clients.All.SendAsync("ReceiveEmotes", chatter.Emotes.Select(x => x.URL).ToList());
                }
            }
        }

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
                if (externalEmoteLookup.ContainsKey(word))
                {
                    emotes.Add(externalEmoteLookup[word]);
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
}
