using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace TASagentTwitchBot.Core.Web.Hubs
{
    public class DonationHub : Hub
    {
        private readonly Donations.IDonationTracker donationTracker;

        public DonationHub(Donations.IDonationTracker donationTracker)
        {
            this.donationTracker = donationTracker;
        }

        public async Task RequestAmount()
        {
            await Clients.Caller.SendAsync("SetAmount", donationTracker.GetAmount());
        }
    }
}
