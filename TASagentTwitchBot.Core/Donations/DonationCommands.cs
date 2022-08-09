namespace TASagentTwitchBot.Core.Donations;

public class DonationCommands : Commands.ICommandContainer
{
    private readonly ICommunication communication;
    private readonly IDonationTracker donationTracker;
    private readonly IDonationHandler donationHandler;

    public DonationCommands(
        ICommunication communication,
        IDonationTracker donationTracker,
        IDonationHandler donationHandler)
    {
        this.communication = communication;
        this.donationTracker = donationTracker;
        this.donationHandler = donationHandler;
    }

    public void RegisterCommands(Commands.ICommandRegistrar commandRegistrar)
    {
        commandRegistrar.RegisterScopedCommand("donation", "add", AddDonation);
        commandRegistrar.RegisterScopedCommand("donations", "add", AddDonation);
        commandRegistrar.RegisterScopedCommand("add", "donation", AddDonation);
        commandRegistrar.RegisterScopedCommand("add", "donations", AddDonation);
        commandRegistrar.RegisterGlobalCommand("adddonation", AddDonation);
        commandRegistrar.RegisterGlobalCommand("adddonations", AddDonation);

        commandRegistrar.RegisterScopedCommand("donation", "sub", RemoveDonation);
        commandRegistrar.RegisterScopedCommand("donations", "sub", RemoveDonation);
        commandRegistrar.RegisterScopedCommand("donation", "remove", RemoveDonation);
        commandRegistrar.RegisterScopedCommand("donations", "remove", RemoveDonation);
        commandRegistrar.RegisterScopedCommand("sub", "donation", RemoveDonation);
        commandRegistrar.RegisterScopedCommand("sub", "donations", RemoveDonation);
        commandRegistrar.RegisterGlobalCommand("subdonation", RemoveDonation);
        commandRegistrar.RegisterGlobalCommand("subdonations", RemoveDonation);
        commandRegistrar.RegisterScopedCommand("remove", "donation", RemoveDonation);
        commandRegistrar.RegisterScopedCommand("remove", "donations", RemoveDonation);

        commandRegistrar.RegisterScopedCommand("set", "goal", SetDonationGoal);
        commandRegistrar.RegisterScopedCommand("set", "donationgoal", SetDonationGoal);
        commandRegistrar.RegisterScopedCommand("set", "donationsgoal", SetDonationGoal);
        commandRegistrar.RegisterScopedCommand("donation", "goal", SetDonationGoal);
        commandRegistrar.RegisterScopedCommand("donations", "goal", SetDonationGoal);
        commandRegistrar.RegisterGlobalCommand("donationgoal", SetDonationGoal);
        commandRegistrar.RegisterGlobalCommand("donationsgoal", SetDonationGoal);

        commandRegistrar.RegisterScopedCommand("test", "donations", TestDonation);
        commandRegistrar.RegisterScopedCommand("test", "donation", TestDonation);
        commandRegistrar.RegisterScopedCommand("donations", "test", TestDonation);
        commandRegistrar.RegisterScopedCommand("donation", "test", TestDonation);
        commandRegistrar.RegisterGlobalCommand("testdonations", TestDonation);
        commandRegistrar.RegisterGlobalCommand("testdonation", TestDonation);
    }

    public IEnumerable<string> GetPublicCommands()
    {
        yield break;
    }

    private Task AddDonation(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < Commands.AuthorizationLevel.Moderator)
        {
            //Moderators and Admins can adjust users
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Add Donations requires specifying how much money to add.");
            return Task.CompletedTask;
        }

        if (!double.TryParse(remainingCommand[0], out double donation))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Unable to parse {remainingCommand[0]} as quantity.");
            return Task.CompletedTask;
        }

        donationTracker.AddDirectDonations(donation);
        return Task.CompletedTask;
    }


    private Task RemoveDonation(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < Commands.AuthorizationLevel.Moderator)
        {
            //Moderators and Admins can adjust users
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Remove Donations requires specifying how much money to remove.");
            return Task.CompletedTask;
        }

        if (!double.TryParse(remainingCommand[0], out double donation))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Unable to parse {remainingCommand[0]} as quantity.");
            return Task.CompletedTask;
        }

        donationTracker.AddDirectDonations(-donation);
        return Task.CompletedTask;
    }

    private Task SetDonationGoal(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < Commands.AuthorizationLevel.Moderator)
        {
            //Moderators and Admins can adjust users
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Setting Fundraising Goal requires specifying the new goal.");
            return Task.CompletedTask;
        }

        if (!double.TryParse(remainingCommand[0], out double donationGoal))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Unable to parse {remainingCommand[0]} as quantity.");
            return Task.CompletedTask;
        }

        donationTracker.SetFundraisingGoal(donationGoal);
        return Task.CompletedTask;
    }

    private Task TestDonation(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < Commands.AuthorizationLevel.Moderator)
        {
            //Moderators and Admins can adjust users
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (remainingCommand is null || remainingCommand.Length < 2)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, TestDonations requires a Name and an amount, followed by an optional message.");
            return Task.CompletedTask;
        }


        if (!double.TryParse(remainingCommand[1], out double donation))
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, Unable to parse {remainingCommand[1]} as quantity.");
            return Task.CompletedTask;
        }

        string message = "";

        if (remainingCommand.Length >= 3)
        {
            message = string.Join(' ', remainingCommand[2..]);
        }

        donationHandler.HandleDonation(remainingCommand[0], donation, message, true);
        return Task.CompletedTask;
    }
}
