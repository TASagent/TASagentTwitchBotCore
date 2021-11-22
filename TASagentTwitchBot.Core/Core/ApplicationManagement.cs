namespace TASagentTwitchBot.Core;

public class ApplicationManagement
{
    private readonly ErrorHandler errorHandler;
    private readonly TaskCompletionSource exitTrigger = new TaskCompletionSource();
    private readonly List<IShutdownListener> shutdownListeners = new List<IShutdownListener>();

    public ApplicationManagement(ErrorHandler errorHandler)
    {
        this.errorHandler = errorHandler;
    }

    public void RegisterShutdownListener(IShutdownListener shutdownListener) => shutdownListeners.Add(shutdownListener);

    public Task WaitForEndAsync() => exitTrigger.Task;

    public void TriggerExit()
    {
        foreach (IShutdownListener listener in shutdownListeners)
        {
            try
            {
                listener.NotifyShuttingDown();
            }
            catch (Exception ex)
            {
                errorHandler.LogSystemException(ex);
            }
        }

        shutdownListeners.Clear();

        exitTrigger.TrySetResult();
    }
}

public interface IShutdownListener
{
    void NotifyShuttingDown();
}
