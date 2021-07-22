namespace TASagentTwitchBot.Core.Donations
{
    public interface IDonationHandler
    {
        void HandleDonation(string name, double amount, string message, bool approved);
    }
}
