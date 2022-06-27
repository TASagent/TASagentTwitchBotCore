using TASagentTwitchBot.Core.API.Twitch;
using TASagentTwitchBot.Core.PubSub;
using TASagentTwitchBot.Core.Database;

namespace TASagentTwitchBot.Core.TTS;

public class TTSRedemptionHandler : IRedemptionContainer
{
    private readonly HelixHelper helixHelper;
    private readonly TTSConfiguration ttsConfig;

    private readonly ICommunication communication;
    private readonly Notifications.ITTSHandler ttsHandler;
    private readonly IUserHelper userHelper;

    public TTSRedemptionHandler(
        TTSConfiguration ttsConfig,
        HelixHelper helixHelper,
        ICommunication communication,
        Notifications.ITTSHandler ttsHandler,
        IUserHelper userHelper)
    {
        this.helixHelper = helixHelper;
        this.ttsConfig = ttsConfig;

        this.communication = communication;
        this.ttsHandler = ttsHandler;
        this.userHelper = userHelper;

        ttsConfig.AssignRedemptionUpdateAction(HandleRedemptionUpdate);
    }

    public async Task RegisterHandler(Dictionary<string, RedemptionHandler> handlers)
    {
        await Initialize();
        handlers.Add(ttsConfig.Redemption.RedemptionID, HandleRedemption);
    }

    private async Task Initialize()
    {
        if (!string.IsNullOrEmpty(ttsConfig.Redemption.RedemptionID))
        {
            //Verify
            try
            {
                TwitchCustomReward customRewards = await helixHelper.GetCustomReward(
                    id: ttsConfig.Redemption.RedemptionID,
                    onlyManageableRewards: true) ??
                    throw new Exception("Unable to Get TTS Redemption data");

                if (customRewards.Data is not null &&
                    customRewards.Data.Count == 1)
                {
                    //Confirmed
                }
                else
                {
                    //Error!
                    throw new Exception("Unable to Get TTS Redemption data");
                }
            }
            catch (Exception ex)
            {
                //Error!
                ttsConfig.UpdateRedemptionID("");

                communication.SendErrorMessage($"Mismatch for TTS Redemption data: {ex.Message}");
            }

        }

        if (string.IsNullOrEmpty(ttsConfig.Redemption.RedemptionID))
        {
            //Create
            TwitchCustomReward creationResponse = await helixHelper.CreateCustomReward(
                title: ttsConfig.Redemption.Name,
                cost: ttsConfig.Redemption.Cost,
                prompt: ttsConfig.Redemption.Description,
                enabled: ttsConfig.Redemption.Enabled,
                backgroundColor: ttsConfig.Redemption.BackgroundColor,
                userInputRequired: true,
                skipQueue: false) ??
                throw new Exception($"Unable to create Custom TTS Redemption");

            communication.SendWarningMessage($"Created \"TTS\" Redemption");

            ttsConfig.UpdateRedemptionID(creationResponse.Data[0].Id);
        }

        //Check Enabled Status

        TwitchCustomReward ttsRedemption = await helixHelper.GetCustomReward(
            id: ttsConfig.Redemption.RedemptionID,
            onlyManageableRewards: true) ??
            throw new Exception("Unable to Get TTS Redemption data");

        if (ttsRedemption.Data is not null &&
            ttsRedemption.Data.Count == 1)
        {
            //Confirmed
            if (ttsRedemption.Data[0].IsEnabled != ttsConfig.Redemption.Enabled ||
                ttsRedemption.Data[0].IsPaused != ttsConfig.Redemption.Paused)
            {
                await helixHelper.UpdateCustomRewardProperties(
                    id: ttsConfig.Redemption.RedemptionID,
                    enabled: ttsConfig.Redemption.Enabled,
                    paused: ttsConfig.Redemption.Paused);

                communication.SendWarningMessage($"Updated TTS Redemption Enabled to {ttsConfig.Redemption.Enabled} and Paused to {ttsConfig.Redemption.Paused}");
            }
        }
        else
        {
            throw new Exception("Unable to Get TTS Redemption data");
        }

        //Refund pending rewards
        TwitchCustomRewardRedemption ? pendingRedemptions = await helixHelper.GetCustomRewardRedemptions(
            rewardId: ttsConfig.Redemption.RedemptionID,
            status: "UNFULFILLED");

        if (pendingRedemptions is null)
        {
            communication.SendErrorMessage("Unable to get pending unfulfilled reward data for TTS Redemption. Something may be misconfigured.");
            return;
        }

        int updatedCount = 0;

        foreach (TwitchCustomRewardRedemption.Datum redemption in pendingRedemptions.Data)
        {
            updatedCount++;

            await helixHelper.UpdateCustomRewardRedemptions(
                rewardId: redemption.RewardData.Id,
                id: redemption.Id,
                status: "CANCELED");
        }

        if (updatedCount > 0)
        {
            communication.SendPublicChatMessage(
                $"Refunded {updatedCount} pending TTS Redemptions.");
        }
    }

    private async void HandleRedemptionUpdate()
    {
        await helixHelper.UpdateCustomRewardProperties(
            id: ttsConfig.Redemption.RedemptionID,
            enabled: ttsConfig.Redemption.Enabled,
            paused: ttsConfig.Redemption.Paused);

        communication.SendDebugMessage($"Updated TTS Redemption Enabled to {ttsConfig.Redemption.Enabled} and Paused to {ttsConfig.Redemption.Paused}");
    }

    private async Task HandleRedemption(
        User user,
        ChannelPointMessageData.Datum.RedemptionData redemption)
    {
        if (!ttsConfig.Enabled)
        {
            //TTS is currently disabled. Kick it back.
            communication.SendDebugMessage($"Rejected TTS Redemption: {user.TwitchUserName}");

            communication.SendPublicChatMessage(
                $"@{user.TwitchUserName}, TTS is currently disabled.");

            await helixHelper.UpdateCustomRewardRedemptions(
                rewardId: redemption.Reward.Id,
                id: redemption.Id,
                status: "CANCELED");

            return;
        }

        communication.SendDebugMessage($"TTS Redemption: {user.TwitchUserName}");

        User speaker = user;

        if (ttsConfig.OverrideVoices)
        {
            User broadcaster = await userHelper.GetBroadcaster();
            //Create virtual user with updated TTS features
            speaker = new User()
            {
                UserId = user.UserId,
                TwitchUserName = user.TwitchUserName,
                TwitchUserId = user.TwitchUserId,

                TTSVoicePreference = broadcaster.TTSVoicePreference,
                TTSPitchPreference = broadcaster.TTSPitchPreference,
                TTSSpeedPreference = broadcaster.TTSSpeedPreference,
                TTSEffectsChain = broadcaster.TTSEffectsChain,

                AuthorizationLevel = user.AuthorizationLevel,
                Color = user.Color
            };
        }

        bool approved = user.AuthorizationLevel >= Commands.AuthorizationLevel.Elevated;

        ttsHandler.HandleTTS(
            user: speaker,
            message: redemption.UserInput,
            approved: approved);

        await helixHelper.UpdateCustomRewardRedemptions(
            rewardId: redemption.Reward.Id,
            id: redemption.Id,
            status: "FULFILLED");

        if (!approved)
        {
            communication.SendPublicChatMessage($"@{user.TwitchUserName} has queued a TTS request.");
        }
    }
}
