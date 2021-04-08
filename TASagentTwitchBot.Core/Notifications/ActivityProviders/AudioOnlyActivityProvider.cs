using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using TASagentTwitchBot.Core.TTS;

namespace TASagentTwitchBot.Core.Notifications
{
    public class AudioOnlyActivityProvider :
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

        protected readonly Database.IUserHelper userHelper;

        protected readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();

        protected readonly HashSet<string> followedUserIds = new HashSet<string>();

        private bool disposedValue;

        public AudioOnlyActivityProvider(
            ICommunication communication,
            Audio.ISoundEffectSystem soundEffectSystem,
            Audio.IAudioPlayer audioPlayer,
            Audio.Effects.IAudioEffectSystem audioEffectSystem,
            IActivityDispatcher activityDispatcher,
            ITTSRenderer ttsRenderer,
            Database.IUserHelper userHelper)
        {
            this.communication = communication;

            this.soundEffectSystem = soundEffectSystem;
            this.audioEffectSystem = audioEffectSystem;
            this.audioPlayer = audioPlayer;

            this.activityDispatcher = activityDispatcher;
            this.ttsRenderer = ttsRenderer;

            this.userHelper = userHelper;
        }

        protected virtual Task Execute(AudioOnlyActivityRequest activityRequest)
        {
            List<Task> taskList = new List<Task>();

            if (activityRequest.AudioRequest is not null)
            {
                taskList.Add(audioPlayer.PlayAudioRequest(activityRequest.AudioRequest));
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
                activity: new AudioOnlyActivityRequest(
                    audioOnlyActivityProvider: this,
                    description: $"Sub: {subscriber.TwitchUserName}: {message ?? ""}",
                    audioRequest: await GetSubscriberAudioRequest(subscriber, message, monthCount, tier)),
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
                    effectsChain: audioEffectSystem.Parse(subscriber.TTSEffectsChain),
                    ttsText: message);
            }

            return JoinRequests(300, soundEffectRequest, ttsRequest);
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
                    case 0: return $"Thank you for the brand new Prime Gaming sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                    case 1: return $"Thank you for the brand new sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                    case 2: return $"Thank you for the brand new tier 2 sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                    case 3: return $"Thank you for the brand new tier 3 sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                    default:
                        BGC.Debug.LogError($"Unexpected SubscriberNotification {tier} tier, {monthCount} months.");
                        return $"Thank you for the brand new sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                }
            }
            else
            {
                switch (tier)
                {
                    case 0: return $"Thank you for subscribing for {monthCount} months with Prime Gaming, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                    case 1: return $"Thank you for subscribing for {monthCount} months, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                    case 2: return $"Thank you for subscribing at tier 2 for {monthCount} months, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                    case 3: return $"Thank you for subscribing at tier 3 for {monthCount} months, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                    default:
                        BGC.Debug.LogError($"Unexpected SubscriberNotification {tier} tier, {monthCount} months.");
                        return $"Thank you for subscribing for {monthCount} months, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
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
                activity: new AudioOnlyActivityRequest(
                    audioOnlyActivityProvider: this,
                    description: $"User {cheerer.TwitchUserName} cheered {quantity} bits: {message}",
                    audioRequest: await GetCheerAudioRequest(cheerer, message, quantity)),
                approved: approved);
        }

        protected virtual Task<string> GetCheerChatResponse(
            Database.User cheerer,
            string message,
            int quantity)
        {
            return Task.FromResult<string>(null);
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
                    effectsChain: audioEffectSystem.Parse(cheerer.TTSEffectsChain),
                    ttsText: message);
            }

            return JoinRequests(300, soundEffectRequest, ttsRequest);
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
                activity: new AudioOnlyActivityRequest(
                    audioOnlyActivityProvider: this,
                    description: $"Raid: {raider} with {count} viewers",
                    audioRequest: await GetRaidAudioRequest(raider, count)),
                approved: approved);
        }

        protected virtual Task<string> GetRaidChatResponse(
            Database.User raider,
            int count)
        {
            return Task.FromResult($"Wow! {raider.TwitchUserName} has Raided with {count} viewers! PogChamp");
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
                activity: new AudioOnlyActivityRequest(
                    audioOnlyActivityProvider: this,
                    description: $"Gift Sub To: {recipientId}",
                    audioRequest: await GetGiftSubAudioRequest(sender, recipient, tier, months)),
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

            communication.NotifyEvent($"Gift Sub from Anon to {recipient}");

            string chatResponse = await GetAnonGiftSubChatResponse(recipient, tier, months);
            if (!string.IsNullOrWhiteSpace(chatResponse))
            {
                communication.SendPublicChatMessage(chatResponse);
            }

            activityDispatcher.QueueActivity(
                activity: new AudioOnlyActivityRequest(
                    audioOnlyActivityProvider: this,
                    description: $"Anon Gift Sub To: {recipient}",
                    audioRequest: await GetAnonGiftSubAudioRequest(recipient, tier, months)),
                approved: approved);
        }

        protected virtual Task<string> GetAnonGiftSubChatResponse(
            Database.User recipient,
            int tier,
            int months)
        {
            return Task.FromResult<string>(null);
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
                activity: new AudioOnlyActivityRequest(
                    audioOnlyActivityProvider: this,
                    description: $"Follower: {follower.TwitchUserName}",
                    audioRequest: await GetFollowAudioRequest(follower)),
                approved: approved);
        }

        protected virtual Task<string> GetFollowChatResponse(Database.User follower)
        {
            return Task.FromResult($"Thanks for following, @{follower.TwitchUserName}");
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
                activity: new AudioOnlyActivityRequest(
                    audioOnlyActivityProvider: this,
                    description: $"TTS {user.TwitchUserName} : {message}",
                    audioRequest: await GetTTSAudioRequest(user, message)),
                approved: approved);
        }

        protected virtual Task<string> GetTTSChatResponse(
            Database.User user,
            string message)
        {
            return Task.FromResult<string>(null);
        }

        protected virtual Task<Audio.AudioRequest> GetTTSAudioRequest(
            Database.User user,
            string message)
        {
            return ttsRenderer.TTSRequest(
                voicePreference: user.TTSVoicePreference,
                pitchPreference: user.TTSPitchPreference,
                effectsChain: audioEffectSystem.Parse(user.TTSEffectsChain),
                ttsText: message);
        }

        #endregion ITTSHandler

        protected static Audio.AudioRequest JoinRequests(int delayMS, params Audio.AudioRequest[] audioRequests)
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

        public class AudioOnlyActivityRequest : ActivityRequest
        {
            private readonly AudioOnlyActivityProvider audioOnlyActivityProvider;
            public Audio.AudioRequest AudioRequest { get; }

            private readonly string description;

            public AudioOnlyActivityRequest(
                AudioOnlyActivityProvider audioOnlyActivityProvider,
                string description,
                Audio.AudioRequest audioRequest = null)
            {
                this.audioOnlyActivityProvider = audioOnlyActivityProvider;
                this.description = description;

                AudioRequest = audioRequest;
            }

            public override Task Execute() => audioOnlyActivityProvider.Execute(this);
            public override string ToString() => description;
        }
    }
}
