using TASagentTwitchBot.Core.Database;
using TASagentTwitchBot.Core.API.Twitch;
using TASagentTwitchBot.Core.PubSub;

namespace TASagentTwitchBot.Plugin.TTTAS;

public class TTTASRedemptionHandler : IRedemptionContainer
{
    private readonly Core.ICommunication communication;
    private readonly ITTTASHandler tttasHandler;
    private readonly HelixHelper helixHelper;

    private readonly TTTASConfiguration tttasConfig;

    public TTTASRedemptionHandler(
        Core.ICommunication communication,
        ITTTASHandler tttasHandler,
        TTTASConfiguration tttasConfig,
        HelixHelper helixHelper)
    {
        this.communication = communication;
        this.tttasHandler = tttasHandler;
        this.tttasConfig = tttasConfig;
        this.helixHelper = helixHelper;
    }

    public async Task RegisterHandler(Dictionary<string, RedemptionHandler> handlers)
    {
        if (tttasConfig.Redemption.Enabled)
        {
            await Initialize();

            handlers.Add(tttasConfig.Redemption.RedemptionID, HandleRedemption);
        }
    }

    private async Task Initialize()
    {
        if (!string.IsNullOrEmpty(tttasConfig.Redemption.RedemptionID))
        {
            //Verify
            TwitchCustomReward customRewards = await helixHelper.GetCustomReward(
                id: tttasConfig.Redemption.RedemptionID,
                onlyManageableRewards: true) ??
                throw new Exception($"Unable to get TTTASRedemption Reward Data");

            if (customRewards.Data is not null &&
                customRewards.Data.Count == 1 &&
                customRewards.Data[0].Title == tttasConfig.Redemption.Name)
            {
                //Confirmed
            }
            else
            {
                //Error!
                //Reset RedemptionID
                tttasConfig.Redemption.RedemptionID = "";

                communication.SendErrorMessage($"Mismatch for {tttasConfig.FeatureNameBrief} Reward RedemptionID");
            }
        }

        if (string.IsNullOrEmpty(tttasConfig.Redemption.RedemptionID))
        {
            //Find or Create
            TwitchCustomReward customRewards = await helixHelper.GetCustomReward(onlyManageableRewards: true) ??
                throw new Exception($"Unable to get TTTASRedemption Reward Data");

            string tttasID = "";

            foreach (TwitchCustomReward.Datum customReward in customRewards.Data)
            {
                if (customReward.Title == tttasConfig.Redemption.Name)
                {
                    tttasID = customReward.Id;
                    break;
                }
            }

            if (string.IsNullOrEmpty(tttasID))
            {
                TwitchCustomReward creationResponse = await helixHelper.CreateCustomReward(
                    title: tttasConfig.Redemption.Name,
                    cost: tttasConfig.Redemption.Cost,
                    prompt: tttasConfig.Redemption.Description,
                    enabled: true,
                    backgroundColor: tttasConfig.Redemption.BackgroundColor,
                    userInputRequired: true,
                    skipQueue: false) ??
                    throw new Exception($"Unable to create TTTASRedemption Reward Data");

                tttasID = creationResponse.Data[0].Id;

                communication.SendDebugMessage($"Created {tttasConfig.FeatureName} Reward");
            }

            tttasConfig.UpdateRedemptionID(tttasID);
        }


        //Check for changed properties
        {
            //Verify
            TwitchCustomReward customRewards = await helixHelper.GetCustomReward(
                id: tttasConfig.Redemption.RedemptionID,
                onlyManageableRewards: true) ??
                throw new Exception($"Unable to get TTTASRedemption Reward Data");

            if (customRewards.Data is not null &&
                customRewards.Data.Count == 1 &&
                customRewards.Data[0].Title == tttasConfig.Redemption.Name)
            {
                //Confirmed Match
                TwitchCustomReward.Datum rewardInfo = customRewards.Data[0];

                //Compare Properties
                if (rewardInfo.Cost != tttasConfig.Redemption.Cost ||
                    rewardInfo.Prompt != tttasConfig.Redemption.Description ||
                    !rewardInfo.BackgroundColor.Equals(tttasConfig.Redemption.BackgroundColor, StringComparison.InvariantCultureIgnoreCase))
                {
                    //Config has changed - Update
                    await helixHelper.UpdateCustomRewardProperties(
                        id: tttasConfig.Redemption.RedemptionID,
                        cost: tttasConfig.Redemption.Cost,
                        prompt: tttasConfig.Redemption.Description,
                        backgroundColor: tttasConfig.Redemption.BackgroundColor);

                    communication.SendWarningMessage($"Updated {tttasConfig.FeatureNameBrief} Redemption details.");
                }
            }
            else
            {
                //Error!
                communication.SendErrorMessage($"Failed to validate {tttasConfig.FeatureNameBrief} Redemption. Continuing.");
            }
        }

        //Refund pending rewards
        TwitchCustomRewardRedemption pendingRedemptions = await helixHelper.GetCustomRewardRedemptions(
            rewardId: tttasConfig.Redemption.RedemptionID,
            status: "UNFULFILLED") ??
            throw new Exception($"Unable to get pending TTTASRedemption Redemption Data");

        int refundedCount = 0;

        foreach (TwitchCustomRewardRedemption.Datum redemption in pendingRedemptions.Data)
        {
            refundedCount++;

            await helixHelper.UpdateCustomRewardRedemptions(
                redemption.RewardData.Id,
                redemption.Id,
                status: "CANCELED");
        }

        if (refundedCount > 0)
        {
            communication.SendPublicChatMessage(
                $"Refunded {refundedCount} pending {tttasConfig.FeatureName} Rewards.");
        }
    }

    public async Task HandleRedemption(User user, ChannelPointMessageData.Datum.RedemptionData redemption)
    {
        //Handle redemption
        communication.SendDebugMessage($"{tttasConfig.FeatureNameBrief} Redemption: {user.TwitchUserName}");

        await helixHelper.UpdateCustomRewardRedemptions(
            redemption.Reward.Id,
            redemption.Id,
            status: "FULFILLED");

        if (tttasConfig.Redemption.AutoApprove || user.AuthorizationLevel >= Core.Commands.AuthorizationLevel.Elevated)
        {
            tttasHandler.HandleTTTAS(
                user: user,
                message: redemption.UserInput,
                approved: true);
        }
        else
        {
            tttasHandler.HandleTTTAS(
                user: user,
                message: redemption.UserInput,
                approved: false);
        }
    }
}
