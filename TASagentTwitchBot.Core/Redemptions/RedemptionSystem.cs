using System.Text.Json;
using System.Threading.Channels;
using TASagentTwitchBot.Core.EventSub;

namespace TASagentTwitchBot.Core.Redemptions;


public delegate Task RedemptionHandler(Database.User user, RedemptionData redemption);

[AutoRegister]
public interface IRedemptionContainer
{
    Task RegisterHandler(Dictionary<string, RedemptionHandler> handlers);
}


public class RedemptionSystem : IEventSubSubscriber, IDisposable
{
    private readonly Config.BotConfiguration botConfig;
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

    private readonly Task logHandlerTask;

    private bool disposedValue;

    public RedemptionSystem(
        Config.BotConfiguration botConfig,
        ErrorHandler errorHandler,
        ICommunication communication,
        IEnumerable<IRedemptionContainer> redemptionContainers,
        Database.IUserHelper userHelper)
    {
        this.botConfig = botConfig;
        this.errorHandler = errorHandler;
        this.communication = communication;

        this.userHelper = userHelper;

        this.redemptionContainers = redemptionContainers.ToArray();

        Channel<(bool, string)> logChannel = Channel.CreateUnbounded<(bool, string)>();
        logWriterChannel = logChannel.Writer;
        logReaderChannel = logChannel.Reader;

        if (botConfig.ExhaustiveRedemptionLogging)
        {
            logHandlerTask = Task.Run(HandleLogs);
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

    public async Task RegisterHandlers(Dictionary<string, EventSub.EventHandler> handlers)
    {
        await Initialize();

        handlers.Add("channel.channel_points_custom_reward_redemption.add", HandleRedemption);
    }

    public async Task HandleRedemption(JsonElement twitchEvent)
    {
        RedemptionData redemptionData = new RedemptionData(
            RedemptionId: twitchEvent.GetProperty("id").GetString()!,
            BroadcasterUserId: twitchEvent.GetProperty("broadcaster_user_id").GetString()!,
            BroadcasterUserLogin: twitchEvent.GetProperty("broadcaster_user_login").GetString()!,
            BroadcasterUserName: twitchEvent.GetProperty("broadcaster_user_name").GetString()!,
            UserId: twitchEvent.GetProperty("user_id").GetString()!,
            UserLogin: twitchEvent.GetProperty("user_login").GetString()!,
            UserName: twitchEvent.GetProperty("user_name").GetString()!,
            UserInput: twitchEvent.GetProperty("user_input").GetString()!,
            Status: twitchEvent.GetProperty("status").GetString()!,
            RedeemedAt: twitchEvent.GetProperty("redeemed_at").GetDateTime()!,
            RewardData: new RedemptionData.Reward(
                Id: twitchEvent.GetProperty("reward").GetProperty("id").GetString()!,
                Title: twitchEvent.GetProperty("reward").GetProperty("title").GetString()!,
                Cost: twitchEvent.GetProperty("reward").GetProperty("cost").GetInt32()!,
                Prompt: twitchEvent.GetProperty("reward").GetProperty("prompt").GetString()!));



        //Handle redemption
        string rewardID = redemptionData.RewardData.Id;

        if (!redemptionHandlers.TryGetValue(rewardID, out RedemptionHandler? redemptionHandler))
        {
            if (botConfig.ExhaustiveRedemptionLogging)
            {
                logWriterChannel.TryWrite((false, $"*** Handler Not Found:\n{JsonSerializer.Serialize(redemptionData)}"));
            }

            communication.SendErrorMessage($"Redemption handler not found: {rewardID}");
            return;
        }

        Database.User? user = await userHelper.GetUserByTwitchId(redemptionData.UserId);

        if (user is null)
        {
            if (botConfig.ExhaustiveRedemptionLogging)
            {
                logWriterChannel.TryWrite((false, $"*** User Not Found:\n{JsonSerializer.Serialize(redemptionData)}"));
            }

            communication.SendErrorMessage($"User not found: {redemptionData.UserId}");
            return;
        }

        if (botConfig.ExhaustiveRedemptionLogging)
        {
            logWriterChannel.TryWrite((true, JsonSerializer.Serialize(redemptionData)));
        }

        await redemptionHandler(user, redemptionData);
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


public record RedemptionData(
    string RedemptionId,
    string BroadcasterUserId,
    string BroadcasterUserLogin,
    string BroadcasterUserName,
    string UserId,
    string UserLogin,
    string UserName,
    string UserInput,
    string Status,
    DateTime RedeemedAt,
    RedemptionData.Reward RewardData)
{
    public record Reward(
        string Id,
        string Title,
        int Cost,
        string Prompt);
}