using System.Threading.Channels;

using TASagentTwitchBot.Core.Audio;

namespace TASagentTwitchBot.Core.Notifications;

[AutoRegister]
public interface IActivityDispatcher
{
    bool ReplayNotification(int index);
    void QueueActivity(ActivityRequest activity, bool approved);
    bool UpdatePendingRequest(int index, bool approved);
    void UpdateAllRequests(string userId, bool approved);

    void Skip();
}

/// <summary>
/// Coordinates different requested display features so they don't collide
/// </summary>
public class ActivityDispatcher : IActivityDispatcher, IDisposable
{
    private readonly ErrorHandler errorHandler;
    private readonly ICommunication communication;
    private readonly IMessageAccumulator messageAccumulator;
    private readonly IAudioPlayer audioPlayer;

    private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private readonly ChannelWriter<ActivityRequest> activityWriter;
    private readonly ChannelReader<ActivityRequest> activityReader;

    private readonly Dictionary<int, ActivityRequest> activityDict = new Dictionary<int, ActivityRequest>();
    private readonly Dictionary<int, ActivityRequest> pendingActivityRequests = new Dictionary<int, ActivityRequest>();

    private readonly Task activityListeningTask;

    private int nextActivityID = 0;
    private bool disposedValue;
    private ActivityRequest? lastFinishedRequest = null;

    public ActivityDispatcher(
        Config.BotConfiguration botConfig,
        ErrorHandler errorHandler,
        ICommunication communication,
        IMessageAccumulator messageAccumulator,
        IAudioPlayer audioPlayer)
    {
        this.errorHandler = errorHandler;
        this.communication = communication;
        this.messageAccumulator = messageAccumulator;
        this.audioPlayer = audioPlayer;

        Channel<ActivityRequest> channel = Channel.CreateUnbounded<ActivityRequest>();
        activityWriter = channel.Writer;
        activityReader = channel.Reader;

        if (botConfig.UseThreadedMonitors)
        {
            activityListeningTask = Task.Run(ListenForActivity);
        }
        else
        {
            activityListeningTask = ListenForActivity();
        }
    }

    private async Task ListenForActivity()
    {
        List<Task> taskList = new List<Task>();

        try
        {
            await foreach (ActivityRequest activityRequest in activityReader.ReadAllAsync(cancellationTokenSource.Token))
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
        catch (OperationCanceledException) { /* swallow */ }
        catch (Exception ex)
        {
            errorHandler.LogSystemException(ex);
        }
    }

    public bool ReplayNotification(int index)
    {
        if (index == -1 && lastFinishedRequest is not null)
        {
            index = lastFinishedRequest.Id;
        }

        if (activityDict.TryGetValue(index, out ActivityRequest? activityRequest))
        {
            return activityWriter.TryWrite(activityRequest);
        }

        return false;
    }

    public void Skip() => audioPlayer.RequestCancel();

    public void QueueActivity(ActivityRequest activity, bool approved)
    {
        activity.Id = nextActivityID++;

        if (!approved)
        {
            //Submit as request instead
            pendingActivityRequests.Add(activity.Id, activity);
            communication.NotifyPendingNotification(activity.Id, activity.ToString()!);
            return;
        }

        activityDict.Add(activity.Id, activity);
        communication.NotifyNotification(activity.Id, activity.ToString()!);
        activityWriter.TryWrite(activity);
    }

    public bool UpdatePendingRequest(int index, bool approve)
    {
        if (!pendingActivityRequests.TryGetValue(index, out ActivityRequest? activity))
        {
            return false;
        }

        pendingActivityRequests.Remove(index);

        if (approve)
        {
            //Queue approved activity
            activityDict.Add(activity.Id, activity);
            communication.NotifyNotification(activity.Id, activity.ToString()!);
            activityWriter.TryWrite(activity);
        }

        messageAccumulator.RemovePendingNotification(activity.Id);

        return true;
    }

    public void UpdateAllRequests(string userId, bool approve)
    {
        foreach (ActivityRequest request in pendingActivityRequests.Values.Where(x => x.RequesterId == userId).ToList())
        {
            pendingActivityRequests.Remove(request.Id);

            if (approve)
            {
                //Queue approved activity
                activityDict.Add(request.Id, request);
                communication.NotifyNotification(request.Id, request.ToString()!);
                activityWriter.TryWrite(request);
            }

            messageAccumulator.RemovePendingNotification(request.Id);
        }
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

                activityListeningTask.Wait(2_000);

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
