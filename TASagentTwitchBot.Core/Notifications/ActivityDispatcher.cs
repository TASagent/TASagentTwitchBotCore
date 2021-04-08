using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using TASagentTwitchBot.Core.Audio;

namespace TASagentTwitchBot.Core.Notifications
{
    public interface IActivityDispatcher
    {
        bool ReplayNotification(int index);
        void QueueActivity(ActivityRequest activity, bool approved);
        bool UpdatePendingRequest(int index, bool approved);

        void Skip();

    }

    /// <summary>
    /// Coordinates different requested display features so they don't collide
    /// </summary>
    public class ActivityDispatcher : IActivityDispatcher, IDisposable
    {
        private readonly ICommunication communication;
        private readonly IAudioPlayer audioPlayer;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ChannelWriter<ActivityRequest> activityWriter;
        private readonly ChannelReader<ActivityRequest> activityReader;

        private readonly Dictionary<int, ActivityRequest> activityDict = new Dictionary<int, ActivityRequest>();
        private readonly Dictionary<int, ActivityRequest> pendingActivityRequests = new Dictionary<int, ActivityRequest>();

        private int nextActivityID = 0;
        private bool disposedValue;
        private ActivityRequest lastFinishedRequest = null;

        public ActivityDispatcher(
            ICommunication communication,
            IAudioPlayer audioPlayer)
        {
            this.communication = communication;
            this.audioPlayer = audioPlayer;

            Channel<ActivityRequest> channel = Channel.CreateUnbounded<ActivityRequest>();
            activityWriter = channel.Writer;
            activityReader = channel.Reader;

            ListenForActivity();
        }

        public async void ListenForActivity()
        {
            List<Task> taskList = new List<Task>();

            await foreach (ActivityRequest activityRequest in activityReader.ReadAllAsync())
            {
                if (activityRequest.Played)
                {
                    communication.SendDebugMessage($"Replaying Notification {activityRequest.Id}: {activityRequest}");
                }
                else
                {
                    activityRequest.Played = true;
                    communication.SendDebugMessage($"Playing Notification {activityRequest.Id}: {activityRequest}");
                }

                await activityRequest.Execute().WithCancellation(cancellationTokenSource.Token);

                lastFinishedRequest = activityRequest;

                taskList.Clear();

                //2 second delay between notifications
                await Task.Delay(2000, cancellationTokenSource.Token);
            }
        }

        public bool ReplayNotification(int index)
        {
            if (index == -1 && lastFinishedRequest != null)
            {
                index = lastFinishedRequest.Id;
            }

            if (!activityDict.ContainsKey(index))
            {
                return false;
            }

            return activityWriter.TryWrite(activityDict[index]);
        }

        public void Skip() => audioPlayer.RequestCancel();


        public void QueueActivity(ActivityRequest activity, bool approved)
        {
            activity.Id = nextActivityID++;

            if (!approved)
            {
                //Submit as request instead
                pendingActivityRequests.Add(activity.Id, activity);
                communication.NotifyPendingNotification(activity.Id, activity.ToString());
                return;
            }

            activityDict.Add(activity.Id, activity);
            communication.NotifyNotification(activity.Id, activity.ToString());
            activityWriter.TryWrite(activity);
        }

        public bool UpdatePendingRequest(int index, bool approve)
        {
            if (!pendingActivityRequests.ContainsKey(index))
            {
                return false;
            }

            ActivityRequest activity = pendingActivityRequests[index];
            pendingActivityRequests.Remove(index);

            if (approve)
            {
                //Queue approved activity
                activityDict.Add(activity.Id, activity);
                communication.NotifyNotification(activity.Id, activity.ToString());
                activityWriter.TryWrite(activity);
            }

            return true;
        }

        public void DemandPlayAudioImmediate(AudioRequest audioRequest) =>
            audioPlayer.DemandPlayAudioImmediate(audioRequest);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    activityWriter.TryComplete();

                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
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
