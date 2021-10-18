using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using TASagentTwitchBot.Core.API.Twitch;
using TASagentTwitchBot.Core.EventSub;
using TASagentTwitchBot.Core.WebServer.API.Twitch;
using TASagentTwitchBot.Core.WebServer.Models;

namespace TASagentTwitchBot.Core.WebServer.EventSub
{
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
        private readonly IHubContext<Web.Hubs.BotHub> botHub;

        private readonly Dictionary<SubKey, SubAttempt> pendingSubs = new Dictionary<SubKey, SubAttempt>();
        private readonly Dictionary<string, SubKey> pendingSubIds = new Dictionary<string, SubKey>();

        //5 minutes
        private static readonly TimeSpan minWaitDuration = new TimeSpan(0, 5, 0);

        public ServerEventSubHandler(
            Config.WebServerConfig webServerConfig,
            ILogger<ServerEventSubHandler> logger,
            HelixEventSubHelper eventSubHelper,
            IHubContext<Web.Hubs.BotHub> botHub)
        {
            this.webServerConfig = webServerConfig;
            this.logger = logger;
            this.eventSubHelper = eventSubHelper;
            this.botHub = botHub;
        }

        public async Task ReportDesiredEventSubs(ApplicationUser user, HashSet<string> subTypes)
        {
            logger.LogInformation($"{user.TwitchBroadcasterName} Sent requested subscription types: {string.Join(", ", subTypes)}");

            //Request existing subs

            foreach (string subType in subTypes)
            {
                string after = null;
                TwitchGetSubscriptionsResponse subResponse;
                bool found = false;

                if (EventSubHandler.GetEventSubConditionType(subType) != EventSubConditionType.BroadcasterUserId)
                {
                    logger.LogWarning($"User {user.TwitchBroadcasterName} requested currently unsupported subtype {subType}");
                    continue;
                }

                do
                {
                    //Iterate over pages
                    subResponse = await eventSubHelper.GetSubscriptions(
                        subscriptionType: subType,
                        after: after);

                    if (subResponse is null)
                    {
                        logger.LogWarning($"Aborting attempt to Verify subscriptions for user {user.TwitchBroadcasterName}");
                        return;
                    }

                    foreach (TwitchSubscriptionDatum sub in subResponse.Data)
                    {
                        //Iterate over subs
                        if (sub.Condition?.BroadcasterUserId == user.TwitchBroadcasterId)
                        {
                            if (sub.Status != "enabled")
                            {
                                logger.LogWarning($"Found matching sub, but its status is {sub.Status}");
                            }

                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        break;
                    }

                    after = subResponse.Pagination?.Cursor;
                }
                while (after != null);

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
            logger.LogInformation($"{user.TwitchBroadcasterName} Requested subscription to event {subType}");

            SubKey key = new SubKey(user.TwitchBroadcasterId, subType);

            if (pendingSubs.ContainsKey(key))
            {
                if (DateTime.Now - pendingSubs[key].SubmissionTime < minWaitDuration)
                {
                    //It hasn't been long enough.
                    logger.LogWarning($"{user.TwitchBroadcasterName} Requesting sub to {subType} too frequently.");
                    return;
                }

                //It has been adequately long
                pendingSubIds.Remove(pendingSubs[key].SubId);
                pendingSubs.Remove(key);
            }

            TwitchSubscribeResponse response = await eventSubHelper.Subscribe(
                subscriptionType: subType,
                condition: new Condition(BroadcasterUserId: user.TwitchBroadcasterId),
                transport: new Transport("webhook", $"{webServerConfig.ExternalAddress}/TASagentServerAPI/EventSub/Event/{user.Id}", user.SubscriptionSecret));

            if (response is not null && response.Data.Length > 0)
            {
                pendingSubs.Add(key, new SubAttempt(response.Data[0].Id, DateTime.Now));
                pendingSubIds.Add(response.Data[0].Id, key);
            }
        }


        public async Task ReportUndesiredEventSub(
            ApplicationUser user,
            string subId)
        {
            logger.LogInformation($"{user.TwitchBroadcasterName} Requested unsubscription from event {subId}");

            TwitchDeleteSubscriptionResponse response = await eventSubHelper.DeleteSubscription(subId);

            if (response != TwitchDeleteSubscriptionResponse.Success)
            {
                logger.LogWarning($"Unsubscription from event {subId} for user {user.TwitchBroadcasterName} failed with {response}.");
            }
        }

        public async void HandleEventPayload(
            ApplicationUser user,
            TwitchEventSubPayload payload)
        {
            //Do the thing
            logger.LogInformation($"{user.TwitchBroadcasterName} Payload Received: {JsonSerializer.Serialize(payload)}");

            await botHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
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
                logger.LogInformation($"Caught exception when trying to verify hash: {e}");
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
}
