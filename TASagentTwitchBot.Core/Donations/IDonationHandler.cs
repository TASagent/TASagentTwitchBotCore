namespace TASagentTwitchBot.Core.Donations;

[AutoRegister]
public interface IDonationHandler
{
    void HandleDonation(string name, double amount, string message, bool approved);
}
