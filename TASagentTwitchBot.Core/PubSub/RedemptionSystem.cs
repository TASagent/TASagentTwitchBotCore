using System.Text.Json;
using System.Threading.Channels;

namespace TASagentTwitchBot.Core.PubSub;

[AutoRegister]
public interface IRedemptionSystem
{
    Task Initialize();
    void HandleRedemption(ChannelPointMessageData.Datum redemption);
}

public delegate Task RedemptionHandler(Database.User user, ChannelPointMessageData.Datum.RedemptionData redemption);

[AutoRegister]
public interface IRedemptionContainer
{
    Task RegisterHandler(Dictionary<string, RedemptionHandler> handlers);
}


public class RedemptionSystem : IRedemptionSystem, IDisposable
{
    private readonly ErrorHandler errorHandler;
    private readonly ICommunication communication;

    private readonly IRedemptionContainer[] redemptionContainers;
    private readonly Database.IUserHelper userHelper;

    private readonly Dictionary<string, RedemptionHandler> redemptionHandlers = new Dictionary<string, RedemptionHandler>();

    private readonly ChannelWriter<(bool, string)> logWriterChannel;
    private readonly ChannelReader<(bool, string)> logReaderChannel;

    private readonly Lazy<Logs.LocalLogger> handledRedemptionLog =
        new Lazy<Logs.LocalLogger>(() => new Logs.LocalLogger("RedemptionLogs", "HandledRedemptions"));
    private readonly Lazy<Logs.LocalLogger> unhandledRedemptionLog =
        new Lazy<Logs.LocalLogger>(() => new Logs.LocalLogger("RedemptionLogs", "UnhandledRedemptions"));

    private readonly bool logRedemptions;
    private readonly Task logHandlerTask;

    private bool disposedValue;

    public RedemptionSystem(
        Config.BotConfiguration botConfig,
        ErrorHandler errorHandler,
        ICommunication communication,
        IEnumerable<IRedemptionContainer> redemptionContainers,
        Database.IUserHelper userHelper)
    {
        this.errorHandler = errorHandler;
        this.communication = communication;

        this.userHelper = userHelper;

        this.redemptionContainers = redemptionContainers.ToArray();

        logRedemptions = botConfig.ExhaustiveRedemptionLogging;

        Channel<(bool, string)> logChannel = Channel.CreateUnbounded<(bool, string)>();
        logWriterChannel = logChannel.Writer;
        logReaderChannel = logChannel.Reader;

        if (logRedemptions)
        {
            if (botConfig.UseThreadedMonitors)
            {
                logHandlerTask = Task.Run(HandleLogs);
            }
            else
            {
                logHandlerTask = HandleLogs();
            }
        }
        else
        {
            logWriterChannel.TryComplete();
            logHandlerTask = Task.CompletedTask;
        }

    }

    private async Task HandleLogs()
    {
        await foreach ((bool handled, string line) in logReaderChannel.ReadAllAsync())
        {
            if (handled)
            {
                handledRedemptionLog.Value.PushLine(line);
            }
            else
            {
                unhandledRedemptionLog.Value.PushLine(line);
            }
        }
    }

    public async void HandleRedemption(ChannelPointMessageData.Datum redemption)
    {
        //Handle redemption
        string rewardID = redemption.Redemption.Reward.Id;

        if (!redemptionHandlers.TryGetValue(rewardID, out RedemptionHandler? redemptionHandler))
        {
            if (logRedemptions)
            {
                logWriterChannel.TryWrite((false, $"*** Handler Not Found:\n{JsonSerializer.Serialize(redemption)}"));
            }

            communication.SendErrorMessage($"Redemption handler not found: {rewardID}");
            return;
        }

        Database.User? user = await userHelper.GetUserByTwitchId(redemption.Redemption.User.Id);

        if (user is null)
        {
            if (logRedemptions)
            {
                logWriterChannel.TryWrite((false, $"*** User Not Found:\n{JsonSerializer.Serialize(redemption)}"));
            }

            communication.SendErrorMessage($"User not found: {redemption.Redemption.User.Id}");
            return;
        }

        if (logRedemptions)
        {
            logWriterChannel.TryWrite((true, JsonSerializer.Serialize(redemption)));
        }

        await redemptionHandler(user, redemption.Redemption);
    }

    public async Task Initialize()
    {
        foreach (IRedemptionContainer redemptionContainer in redemptionContainers)
        {
            try
            {
                await redemptionContainer.RegisterHandler(redemptionHandlers);
            }
            catch (Exception ex)
            {
                errorHandler.LogSystemException(ex);
            }
        }
    }

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                logWriterChannel.TryComplete();

                logHandlerTask.Wait(2_000);

                if (handledRedemptionLog.IsValueCreated)
                {
                    handledRedemptionLog.Value.Dispose();
                }

                if (unhandledRedemptionLog.IsValueCreated)
                {
                    unhandledRedemptionLog.Value.Dispose();
                }
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

    #endregion IDisposable
}
