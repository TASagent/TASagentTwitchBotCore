using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TASagentTwitchBot.Core.API.Twitch;

namespace TASagentTwitchBot.Core.WebSub
{
    public interface IStreamChangeSubscriber : IWebSubSubscriber
    {
        void NotifyUpdate(TwitchStreamData streamData);
    }

    public interface IStreamLiveListener
    {
        void NotifyLiveStatus(bool isLive);
    }

    public interface IStreamDetailListener
    {
        void NotifyStreamDetailUpdate(TwitchStreamData streamData);
    }


    public class StreamChangeSubscriber : IStreamChangeSubscriber, IWebSubSubscriber
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly Config.IExternalWebAccessConfiguration webAccessConfig;
        private readonly ICommunication communication;
        private readonly HelixHelper helixHelper;

        private readonly IStreamLiveListener[] streamLiveListeners;
        private readonly IStreamDetailListener[] streamDetailListeners;

        private TwitchStreamData currentStreamData = null;
        private string externalURL = null;
        private string subURL = null;

        public StreamChangeSubscriber(
            Config.BotConfiguration botConfig,
            Config.IExternalWebAccessConfiguration webAccessConfiguration,
            ICommunication communication,
            HelixHelper helixHelper,
            IEnumerable<IStreamLiveListener> streamLiveListeners,
            IEnumerable<IStreamDetailListener> streamDetailListeners)
        {
            this.botConfig = botConfig;
            webAccessConfig = webAccessConfiguration;

            this.communication = communication;
            this.helixHelper = helixHelper;

            this.streamLiveListeners = streamLiveListeners.ToArray();
            this.streamDetailListeners = streamDetailListeners.ToArray();
        }

        public async Task Subscribe(WebSubHandler webSubHandler)
        {
            string externalAddress = await webAccessConfig.GetExternalWebSubAddress();

            externalURL = $"{externalAddress}/TASagentBotAPI/WebSub/Stream";
            subURL = $"https://api.twitch.tv/helix/streams?user_id={botConfig.BroadcasterId}";

            bool success = await helixHelper.WebhookSubscribe(
                callback: externalURL,
                mode: "subscribe",
                topic: subURL,
                lease: 48 * 60 * 60,
                secret: webSubHandler.CreateSecretForRoute("/TASagentBotAPI/WebSub/Stream"));

            if (!success)
            {
                communication.SendErrorMessage("Failed to subscribe to Stream Changes. Aborting.");

                externalURL = null;
                subURL = null;

                return;
            }

            TwitchStreams streamData = await helixHelper.GetStreams(userIDs: new List<string>() { botConfig.BroadcasterId });

            if (streamData.Data is null || streamData.Data.Count == 0)
            {
                currentStreamData = null;
            }
            else
            {
                currentStreamData = streamData.Data[0];

                foreach (IStreamDetailListener detailListener in streamDetailListeners)
                {
                    detailListener.NotifyStreamDetailUpdate(currentStreamData);
                }
            }

            foreach (IStreamLiveListener liveListener in streamLiveListeners)
            {
                liveListener.NotifyLiveStatus(currentStreamData is not null);
            }
        }

        public void NotifyUpdate(TwitchStreamData streamData)
        {
            if (currentStreamData == streamData)
            {
                //No change in status
                return;
            }

            //Online status changed if one of these is null
            bool onlineStatusChange = currentStreamData is null || streamData is null;

            currentStreamData = streamData;

            if (onlineStatusChange)
            {
                foreach (IStreamLiveListener liveListener in streamLiveListeners)
                {
                    //We are live if the streamData is not null
                    liveListener.NotifyLiveStatus(currentStreamData is not null);
                }
            }

            if (currentStreamData is not null)
            {
                //Only update if we have real data
                foreach (IStreamDetailListener detailListener in streamDetailListeners)
                {
                    detailListener.NotifyStreamDetailUpdate(currentStreamData);
                }
            }
        }

        public async Task Unsubscribe(WebSubHandler webSubHandler)
        {
            if (externalURL is null || subURL is null)
            {
                return;
            }

            TaskCompletionSource taskCompletionSource = new TaskCompletionSource();
            webSubHandler.NotifyPendingClosure("/TASagentBotAPI/WebSub/Stream", taskCompletionSource);

            await helixHelper.WebhookSubscribe(
                callback: externalURL,
                mode: "unsubscribe",
                topic: subURL,
                lease: 0,
                secret: "");


            //Clear values
            externalURL = null;
            subURL = null;

            await taskCompletionSource.Task;
        }
    }
}
