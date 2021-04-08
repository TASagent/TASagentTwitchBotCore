using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TASagentTwitchBot.Core.PubSub
{
    public interface IRedemptionSystem
    {
        Task Initialize();
        void HandleRedemption(ChannelPointMessageData.Datum redemption);
    }

    public class RedemptionSystem : IRedemptionSystem
    {
        private readonly ICommunication communication;

        private readonly IRedemptionContainer[] redemptionContainers;
        private readonly Database.BaseDatabaseContext db;

        private readonly Dictionary<string, RedemptionHandler> redemptionHandlers = new Dictionary<string, RedemptionHandler>();

        public RedemptionSystem(
            ICommunication communication,
            IEnumerable<IRedemptionContainer> redemptionContainers,
            Database.BaseDatabaseContext db)
        {
            this.communication = communication;

            this.db = db;

            this.redemptionContainers = redemptionContainers.ToArray();
        }

        public async void HandleRedemption(ChannelPointMessageData.Datum redemption)
        {
            //Handle redemption
            string rewardID = redemption.Redemption.Reward.Id;

            if (!redemptionHandlers.ContainsKey(rewardID))
            {
                communication.SendErrorMessage($"Redemption handler not found: {rewardID}");
                return;
            }

            Database.User user = db.Users.First(x => x.TwitchUserId == redemption.Redemption.User.Id);

            if (user is null)
            {
                communication.SendErrorMessage($"User not found: {redemption.Redemption.User.Id}");
                return;
            }

            await redemptionHandlers[rewardID](user, redemption.Redemption);
        }

        public async Task Initialize()
        {
            foreach (IRedemptionContainer redemptionContainer in redemptionContainers)
            {
                await redemptionContainer.RegisterHandler(redemptionHandlers);
            }
        }
    }

    public delegate Task RedemptionHandler(Database.User user, ChannelPointMessageData.Datum.RedemptionData redemption);

    public interface IRedemptionContainer
    {
        Task RegisterHandler(Dictionary<string, RedemptionHandler> handlers);
    }
}
