using System;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Plugin.TTTAS
{
    public interface ITTTASHandler
    {
        void HandleTTTAS(Core.Database.User user, string message, bool approved);
    }

    public class TTTASFullHandler : ITTTASHandler
    {
        private readonly Core.ICommunication communication;
        private readonly Core.Notifications.IActivityDispatcher activityDispatcher;
        private readonly Core.Notifications.FullActivityProvider fullActivityProvider;
        private readonly Core.Audio.ISoundEffectSystem soundEffectSystem;
        private readonly ITTTASRenderer tttasRenderer;

        private readonly TTTASConfiguration tttasConfig;

        public TTTASFullHandler(
            Core.ICommunication communication,
            Core.Notifications.FullActivityProvider fullActivityProvider,
            Core.Notifications.IActivityDispatcher activityDispatcher,
            Core.Audio.ISoundEffectSystem soundEffectSystem,
            ITTTASRenderer tttasRenderer,
            TTTASConfiguration tttasConfig)
        {
            this.communication = communication;
            this.fullActivityProvider = fullActivityProvider;
            this.activityDispatcher = activityDispatcher;
            this.soundEffectSystem = soundEffectSystem;
            this.tttasRenderer = tttasRenderer;
            this.tttasConfig = tttasConfig;
        }

        public async void HandleTTTAS(
            Core.Database.User user,
            string message,
            bool approved)
        {
            activityDispatcher.QueueActivity(
                activity: new Core.Notifications.FullActivityProvider.FullActivityRequest(
                    fullActivityProvider: fullActivityProvider,
                    description: $"{tttasConfig.FeatureNameBrief} {user.TwitchUserName}: {message}",
                    notificationMessage: null,
                    audioRequest: await GetTTTASAudioRequest(user, message),
                    marqueeMessage: new Core.Notifications.MarqueeMessage(user.TwitchUserName, message, user.Color)),
                approved: approved);
        }

        private async Task<Core.Audio.AudioRequest> GetTTTASAudioRequest(Core.Database.User _, string message)
        {
            Core.Audio.AudioRequest soundEffectRequest = null;
            Core.Audio.AudioRequest tttasRequest = null;

            if (!string.IsNullOrEmpty(tttasConfig.SoundEffect) && soundEffectSystem.HasSoundEffects())
            {
                Core.Audio.SoundEffect tttasSoundEffect = soundEffectSystem.GetSoundEffectByName(tttasConfig.SoundEffect);
                if (tttasSoundEffect is null)
                {
                    communication.SendWarningMessage($"Expected {tttasConfig.FeatureNameBrief} SoundEffect \"{tttasConfig.SoundEffect}\" not found.  Defaulting to first sound effect.");
                    tttasSoundEffect = soundEffectSystem.GetSoundEffectByName(soundEffectSystem.GetSoundEffects()[0]);
                }

                soundEffectRequest = new Core.Audio.SoundEffectRequest(tttasSoundEffect);
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                tttasRequest = await tttasRenderer.TTTASRequest(
                    tttasText: message);
            }

            return Core.Notifications.FullActivityProvider.JoinRequests(300, soundEffectRequest, tttasRequest);
        }
    }
}
