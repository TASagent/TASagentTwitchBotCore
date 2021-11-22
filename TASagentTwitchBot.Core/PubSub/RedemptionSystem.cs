namespace TASagentTwitchBot.Core.PubSub;

public interface IRedemptionSystem
{
    Task Initialize();
    void HandleRedemption(ChannelPointMessageData.Datum redemption);
}

public class RedemptionSystem : IRedemptionSystem
{
    private readonly ICommunication communication;

    private readonly IRedemptionContainer[] redemptionContainers;
    private readonly Database.IUserHelper userHelper;

    private readonly Dictionary<string, RedemptionHandler> redemptionHandlers = new Dictionary<string, RedemptionHandler>();

    public RedemptionSystem(
        ICommunication communication,
        IEnumerable<IRedemptionContainer> redemptionContainers,
        Database.IUserHelper userHelper)
    {
        this.communication = communication;

        this.userHelper = userHelper;

        this.redemptionContainers = redemptionContainers.ToArray();
    }

    public async void HandleRedemption(ChannelPointMessageData.Datum redemption)
    {
        //Handle redemption
        string rewardID = redemption.Redemption.Reward.Id;

        if (!redemptionHandlers.TryGetValue(rewardID, out RedemptionHandler? redemptionHandler))
        {
            communication.SendErrorMessage($"Redemption handler not found: {rewardID}");
            return;
        }

        Database.User? user = await userHelper.GetUserByTwitchId(redemption.Redemption.User.Id);

        if (user is null)
        {
            communication.SendErrorMessage($"User not found: {redemption.Redemption.User.Id}");
            return;
        }

        await redemptionHandler(user, redemption.Redemption);
    }

    public async Task Initialize()
    {
        foreach (IRedemptionContainer redemptionContainer in redemptionContainers)
        {
            await redemptionContainer.RegisterHandler(redemptionHandlers);
        }
    }
}

public delegate Task RedemptionHandler(Database.User user, ChannelPointMessageData.Datum.RedemptionData redemption);

public interface IRedemptionContainer
{
    Task RegisterHandler(Dictionary<string, RedemptionHandler> handlers);
}
