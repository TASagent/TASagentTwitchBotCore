using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using TASagentTwitchBot.Core.TTS;

namespace TASagentTwitchBot.Core.Notifications
{
    public class FullActivityProvider :
        ISubscriptionHandler,
        ICheerHandler,
        IRaidHandler,
        IGiftSubHandler,
        IFollowerHandler,
        ITTSHandler,
        IDisposable
    {
        protected readonly ICommunication communication;
        protected readonly IActivityDispatcher activityDispatcher;
        protected readonly Audio.ISoundEffectSystem soundEffectSystem;
        protected readonly Audio.IAudioPlayer audioPlayer;
        protected readonly Audio.Effects.IAudioEffectSystem audioEffectSystem;
        protected readonly ITTSRenderer ttsRenderer;
        protected readonly NotificationServer notificationServer;
        protected readonly Bits.CheerHelper cheerHelper;

        protected readonly Database.IUserHelper userHelper;

        protected readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();

        protected readonly HashSet<string> followedUserIds = new HashSet<string>();

        private bool disposedValue;

        public FullActivityProvider(
            ICommunication communication,
            Audio.ISoundEffectSystem soundEffectSystem,
            Audio.IAudioPlayer audioPlayer,
            Audio.Effects.IAudioEffectSystem audioEffectSystem,
            Bits.CheerHelper cheerHelper,
            IActivityDispatcher activityDispatcher,
            ITTSRenderer ttsRenderer,
            NotificationServer notificationServer,
            Database.IUserHelper userHelper)
        {
            this.communication = communication;

            this.soundEffectSystem = soundEffectSystem;
            this.audioEffectSystem = audioEffectSystem;
            this.audioPlayer = audioPlayer;
            this.cheerHelper = cheerHelper;

            this.activityDispatcher = activityDispatcher;
            this.ttsRenderer = ttsRenderer;
            this.notificationServer = notificationServer;

            this.userHelper = userHelper;
        }

        protected virtual Task Execute(FullActivityRequest activityRequest)
        {
            List<Task> taskList = new List<Task>();

            if (activityRequest.NotificationMessage is not null)
            {
                taskList.Add(notificationServer.ShowNotificationAsync(activityRequest.NotificationMessage));
            }

            if (activityRequest.AudioRequest is not null)
            {
                taskList.Add(audioPlayer.PlayAudioRequest(activityRequest.AudioRequest));
            }

            if (activityRequest.MarqueeMessage is not null)
            {
                //Don't bother waiting on this one to complete
                taskList.Add(notificationServer.ShowTTSMessageAsync(activityRequest.MarqueeMessage));
            }

            return Task.WhenAll(taskList).WithCancellation(generalTokenSource.Token);
        }

        #region ISubscriptionHandler

        public virtual async void HandleSubscription(
            string userId,
            string message,
            int monthCount,
            int tier,
            bool approved)
        {
            Database.User subscriber = await userHelper.GetUserByTwitchId(userId);

            if (subscriber == null)
            {
                communication.SendErrorMessage($"Unable to find user {userId} for Subscription handling");
                return;
            }

            communication.NotifyEvent($"Tier {tier} Sub: {subscriber.TwitchUserName}");

            string chatResponse = await GetSubscriberChatResponse(subscriber, message, monthCount, tier);
            if (!string.IsNullOrWhiteSpace(chatResponse))
            {
                communication.SendPublicChatMessage(chatResponse);
            }

            activityDispatcher.QueueActivity(
                activity: new FullActivityRequest(
                    fullActivityProvider: this,
                    description: $"Sub: {subscriber.TwitchUserName}: {message ?? ""}",
                    notificationMessage: await GetSubscriberNotificationRequest(subscriber, message, monthCount, tier),
                    audioRequest: await GetSubscriberAudioRequest(subscriber, message, monthCount, tier),
                    marqueeMessage: await GetSubscriberMarqueeMessage(subscriber, message, monthCount, tier)),
                approved: approved);
        }

        protected virtual Task<string> GetSubscriberChatResponse(
            Database.User subscriber,
            string message,
            int monthCount,
            int tier)
        {
            if (monthCount <= 1)
            {
                return Task.FromResult($"Holy Cow! Thanks for the sub, {subscriber.TwitchUserName}!");
            }

            return Task.FromResult($"Holy Cow! Thanks for {monthCount} months, {subscriber.TwitchUserName}!");
        }

        protected virtual Task<NotificationMessage> GetSubscriberNotificationRequest(
            Database.User subscriber,
            string message,
            int monthCount,
            int tier)
        {
            return Task.FromResult<NotificationMessage>(new ImageNotificationMessage(
                image: notificationServer.GetNextImageURL(),
                duration: 5000,
                message: GetSubscriberNotificationMessage(subscriber, message, monthCount, tier)));
        }

        protected virtual async Task<Audio.AudioRequest> GetSubscriberAudioRequest(
            Database.User subscriber,
            string message,
            int monthCount,
            int tier)
        {
            Audio.AudioRequest soundEffectRequest = null;
            Audio.AudioRequest ttsRequest = null;

            if (soundEffectSystem.HasSoundEffects())
            {
                Audio.SoundEffect subSoundEffect = soundEffectSystem.GetSoundEffectByName("SMW PowerUp");
                if (subSoundEffect is null)
                {
                    communication.SendWarningMessage($"Expected Sub SoundEffect not found.  Defaulting to first sound effect.");
                    subSoundEffect = soundEffectSystem.GetSoundEffectByName(soundEffectSystem.GetSoundEffects()[0]);
                }

                soundEffectRequest = new Audio.SoundEffectRequest(subSoundEffect);
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                ttsRequest = await ttsRenderer.TTSRequest(
                    voicePreference: subscriber.TTSVoicePreference,
                    pitchPreference: subscriber.TTSPitchPreference,
                    speedPreference: subscriber.TTSSpeedPreference,
                    effectsChain: audioEffectSystem.SafeParse(subscriber.TTSEffectsChain),
                    ttsText: message);
            }

            return JoinRequests(300, soundEffectRequest, ttsRequest);
        }

        protected virtual Task<MarqueeMessage> GetSubscriberMarqueeMessage(
            Database.User subscriber,
            string message,
            int monthCount,
            int tier)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Task.FromResult<MarqueeMessage>(null);
            }

