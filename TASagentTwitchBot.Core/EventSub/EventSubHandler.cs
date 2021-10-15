﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace TASagentTwitchBot.Core.EventSub
{
    public delegate Task EventHandler(JsonElement twitchEvent);

    public interface IEventSubSubscriber
    {
        void RegisterHandlers(Dictionary<string, EventHandler> handlers);
    }

    public class EventSubHandler : IDisposable
    {
        private readonly EventSubConfig eventSubConfig;
        private readonly ICommunication communication;
        private readonly HubConnection serverHubConnection;
        private readonly ErrorHandler errorHandler;

        //Event handlers
        private readonly IEventSubSubscriber[] eventSubSubscribers;

        private readonly Dictionary<string, EventHandler> eventHandlers = new Dictionary<string, EventHandler>();


        private bool disposedValue;

        public EventSubHandler(
            EventSubConfig eventSubConfig,
            ICommunication communication,
            ErrorHandler errorHandler,
            IEnumerable<IEventSubSubscriber> eventSubSubscribers)
        {
            this.eventSubConfig = eventSubConfig;
            this.communication = communication;
            this.errorHandler = errorHandler;

            this.eventSubSubscribers = eventSubSubscribers.ToArray();

            foreach (IEventSubSubscriber subscriber in this.eventSubSubscribers)
            {
                subscriber.RegisterHandlers(eventHandlers);
            }

            if (!string.IsNullOrEmpty(eventSubConfig.ServerAccessToken) &&
                !string.IsNullOrEmpty(eventSubConfig.ServerAddress) &&
                !string.IsNullOrEmpty(eventSubConfig.ServerUserName))
            {
                serverHubConnection = new HubConnectionBuilder()
                    .WithUrl($"{eventSubConfig.ServerAddress}/Hubs/BotSocket", options =>
                    {
                        options.Headers.Add("User-Id", eventSubConfig.ServerUserName);
                        options.Headers.Add("Authorization", $"Bearer {eventSubConfig.ServerAccessToken}");
                    })
                    .WithAutomaticReconnect()
                    .Build();

                serverHubConnection.Closed += ServerHubConnectionClosed;

                serverHubConnection.On<string>("ReceiveMessage", ReceiveMessage);
                serverHubConnection.On<EventSubPayload>("ReceiveEvent", ReceiveEvent);

                Initialize();
            }
            else
            {
                communication.SendErrorMessage($"EventSub not configured, skipping. Register at https://server.tas.wtf/ and contact TASagent. " +
                    $"Then update relevant details in ~/Config/EventSubConfig.json");
            }
        }

        public async void Initialize()
        {
            try
            {
                await serverHubConnection.StartAsync();
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    communication.SendErrorMessage($"EventSub failed to connect due to permissions. Please contact TASagent to be given access.");
                }
                else
                {
                    communication.SendErrorMessage($"EventSub failed to connect. Make sure settings are correct.");
                }

                errorHandler.LogSystemException(ex);
            }
            catch (Exception ex)
            {
                errorHandler.LogSystemException(ex);
                communication.SendErrorMessage($"EventSub failed to connect. Make sure settings are correct.");
                return;
            }

            HashSet<string> requiredEvents = new HashSet<string>(eventHandlers.Keys);

            //Make sure the server subs to relevant events
            await serverHubConnection.InvokeAsync("ReportDesiredEventSubs", requiredEvents);
        }

        private Task ServerHubConnectionClosed(Exception arg)
        {
            errorHandler.LogSystemException(arg);
            return Task.CompletedTask;
        }

        public void ReceiveMessage(string message)
        {
            communication.SendDebugMessage($"WebServer Message: {message}");
        }

        public async Task ReceiveEvent(EventSubPayload eventSubPayload)
        {
            if (eventHandlers.ContainsKey(eventSubPayload.EventType))
            {
                await eventHandlers[eventSubPayload.EventType](eventSubPayload.TwitchEvent);
            }
            else
            {
                communication.SendWarningMessage($"Received undesired EventSub event: {JsonSerializer.Serialize(eventSubPayload)}");
                await serverHubConnection.InvokeAsync("ReportUndesiredEventSub", eventSubPayload.SubId);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (serverHubConnection is not null)
                    {
                        if (serverHubConnection.State != HubConnectionState.Disconnected)
                        {
                            serverHubConnection.StopAsync().Wait();
                        }

                        serverHubConnection.DisposeAsync().AsTask().Wait();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public static EventSubConditionType GetEventSubConditionType(string eventType)
        {
            switch (eventType)
            {
                case "channel.update":
                case "channel.follow":
                case "channel.subscribe":
                case "channel.subscription.end":
                case "channel.subscription.gift":
                case "channel.subscription.message":
                case "channel.cheer":
                case "channel.ban":
                case "channel.unban":
                case "channel.moderator.add":
                case "channel.moderator.remove":
                case "channel.channel_points_custom_reward.add":
                case "channel.poll.begin":
                case "channel.poll.progress":
                case "channel.poll.end":
                case "channel.prediction.begin":
                case "channel.prediction.progress":
                case "channel.prediction.lock":
                case "channel.prediction.end":
                case "channel.goal.begin":
                case "channel.goal.progress":
                case "channel.goal.end":
                case "channel.hype_train.begin":
                case "channel.hype_train.progress":
                case "channel.hype_train.end":
                case "stream.online":
                case "stream.offline":
                    return EventSubConditionType.BroadcasterUserId;

                case "channel.raid":
                    return EventSubConditionType.FromBroadcasterUserId | EventSubConditionType.ToBroadcasterUserId;

                case "channel.channel_points_custom_reward.update":
                case "channel.channel_points_custom_reward.remove":
                case "channel.channel_points_custom_reward_redemption.add":
                case "channel.channel_points_custom_reward_redemption.update":
                    return EventSubConditionType.BroadcasterUserId | EventSubConditionType.RewardId;

                case "user.authorization.grant":
                case "user.authorization.revoke":
                    return EventSubConditionType.ClientId;

                case "user.update":
                    return EventSubConditionType.UserId;

                case "extension.bits_transaction.create":
                    return EventSubConditionType.ExtensionClientId;

                default: throw new NotSupportedException($"Unrecognized eventType: {eventType}");
            }
        }
    }

    [Flags]
    public enum EventSubConditionType
    {
        None = 0,
        BroadcasterUserId = 1,
        ToBroadcasterUserId = 1 << 1,
        FromBroadcasterUserId = 1 << 2,
        RewardId = 1 << 3,
        ClientId = 1 << 4,
        UserId = 1 << 5,
        ExtensionClientId = 1 << 6
    }

    public record EventSubPayload(
        [property: JsonPropertyName("sub_id")] string SubId,
        [property: JsonPropertyName("event_type")] string EventType,
        [property: JsonPropertyName("event")] JsonElement TwitchEvent);
}
