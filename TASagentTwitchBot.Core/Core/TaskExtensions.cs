namespace TASagentTwitchBot.Core;

public static class TaskExtensions
{
    public static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        return task.IsCompleted ?
            task :
            task.ContinueWith(
                continuationFunction: completedTask => completedTask.GetAwaiter().GetResult(),
                cancellationToken: cancellationToken,
                continuationOptions: TaskContinuationOptions.ExecuteSynchronously,
                scheduler: TaskScheduler.Default);
    }

    public static Task WithCancellation(this Task task, CancellationToken cancellationToken)
    {
        return task.IsCompleted ?
            task :
            task.ContinueWith(
                continuationAction: task => task.GetAwaiter(),
                cancellationToken: cancellationToken,
                continuationOptions: TaskContinuationOptions.ExecuteSynchronously,
                scheduler: TaskScheduler.Default);
    }
}
