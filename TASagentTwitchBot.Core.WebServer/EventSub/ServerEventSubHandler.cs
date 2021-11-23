using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.API.Twitch;
using TASagentTwitchBot.Core.EventSub;
using TASagentTwitchBot.Core.WebServer.API.Twitch;
using TASagentTwitchBot.Core.WebServer.Models;

namespace TASagentTwitchBot.Core.WebServer.EventSub;

public interface IServerEventSubHandler
{
    Task SubscribeToStandardEvent(ApplicationUser user, string subType);
    Task ReportDesiredEventSubs(ApplicationUser user, HashSet<string> subTypes);
    Task ReportUndesiredEventSub(ApplicationUser user, string subType);


    void HandleEventPayload(ApplicationUser user, TwitchEventSubPayload payload);
    bool VerifyEventSubMessage(string secret, string signature, string message);
    bool HandleSubVerification(string subId);
    bool HandleSubRevocation(string subId);
}

public class ServerEventSubHandler : IServerEventSubHandler
{
    private const string SIGNATURE_PREFIX = "sha256=";

    private readonly Config.WebServerConfig webServerConfig;
    private readonly ILogger<ServerEventSubHandler> logger;
    private readonly HelixEventSubHelper eventSubHelper;
    private readonly IHubContext<Web.Hubs.BotEventSubHub> botEventSubHub;

    private readonly Dictionary<SubKey, SubAttempt> pendingSubs = new Dictionary<SubKey, SubAttempt>();
    private readonly Dictionary<string, SubKey> pendingSubIds = new Dictionary<string, SubKey>();

    //5 minutes
    private static readonly TimeSpan minWaitDuration = new TimeSpan(0, 5, 0);

    public ServerEventSubHandler(
        Config.WebServerConfig webServerConfig,
        ILogger<ServerEventSubHandler> logger,
        HelixEventSubHelper eventSubHelper,
        IHubContext<Web.Hubs.BotEventSubHub> botEventSubHub)
    {
        this.webServerConfig = webServerConfig;
        this.logger = logger;
        this.eventSubHelper = eventSubHelper;
        this.botEventSubHub = botEventSubHub;
    }

    public async Task ReportDesiredEventSubs(ApplicationUser user, HashSet<string> subTypes)
    {
        logger.LogInformation("{TwitchBroadcasterName} Sent requested subscription types: {subTypes}", user.TwitchBroadcasterName, string.Join(", ", subTypes));

        //Request existing subs

        foreach (string subType in subTypes)
        {
            string? after = null;
            TwitchGetSubscriptionsResponse? subResponse;
            bool found = false;

            do
            {
                //Iterate over pages
                subResponse = await eventSubHelper.GetSubscriptions(
                    subscriptionType: subType,
                    after: after);

                if (subResponse is null)
                {
                    logger.LogWarning("Aborting attempt to Verify subscriptions for user {TwitchBroadcasterName}", user.TwitchBroadcasterName);
                    return;
                }

                foreach (TwitchSubscriptionDatum sub in subResponse.Data)
                {
                    //Iterate over subs
                    if (sub.Condition?.BroadcasterUserId == user.TwitchBroadcasterId)
                    {
                        if (sub.Status == "enabled")
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                {
                    break;
                }

                after = subResponse!.Pagination?.Cursor;
            }
            while (after is not null);

            if (!found)
            {
                //Subscribe
                await SubscribeToStandardEvent(user, subType);
            }
        }
    }

    /// <summary>
    /// Subscribe to a Standard Event, like "channel.follow", where the condition just relies on a BroadcasterUserId
    /// </summary>
    public async Task SubscribeToStandardEvent(
        ApplicationUser user,
        string subType)
    {
        if (string.IsNullOrEmpty(user.TwitchBroadcasterName) || string.IsNullOrEmpty(user.TwitchBroadcasterId))
        {
            logger.LogWarning("Subscription request from {UserName} with blank or null Broadcaster Name or Id.", user.UserName);
            return;
        }


        logger.LogInformation("{TwitchBroadcasterName} Requested subscription to event {SubType}", user.TwitchBroadcasterName, subType);

        SubKey key = new SubKey(user.TwitchBroadcasterId, subType);

        if (pendingSubs.ContainsKey(key))
        {
            if (DateTime.Now - pendingSubs[key].SubmissionTime < minWaitDuration)
            {
                //It hasn't been long enough.
                logger.LogWarning("{TwitchBroadcasterName} Requesting sub to {SubType} too frequently.", user.TwitchBroadcasterName, subType);

                await botEventSubHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
                    method: "ReceiveWarning",
                    arg1: $"Requesting sub to {subType} too frequently. Await pending sub request.");

                return;
            }

            //It has been adequately long
            pendingSubIds.Remove(pendingSubs[key].SubId);
            pendingSubs.Remove(key);
        }

        //Get information about the structure of the Condition
        try
        {
            EventSubConditionType conditionType = EventSubHandler.GetEventSubConditionType(subType);
            Condition condition;

            if (conditionType == EventSubConditionType.BroadcasterUserId)
            {
                //Condition is Only BroadcasterUserId
                condition = new Condition(BroadcasterUserId: user.TwitchBroadcasterId);
            }
            else if ((conditionType & EventSubConditionType.BroadcasterUserId) == EventSubConditionType.BroadcasterUserId)
            {
                //Condition Includes BroadcasterUserId (like Channel Point Redemptions)
                condition = new Condition(BroadcasterUserId: user.TwitchBroadcasterId);
            }
            else if ((conditionType & EventSubConditionType.ToBroadcasterUserId) == EventSubConditionType.ToBroadcasterUserId)
            {
                //Condition Includes ToBroadcasterUserId (like Raid)
                condition = new Condition(ToBroadcasterUserId: user.TwitchBroadcasterId);
            }
            else if (conditionType == EventSubConditionType.UserId)
            {
                condition = new Condition(UserId: user.TwitchBroadcasterId);
            }
            else
            {
                throw new NotSupportedException($"Unrecognized ConditionType for EventSub {subType}: {conditionType}");
            }

            TwitchSubscribeResponse? response = await eventSubHelper.Subscribe(
                subscriptionType: subType,
                condition: condition,
                transport: new Transport("webhook", $"{webServerConfig.ExternalAddress}/TASagentServerAPI/EventSub/Event/{user.Id}", user.SubscriptionSecret));

            if (response is not null && response.Data.Length > 0)
            {
                pendingSubs.Add(key, new SubAttempt(response.Data[0].Id, DateTime.Now));
                pendingSubIds.Add(response.Data[0].Id, key);
            }
        }
        catch (NotSupportedException ex)
        {
            logger.LogWarning("{TwitchBroadcasterName} Requesting sub to unsupported {SubType}: {Message}", user.TwitchBroadcasterName, subType, ex.Message);
            await botEventSubHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
                method: "ReceiveWarning",
                arg1: $"Unable to subscribe to event {subType}: {ex.Message}");
        }
    }


