﻿namespace TASagentTwitchBot.Core.Donations;

public class DonationCommands : Commands.ICommandContainer
{
    private readonly ICommunication communication;
    private readonly IDonationTracker donationTracker;

    public DonationCommands(
        ICommunication communication,
        IDonationTracker donationTracker)
    {
        this.communication = communication;
        this.donationTracker = donationTracker;
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
}