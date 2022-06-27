using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace TASagentTwitchBot.Core.Donations;

[AutoRegister]
public interface IDonationTracker
{
    void AddBits(int count);
    void AddSubs(int count, int tier);
    void AddDirectDonations(double amount);

    DonationAmount GetAmount();
}


public class NoDonationTracker : IDonationTracker
{
    public NoDonationTracker() { }

    void IDonationTracker.AddBits(int count) { }
    void IDonationTracker.AddDirectDonations(double amount) { }
    void IDonationTracker.AddSubs(int count, int tier) { }

    DonationAmount IDonationTracker.GetAmount() => new DonationAmount(0.00);
}

public class PersistentDonationTracker : IDonationTracker
{
    private readonly IHubContext<Web.Hubs.DonationHub> donationHubContext;
    private readonly TrackedDonations trackedDonations;

    public PersistentDonationTracker(
        IHubContext<Web.Hubs.DonationHub> donationHubContext)
    {
        this.donationHubContext = donationHubContext;

        trackedDonations = TrackedDonations.GetData();
    }

    public async void AddBits(int count)
    {
        trackedDonations.BitCount += count;
        trackedDonations.Serialize();

        await donationHubContext.Clients.All.SendAsync("SetAmount", GetAmount());
    }

    public async void AddDirectDonations(double amount)
    {
        trackedDonations.DirectDonationAmount += amount;
        trackedDonations.Serialize();

        await donationHubContext.Clients.All.SendAsync("SetAmount", GetAmount());
    }

    public async void AddSubs(int count, int tier)
    {
        int multiplier = 1;

        if (tier == 2)
        {
            multiplier = 2;
        }
        else if (tier == 3)
        {
            multiplier = 6;
        }

        trackedDonations.SubCount += multiplier * count;
        trackedDonations.Serialize();

        await donationHubContext.Clients.All.SendAsync("SetAmount", GetAmount());
    }

    public DonationAmount GetAmount() => new DonationAmount(trackedDonations.GetDollarAmount);

    private class TrackedDonations
    {
        [JsonIgnore]
        private static string FilePath => BGC.IO.DataManagement.PathForDataFile("Config", "Tiltify", "Donations.json");

        [JsonIgnore]
        private static readonly object _lock = new object();

        public int BitCount { get; set; } = 0;
        public int SubCount { get; set; } = 0;
        public double DirectDonationAmount { get; set; } = 0.0;


        public double DollarsPerSub { get; set; } = 2.50;
        public double DollarsPerBit { get; set; } = 0.01;

        [JsonIgnore]
        public double GetDollarAmount => DollarsPerBit * BitCount + DollarsPerSub * SubCount + DirectDonationAmount;

        public static TrackedDonations GetData()
        {
            TrackedDonations data;

            if (File.Exists(FilePath))
            {
                data = JsonSerializer.Deserialize<TrackedDonations>(File.ReadAllText(FilePath))!;
            }
            else
            {
                data = new TrackedDonations();
                data.Serialize();
            }

            return data;
        }

        public void Serialize()
        {
            lock (_lock)
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
            }
        }
    }
}

public record DonationAmount(double CurrentAmount);