    public async Task ReportUndesiredEventSub(
        ApplicationUser user,
        string subId)
    {
        logger.LogInformation("{TwitchBroadcasterName} Requested unsubscription from event {SubId}", user.TwitchBroadcasterName, subId);

        TwitchDeleteSubscriptionResponse response = await eventSubHelper.DeleteSubscription(subId);

        if (response != TwitchDeleteSubscriptionResponse.Success)
        {
            logger.LogWarning("Unsubscription from event {SubId} for user {TwitchBroadcasterName} failed with {Response}.", subId, user.TwitchBroadcasterName, response);
        }
    }

    public async void HandleEventPayload(
        ApplicationUser user,
        TwitchEventSubPayload payload)
    {
        if (string.IsNullOrEmpty(user.TwitchBroadcasterId))
        {
            logger.LogWarning("Subscription Event Payload for {UserName} with blank or null Broadcaster Name or Id.", user.UserName);
            return;
        }

        //Do the thing
        await botEventSubHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
            method: "ReceiveEvent",
            arg1: new EventSubPayload(
                SubId: payload.Subscription.Id,
                EventType: payload.Subscription.Type,
                TwitchEvent: payload.TwitchEvent));
    }

    public bool VerifyEventSubMessage(
        string secret,
        string signature,
        string message)
    {
        try
        {
            byte[] secretBytes = Encoding.ASCII.GetBytes(secret);
            using HMACSHA256 hasher = new HMACSHA256(secretBytes);
            byte[] result = hasher.ComputeHash(Encoding.ASCII.GetBytes(message));
            string stringified = BitConverter.ToString(result).Replace("-", "").ToUpper();

            if (signature.StartsWith(SIGNATURE_PREFIX))
            {
                signature = signature[SIGNATURE_PREFIX.Length..];
            }

            return signature.Equals(stringified, StringComparison.OrdinalIgnoreCase);

        }
        catch (Exception e)
        {
            logger.LogInformation("Caught exception when trying to verify hash: {e}", e.ToString());
            return false;
        }
    }

    public bool HandleSubVerification(string subId)
    {
        if (pendingSubIds.ContainsKey(subId))
        {
            //Verified sub

            //Clean up pending references
            pendingSubs.Remove(pendingSubIds[subId]);
            pendingSubIds.Remove(subId);

            return true;
        }

        //We were not waiting for this sub
        return false;
    }

    public bool HandleSubRevocation(string subId)
    {
        //We don't current track these, just say okay
        return true;
    }

    private record SubKey(string UserId, string SubType);
    private record SubAttempt(string SubId, DateTime SubmissionTime);
}
