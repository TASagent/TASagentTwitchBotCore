using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using TASagentTwitchBot.Core.Database;
using TASagentTwitchBot.Core.API.Twitch;
using TASagentTwitchBot.Core.PubSub;
using System.Threading;

namespace TASagentTwitchBot.Plugin.TTTAS
{
    public class TTTASRedemptionHandler : IRedemptionContainer, IDisposable
    {
        private readonly Core.ICommunication communication;
        private readonly ITTTASHandler tttasHandler;
        private readonly HelixHelper helixHelper;

        private readonly TTTASConfiguration tttasConfig;

        private readonly SemaphoreSlim initSemaphore = new SemaphoreSlim(1);

        private bool initialized = false;
        private bool disposedValue = false;

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
                if (!initialized)
                {
                    await InitializeAsync();
                }

                handlers.Add(tttasConfig.Redemption.RedemptionID, HandleRedemption);
            }
        }

        private async Task InitializeAsync()
        {
            if (initialized)
            {
                return;
            }

            await initSemaphore.WaitAsync();

            if (initialized)
            {
                initSemaphore.Release();
                return;
            }

            await Initialize();

            initialized = true;
            initSemaphore.Release();
        }

        private async Task Initialize()
        {
            if (!string.IsNullOrEmpty(tttasConfig.Redemption.RedemptionID))
            {
                //Verify
                var customRewards = await helixHelper.GetCustomReward(
                    id: tttasConfig.Redemption.RedemptionID,
                    onlyManageableRewards: true);

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
                    tttasConfig.Redemption.RedemptionID = null;

                    communication.SendErrorMessage($"Mismatch for {tttasConfig.FeatureNameBrief} Reward RedemptionID");
                }
            }

            if (string.IsNullOrEmpty(tttasConfig.Redemption.RedemptionID))
            {
                //Find or Create
                var customRewards = await helixHelper.GetCustomReward(onlyManageableRewards: true);

                string tttasID = "";

                foreach (var customReward in customRewards.Data)
                {
                    if (customReward.Title == tttasConfig.Redemption.Name)
                    {
                        tttasID = customReward.Id;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(tttasID))
                {
                    var creationResponse = await helixHelper.CreateCustomReward(
                        title: tttasConfig.Redemption.Name,
                        cost: tttasConfig.Redemption.Cost,
                        prompt: tttasConfig.Redemption.Description,
                        enabled: true,
                        backgroundColor: tttasConfig.Redemption.BackgroundColor,
                        userInputRequired: true,
                        maxPerStreamEnabled: false,
                        maxPerUserPerStreamEnabled: false,
                        globalCooldownEnabled: false,
                        redemptionsSkipQueue: false);

                    tttasID = creationResponse.Data[0].Id;

                    communication.SendDebugMessage($"Created {tttasConfig.FeatureName} Reward");
                }

                tttasConfig.UpdateRedemptionID(tttasID);
            }


            //Check for changed properties
            {
                //Verify
                var customRewards = await helixHelper.GetCustomReward(
                    id: tttasConfig.Redemption.RedemptionID,
                    onlyManageableRewards: true);

                if (customRewards.Data is not null &&
                    customRewards.Data.Count == 1 &&
                    customRewards.Data[0].Title == tttasConfig.Redemption.Name)
                {
                    //Confirmed Match
                    var rewardInfo = customRewards.Data[0];

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
            var pendingRedemptions = await helixHelper.GetCustomRewardRedemptions(
                rewardId: tttasConfig.Redemption.RedemptionID,
                status: "UNFULFILLED");

            int refundedCount = 0;

            foreach (var redemption in pendingRedemptions.Data)
            {
                refundedCount++;

                await helixHelper.UpdateCustomRewardRedemptions(
                    redemption.RewardData.Id,
                    redemption.Id,
                    status: "UNFULFILLED");
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    initSemaphore.Dispose();
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
    }
}
