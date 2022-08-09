using Microsoft.AspNetCore.SignalR;

namespace TASagentTwitchBot.Core.Web.Hubs;

public class DonationHub : Hub
{
    private readonly Donations.IDonationTracker donationTracker;

    public DonationHub(Donations.IDonationTracker donationTracker)
    {
        this.donationTracker = donationTracker;
    }

    public async Task RequestState()
    {
        await Clients.Caller.SendAsync("SetState", donationTracker.GetState());
    }
}