            return Task.FromResult(new MarqueeMessage(subscriber.TwitchUserName, message, subscriber.Color));
        }

        protected virtual string GetSubscriberNotificationMessage(
            Database.User subscriber,
            string message,
            int monthCount,
            int tier)
        {
            string fontColor = subscriber.Color;
            if (string.IsNullOrWhiteSpace(fontColor))
            {
                fontColor = "#0000FF";
            }

            if (monthCount == 1)
            {
                switch (tier)
                {
                    case 0: return $"Thank you for the brand new Prime Gaming sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber.TwitchUserName)}</span>!";
                    case 1: return $"Thank you for the brand new sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber.TwitchUserName)}</span>!";
                    case 2: return $"Thank you for the brand new tier 2 sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber.TwitchUserName)}</span>!";
                    case 3: return $"Thank you for the brand new tier 3 sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber.TwitchUserName)}</span>!";
                    default:
                        BGC.Debug.LogError($"Unexpected SubscriberNotification {tier} tier, {monthCount} months.");
                        return $"Thank you for the brand new sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber.TwitchUserName)}</span>!";
                }
            }
            else
            {
                switch (tier)
                {
                    case 0: return $"Thank you for subscribing for {monthCount} months with Prime Gaming, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber.TwitchUserName)}</span>!";
                    case 1: return $"Thank you for subscribing for {monthCount} months, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber.TwitchUserName)}</span>!";
                    case 2: return $"Thank you for subscribing at tier 2 for {monthCount} months, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber.TwitchUserName)}</span>!";
                    case 3: return $"Thank you for subscribing at tier 3 for {monthCount} months, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber.TwitchUserName)}</span>!";
                    default:
                        BGC.Debug.LogError($"Unexpected SubscriberNotification {tier} tier, {monthCount} months.");
                        return $"Thank you for subscribing for {monthCount} months, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber.TwitchUserName)}</span>!";
                }
            }
        }

        #endregion ISubscriptionHandler
        #region ICheerHandler

        public virtual async void HandleCheer(
            Database.User cheerer,
            string message,
            int quantity,
            bool approved)
        {
            communication.NotifyEvent($"Cheer {quantity}: {cheerer.TwitchUserName}");

            string chatResponse = await GetCheerChatResponse(cheerer, message, quantity);
            if (!string.IsNullOrWhiteSpace(chatResponse))
            {
                communication.SendPublicChatMessage(chatResponse);
            }

            activityDispatcher.QueueActivity(
                activity: new FullActivityRequest(
                    fullActivityProvider: this,
                    description: $"User {cheerer.TwitchUserName} cheered {quantity} bits: {message}",
                    notificationMessage: await GetCheerNotificationRequest(cheerer, message, quantity),
                    audioRequest: await GetCheerAudioRequest(cheerer, message, quantity),
                    marqueeMessage: await GetCheerMarqueeMessage(cheerer, message, quantity)),
                approved: approved);
        }

        protected virtual Task<string> GetCheerChatResponse(
            Database.User cheerer,
            string message,
            int quantity)
        {
            return Task.FromResult<string>(null);
        }

        protected virtual async Task<NotificationMessage> GetCheerNotificationRequest(
            Database.User cheerer,
            string message,
            int quantity)
        {
            return new ImageNotificationMessage(
                image: await cheerHelper.GetCheerImageURL(message, quantity),
                duration: 10_000,
                message: GetCheerMessage(cheerer, message, quantity));
        }

        protected virtual string GetCheerMessage(
            Database.User cheerer,
            string message,
            int quantity)
        {
            string fontColor = cheerer.Color;
            if (string.IsNullOrWhiteSpace(fontColor))
            {
                fontColor = "#0000FF";
            }

            return $"<span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(cheerer.TwitchUserName)}</span> has cheered {quantity} {(quantity == 1 ? "bit" : "bits")}: {HttpUtility.HtmlEncode(message)}";
        }

        protected virtual async Task<Audio.AudioRequest> GetCheerAudioRequest(
            Database.User cheerer,
            string message,
            int quantity)
        {
            Audio.AudioRequest soundEffectRequest = null;
            Audio.AudioRequest ttsRequest = null;

            if (soundEffectSystem.HasSoundEffects())
            {
                Audio.SoundEffect cheerSoundEffect = soundEffectSystem.GetSoundEffectByName("FF7 Purchase");
                if (cheerSoundEffect is null)
                {
                    communication.SendWarningMessage($"Expected Cheer SoundEffect not found.  Defaulting to first");
                    cheerSoundEffect = soundEffectSystem.GetSoundEffectByName(soundEffectSystem.GetSoundEffects()[0]);
                }

                soundEffectRequest = new Audio.SoundEffectRequest(cheerSoundEffect);
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                ttsRequest = await ttsRenderer.TTSRequest(
                    voicePreference: cheerer.TTSVoicePreference,
                    pitchPreference: cheerer.TTSPitchPreference,
                    speedPreference: cheerer.TTSSpeedPreference,
                    effectsChain: audioEffectSystem.SafeParse(cheerer.TTSEffectsChain),
                    ttsText: message);
            }

            return JoinRequests(300, soundEffectRequest, ttsRequest);
        }

        protected virtual Task<MarqueeMessage> GetCheerMarqueeMessage(
            Database.User cheerer,
            string message,
            int quantity)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Task.FromResult<MarqueeMessage>(null);
            }

            return Task.FromResult(new MarqueeMessage(cheerer.TwitchUserName, message, cheerer.Color));
        }

        #endregion ICheerHandler
        #region IRaidHandler

        public virtual async void HandleRaid(
            string raiderId,
            int count,
            bool approved)
        {
            Database.User raider = await userHelper.GetUserByTwitchId(raiderId);

            if (raider == null)
            {
                communication.SendErrorMessage($"Unable to find user {raiderId} for Raid handling");
                return;
            }

            communication.NotifyEvent($"{count} Raid: {raider.TwitchUserName}");

            string chatResponse = await GetRaidChatResponse(raider, count);
            if (!string.IsNullOrWhiteSpace(chatResponse))
            {
                communication.SendPublicChatMessage(chatResponse);
            }

            activityDispatcher.QueueActivity(
                activity: new FullActivityRequest(
                    fullActivityProvider: this,
                    description: $"Raid: {raider.TwitchUserName} with {count} viewers",
                    notificationMessage: await GetRaidNotificationRequest(raider, count),
                    audioRequest: await GetRaidAudioRequest(raider, count),
                    marqueeMessage: await GetRaidMarqueeMessage(raider, count)),
                approved: approved);
        }

        protected virtual Task<string> GetRaidChatResponse(
            Database.User raider,
            int count)
        {
            return Task.FromResult($"Wow! {raider.TwitchUserName} has Raided with {count} viewers! PogChamp");
        }

        protected virtual Task<NotificationMessage> GetRaidNotificationRequest(
            Database.User raider,
            int count)
        {
            return Task.FromResult<NotificationMessage>(new ImageNotificationMessage(
                image: notificationServer.GetNextImageURL(),
                duration: 10_000,
                message: $"WOW! {count} raiders incoming from {HttpUtility.HtmlEncode(raider.TwitchUserName)}!"));
        }

        protected virtual Task<Audio.AudioRequest> GetRaidAudioRequest(
            Database.User raider,
            int count)
        {
            Audio.AudioRequest soundEffectRequest = null;

            if (soundEffectSystem.HasSoundEffects())
            {
                Audio.SoundEffect raidSoundEffect = soundEffectSystem.GetSoundEffectByName("SMW CastleClear");
                if (raidSoundEffect is null)
                {
                    communication.SendWarningMessage($"Expected Raid SoundEffect not found.  Defaulting to first");
                    raidSoundEffect = soundEffectSystem.GetSoundEffectByName(soundEffectSystem.GetSoundEffects()[0]);
                }

                soundEffectRequest = new Audio.SoundEffectRequest(raidSoundEffect);
            }

            return Task.FromResult(soundEffectRequest);
        }

        protected virtual Task<MarqueeMessage> GetRaidMarqueeMessage(
            Database.User raider,
            int count)
        {
            return Task.FromResult<MarqueeMessage>(null);
        }

        #endregion IRaidHandler
        #region IGiftSubHandler

        public virtual async void HandleGiftSub(
            string senderId,
            string recipientId,
            int tier,
            int months,
            bool approved)
        {
            Database.User sender = await userHelper.GetUserByTwitchId(senderId);
            Database.User recipient = await userHelper.GetUserByTwitchId(recipientId);

            if (sender == null)
            {
                communication.SendErrorMessage($"Unable to find sender {senderId} for Gift Sub handling");
                return;
            }

            if (recipient == null)
            {
                communication.SendErrorMessage($"Unable to find reciever {recipientId} for Gift Sub handling");
                return;
            }

            communication.NotifyEvent($"Gift Sub from {sender.TwitchUserName} to {recipient.TwitchUserName}");

            string chatResponse = await GetGiftSubChatResponse(sender, recipient, tier, months);
            if (!string.IsNullOrWhiteSpace(chatResponse))
            {
                communication.SendPublicChatMessage(chatResponse);
            }

            activityDispatcher.QueueActivity(
                activity: new FullActivityRequest(
                    fullActivityProvider: this,
                    description: $"Gift Sub To: {recipient.TwitchUserName}",
                    notificationMessage: await GetGiftSubNotificationRequest(sender, recipient, tier, months),
                    audioRequest: await GetGiftSubAudioRequest(sender, recipient, tier, months),
                    marqueeMessage: await GetGiftSubMarqueeMessage(sender, recipient, tier, months)),
                approved: approved);
        }

        protected virtual Task<string> GetGiftSubChatResponse(
            Database.User sender,
            Database.User recipient,
            int tier,
            int months)
        {
            return Task.FromResult<string>(null);
        }

        protected virtual Task<NotificationMessage> GetGiftSubNotificationRequest(
            Database.User sender,
            Database.User recipient,
            int tier,
            int months)
        {
            return Task.FromResult<NotificationMessage>(new ImageNotificationMessage(
                image: notificationServer.GetNextImageURL(),
                duration: 5_000,
                message: GetGiftSubNotificationMessage(sender, recipient, tier, months)));
        }

        protected virtual Task<Audio.AudioRequest> GetGiftSubAudioRequest(
            Database.User sender,
            Database.User recipient,
            int tier,
            int months)
        {
            Audio.AudioRequest soundEffectRequest = null;

            if (soundEffectSystem.HasSoundEffects())
            {
                Audio.SoundEffect raidSoundEffect = soundEffectSystem.GetSoundEffectByName("SMW PowerUp");
                if (raidSoundEffect is null)
                {
                    communication.SendWarningMessage($"Expected GiftSub SoundEffect not found.  Defaulting to first");
                    raidSoundEffect = soundEffectSystem.GetSoundEffectByName(soundEffectSystem.GetSoundEffects()[0]);
                }

                soundEffectRequest = new Audio.SoundEffectRequest(raidSoundEffect);
            }

            return Task.FromResult(soundEffectRequest);
        }

        protected virtual Task<MarqueeMessage> GetGiftSubMarqueeMessage(
            Database.User sender,
            Database.User recipient,
            int tier,
            int months)
        {
            return Task.FromResult<MarqueeMessage>(null);
        }

        protected virtual string GetGiftSubNotificationMessage(
            Database.User sender,
            Database.User recipient,
            int tier,
            int months)
        {
            if (months <= 1)
            {
                switch (tier)
                {
                    case 0: return $"It's possible to give a sub with Prime Gaming? Who Knew? Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    case 1: return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    case 2: return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a tier 2 sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    case 3: return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a tier 3 sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    default:
                        BGC.Debug.LogError($"Unexpected SubscriberNotification Values: {sender.TwitchUserName} sender, {recipient.TwitchUserName} recipient, {tier} tier");
                        return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                }
            }
            else
            {
                switch (tier)
                {
                    case 0: return $"It's possible to give a sub with Prime Gaming? Who Knew? Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    case 1: return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting {months} months to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    case 2: return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting {months} months of tier 2 to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    case 3: return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting {months} months of tier 3 to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    default:
                        BGC.Debug.LogError($"Unexpected SubscriberNotification Values: {sender.TwitchUserName} sender, {recipient.TwitchUserName} recipient, {tier} tier");
                        return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                }
            }
        }

        public virtual async void HandleAnonGiftSub(
            string recipientId,
            int tier,
            int months,
            bool approved)
        {
            Database.User recipient = await userHelper.GetUserByTwitchId(recipientId);

            if (recipient == null)
            {
                communication.SendErrorMessage($"Unable to find reciever {recipientId} for Gift Sub handling");
                return;
            }

            communication.NotifyEvent($"Gift Sub from Anon to {recipient.TwitchUserName}");

            string chatResponse = await GetAnonGiftSubChatResponse(recipient, tier, months);
            if (!string.IsNullOrWhiteSpace(chatResponse))
            {
                communication.SendPublicChatMessage(chatResponse);
            }

            activityDispatcher.QueueActivity(
                activity: new FullActivityRequest(
                    fullActivityProvider: this,
                    description: $"Anon Gift Sub To: {recipient.TwitchUserName}",
                    notificationMessage: await GetAnonGiftSubNotificationRequest(recipient, tier, months),
                    audioRequest: await GetAnonGiftSubAudioRequest(recipient, tier, months),
                    marqueeMessage: await GetAnonGiftSubMarqueeMessage(recipient, tier, months)),
                approved: approved);
        }

        protected virtual Task<string> GetAnonGiftSubChatResponse(
            Database.User recipient,
            int tier,
            int months)
        {
            return Task.FromResult<string>(null);
        }

        protected virtual Task<NotificationMessage> GetAnonGiftSubNotificationRequest(
            Database.User recipient,
            int tier,
            int months)
        {
            return Task.FromResult<NotificationMessage>(new ImageNotificationMessage(
                image: notificationServer.GetNextImageURL(),
                duration: 5_000,
                message: GetAnonGiftSubNotificationMessage(recipient, tier, months)));
        }

        protected virtual Task<Audio.AudioRequest> GetAnonGiftSubAudioRequest(
            Database.User recipient,
            int tier,
            int months)
        {
            Audio.AudioRequest soundEffectRequest = null;

            if (soundEffectSystem.HasSoundEffects())
            {
                Audio.SoundEffect raidSoundEffect = soundEffectSystem.GetSoundEffectByName("SMW PowerUp");
                if (raidSoundEffect is null)
                {
                    communication.SendWarningMessage($"Expected GiftSub SoundEffect not found.  Defaulting to first");
                    raidSoundEffect = soundEffectSystem.GetSoundEffectByName(soundEffectSystem.GetSoundEffects()[0]);
                }

                soundEffectRequest = new Audio.SoundEffectRequest(raidSoundEffect);
            }

            return Task.FromResult(soundEffectRequest);
        }

        protected virtual Task<MarqueeMessage> GetAnonGiftSubMarqueeMessage(
            Database.User recipient,
            int tier,
            int months)
        {
            return Task.FromResult<MarqueeMessage>(null);
        }

        protected virtual string GetAnonGiftSubNotificationMessage(
            Database.User recipient,
            int tier,
            int months)
        {
            if (months <= 1)
            {
                switch (tier)
                {
                    case 0: return $"It's possible to give a sub with Prime Gaming? Who Knew? Thank you, Anonymous, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    case 1: return $"Thank you, Anonymous, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    case 2: return $"Thank you, Anonymous, for gifting a tier 2 sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    case 3: return $"Thank you, Anonymous, for gifting a tier 3 sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    default:
                        BGC.Debug.LogError($"Unexpected SubscriberNotification Values: {recipient.TwitchUserName} recipient, {tier} tier, {months} months");
                        return $"Thank you, Anonymous, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                }
            }
            else
            {
                switch (tier)
                {
                    case 0: return $"It's possible to give a sub with Prime Gaming? Who Knew? Thank you, Anonymous, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    case 1: return $"Thank you, Anonymous, for gifting {months} months to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    case 2: return $"Thank you, Anonymous, for gifting {months} months of tier 2 to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    case 3: return $"Thank you, Anonymous, for gifting {months} months of tier 3 to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                    default:
                        BGC.Debug.LogError($"Unexpected SubscriberNotification Values: {recipient.TwitchUserName} recipient, {tier} tier, {months} months");
                        return $"Thank you, Anonymous, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                }
            }
        }

        #endregion IGiftSubHandler
        #region IFollowerHandler

        public virtual async void HandleFollower(
            Database.User follower,
            bool approved)
        {
            if (followedUserIds.Add(follower.TwitchUserId))
            {
                communication.NotifyEvent($"Follow: {follower.TwitchUserName}");
            }
            else
            {
                communication.NotifyEvent($"Re-Follow: {follower.TwitchUserName}");
                //Skip notifications
                return;
            }

            string chatResponse = await GetFollowChatResponse(follower);
            if (!string.IsNullOrWhiteSpace(chatResponse))
            {
                communication.SendPublicChatMessage(chatResponse);
            }

            activityDispatcher.QueueActivity(
                activity: new FullActivityRequest(
                    fullActivityProvider: this,
                    description: $"Follower: {follower.TwitchUserName}",
                    notificationMessage: await GetFollowNotificationRequest(follower),
                    audioRequest: await GetFollowAudioRequest(follower),
                    marqueeMessage: await GetFollowMarqueeMessage(follower)),
                approved: approved);
        }

        protected virtual Task<string> GetFollowChatResponse(Database.User follower)
        {
            return Task.FromResult($"Thanks for following, @{follower.TwitchUserName}");
        }

        protected virtual Task<NotificationMessage> GetFollowNotificationRequest(Database.User follower)
        {
            return Task.FromResult<NotificationMessage>(new ImageNotificationMessage(
                image: notificationServer.GetNextImageURL(),
                duration: 4_000,
                message: GetFollowNotificationMessage(follower)));
        }

        protected virtual Task<Audio.AudioRequest> GetFollowAudioRequest(Database.User follower)
        {
            Audio.AudioRequest soundEffectRequest = null;

            if (soundEffectSystem.HasSoundEffects())
            {
                Audio.SoundEffect raidSoundEffect = soundEffectSystem.GetSoundEffectByName("SMW MessageBlock");
                if (raidSoundEffect is null)
                {
                    communication.SendWarningMessage($"Expected Follow SoundEffect not found.  Defaulting to first");
                    raidSoundEffect = soundEffectSystem.GetSoundEffectByName(soundEffectSystem.GetSoundEffects()[0]);
                }

                soundEffectRequest = new Audio.SoundEffectRequest(raidSoundEffect);
            }

            return Task.FromResult(soundEffectRequest);
        }

        protected virtual Task<MarqueeMessage> GetFollowMarqueeMessage(Database.User follower)
        {
            return Task.FromResult<MarqueeMessage>(null);
        }

        protected virtual string GetFollowNotificationMessage(Database.User follower)
        {
            string fontColor = follower.Color;
            if (string.IsNullOrWhiteSpace(fontColor))
            {
                fontColor = "#0000FF";
            }

            return $"Thanks for the following, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(follower.TwitchUserName)}</span>!";
        }

        #endregion IFollowerHandler
        #region ITTSHandler

        public virtual async void HandleTTS(
            Database.User user,
            string message,
            bool approved)
        {
            string chatResponse = await GetTTSChatResponse(user, message);
            if (!string.IsNullOrWhiteSpace(chatResponse))
            {
                communication.SendPublicChatMessage(chatResponse);
            }

            activityDispatcher.QueueActivity(
                activity: new FullActivityRequest(
                    fullActivityProvider: this,
                    description: $"TTS {user.TwitchUserName} : {message}",
                    notificationMessage: await GetTTSNotificationRequest(user, message),
                    audioRequest: await GetTTSAudioRequest(user, message),
                    marqueeMessage: await GetTTSMarqueeMessage(user, message)),
                approved: approved);
        }

        protected virtual Task<string> GetTTSChatResponse(
            Database.User user,
            string message)
        {
            return Task.FromResult<string>(null);
        }

        protected virtual Task<NotificationMessage> GetTTSNotificationRequest(
            Database.User user,
            string message)
        {
            return Task.FromResult<NotificationMessage>(null);
        }

        protected virtual Task<Audio.AudioRequest> GetTTSAudioRequest(
            Database.User user,
            string message)
        {
            return ttsRenderer.TTSRequest(
                voicePreference: user.TTSVoicePreference,
                pitchPreference: user.TTSPitchPreference,
                speedPreference: user.TTSSpeedPreference,
                effectsChain: audioEffectSystem.SafeParse(user.TTSEffectsChain),
                ttsText: message);
        }

        protected virtual Task<MarqueeMessage> GetTTSMarqueeMessage(
            Database.User user,
            string message)
        {
            return Task.FromResult(new MarqueeMessage(user.TwitchUserName, message, user.Color));
        }

        #endregion ITTSHandler

        public static Audio.AudioRequest JoinRequests(int delayMS, params Audio.AudioRequest[] audioRequests)
        {
            List<Audio.AudioRequest> audioRequestList = new List<Audio.AudioRequest>(audioRequests.Where(x => x is not null));

            if (audioRequestList.Count == 0)
            {
                return null;
            }

            if (audioRequestList.Count == 1)
            {
                return audioRequestList[0];
            }

            for (int i = audioRequestList.Count - 1; i > 0; i--)
            {
                audioRequestList.Insert(i, new Audio.AudioDelay(delayMS));
            }

            return new Audio.ConcatenatedAudioRequest(audioRequestList);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    generalTokenSource.Cancel();
                    generalTokenSource.Dispose();
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

        public class FullActivityRequest : ActivityRequest
        {
            private readonly FullActivityProvider fullActivityProvider;
            public NotificationMessage NotificationMessage { get; }
            public Audio.AudioRequest AudioRequest { get; }
            public MarqueeMessage MarqueeMessage { get; }

            private readonly string description;

            public FullActivityRequest(
                FullActivityProvider fullActivityProvider,
                string description,
                NotificationMessage notificationMessage = null,
                Audio.AudioRequest audioRequest = null,
                MarqueeMessage marqueeMessage = null)
            {
                this.fullActivityProvider = fullActivityProvider;
                this.description = description;

                NotificationMessage = notificationMessage;
                AudioRequest = audioRequest;
                MarqueeMessage = marqueeMessage;
            }

            public override Task Execute() => fullActivityProvider.Execute(this);
            public override string ToString() => description;
        }
    }
}
