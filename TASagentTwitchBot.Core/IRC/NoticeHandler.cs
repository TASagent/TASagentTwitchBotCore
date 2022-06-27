namespace TASagentTwitchBot.Core.IRC;

[AutoRegister]
public interface INoticeHandler
{
    void HandleIRCNotice(IRCMessage message);
}

public class NoticeHandler : INoticeHandler
{
    protected readonly ICommunication communication;
    protected readonly Notifications.IRaidHandler raidhandler;
    protected readonly Notifications.ISubscriptionHandler subhandler;
    protected readonly Notifications.IGiftSubHandler giftSubHandler;

    public NoticeHandler(
        ICommunication communication,
        Notifications.ISubscriptionHandler subhandler,
        Notifications.IRaidHandler raidhandler,
        Notifications.IGiftSubHandler giftSubHandler)
    {
        this.communication = communication;
        this.subhandler = subhandler;
        this.raidhandler = raidhandler;
        this.giftSubHandler = giftSubHandler;
    }

    public void HandleIRCNotice(IRCMessage message)
    {
        string noticeType = message.tags!["msg-id"];

        switch (noticeType)
        {
            case "sub":
            case "resub":
                HandleSub(message);
                break;

            case "subgift":
                HandleGift(message);
                break;

            case "anonsubgift":
                HandleAnonGift(message);
                break;

            case "raid":
                HandleRaid(message);
                break;

            case "submysterygift":
            case "giftpaidupgrade":
            case "rewardgift":
            case "anongiftpaidupgrade":
            case "unraid":
            case "ritual":
            case "bitsbadgetier":
            case "host_on":
            case "host_off":
            case "host_target_went_offline":
                HandleGenericNotice(message);
                break;

            default:
                communication.SendDebugMessage($"Unsupported NoticeType: {noticeType}");
                HandleGenericNotice(message);
                break;
        }
    }

    protected virtual void HandleRaid(IRCMessage ircMessage)
    {
        raidhandler.HandleRaid(
            raiderId: ircMessage.tags!["user-id"],
            count: int.Parse(ircMessage.tags["msg-param-viewerCount"]),
            approved: true);
    }

    protected virtual void HandleSub(IRCMessage ircMessage)
    {
        subhandler.HandleSubscription(
            userId: ircMessage.tags!["user-id"],
            message: ircMessage.message ?? "",
            monthCount: int.Parse(ircMessage.tags["msg-param-cumulative-months"]),
            tier: PlanToTier(ircMessage.tags["msg-param-sub-plan"]),
            approved: true);
    }

    protected virtual void HandleGift(IRCMessage ircMessage)
    {
        giftSubHandler.HandleGiftSub(
            senderId: ircMessage.tags!["user-id"],
            recipientId: ircMessage.tags["msg-param-recipient-id"],
            tier: PlanToTier(ircMessage.tags["msg-param-sub-plan"]),
            months: int.Parse(ircMessage.tags["msg-param-gift-months"]),
            approved: true);
    }

    protected virtual void HandleAnonGift(IRCMessage ircMessage)
    {
        giftSubHandler.HandleAnonGiftSub(
            recipientId: ircMessage.tags!["msg-param-recipient-id"],
            tier: PlanToTier(ircMessage.tags["msg-param-sub-plan"]),
            months: int.Parse(ircMessage.tags["msg-param-gift-months"]),
            approved: true);
    }

    protected virtual void HandleGenericNotice(IRCMessage ircMessage)
    {
        communication.SendDebugMessage($"Notice: {ircMessage}");
    }

    protected int PlanToTier(string plan)
    {
        switch (plan)
        {
            case "Prime": return 0;
            case "1000": return 1;
            case "2000": return 2;
            case "3000": return 3;
            default:
                communication.SendErrorMessage($"Unexpected SubscrptionPlan Value: {plan}");
                return -1;
        }
    }
}
