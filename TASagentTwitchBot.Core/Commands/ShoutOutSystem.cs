using TASagentTwitchBot.Core.Database;
using TASagentTwitchBot.Core.API.Twitch;

namespace TASagentTwitchBot.Core.Commands;

public class ShoutOutSystem : ICommandContainer
{
    private readonly Config.BotConfiguration botConfig;
    private readonly ICommunication communication;
    private readonly IUserHelper userHelper;

    private readonly HelixHelper helixHelper;

    public ShoutOutSystem(
        Config.BotConfiguration botConfig,
        ICommunication communication,
        IUserHelper userHelper,
        HelixHelper helixHelper)
    {
        this.botConfig = botConfig;
        this.communication = communication;
        this.userHelper = userHelper;

        this.helixHelper = helixHelper;
    }

    public void RegisterCommands(ICommandRegistrar commandRegistrar)
    {
        commandRegistrar.RegisterGlobalCommand("so", ShoutOutCommandHandler);
        commandRegistrar.RegisterHelpCommand("so", ShoutOutHelpHandler);
    }

    public IEnumerable<string> GetPublicCommands()
    {
        yield return "so";
    }

    private string ShoutOutHelpHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            return "Send a shout-out message about another user.";
        }
        else
        {
            return $"No shout out subcommand found: {string.Join(' ', remainingCommand)}";
        }
    }

    private async Task ShoutOutCommandHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return;
        }

        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            //Get Random Quote
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no user specified for Shout-Out command.");
            return;
        }

        string userName = remainingCommand[0].ToLowerInvariant();
        if (userName[0] == '@')
        {
            //strip off superfluous @ signs
            userName = userName[1..];
        }

        User? matchingUser = await userHelper.GetUserByTwitchLogin(userName);

        if (matchingUser is null)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, twitch user \"{userName}\" not found.");
            return;
        }

        TwitchChannels? channelsInfo = await helixHelper.GetChannels(matchingUser.TwitchUserId);

        if (channelsInfo is null || channelsInfo.Data.Count == 0)
        {
            communication.SendErrorMessage($"Unable to request channel information about user \"{matchingUser.TwitchUserName}\".");
            communication.SendPublicChatMessage($"/shoutout {matchingUser.TwitchUserName}");
            communication.SendPublicChatMessage($"Check out {matchingUser.TwitchUserName} at twitch.tv/{matchingUser.TwitchUserName}");
            return;
        }

        if (string.IsNullOrWhiteSpace(channelsInfo.Data[0].GameName))
        {
            communication.SendPublicChatMessage($"/shoutout {matchingUser.TwitchUserName}");
            communication.SendPublicChatMessage($"Check out {matchingUser.TwitchUserName} at twitch.tv/{matchingUser.TwitchUserName}");
            return;
        }

        communication.SendPublicChatMessage($"/shoutout {matchingUser.TwitchUserName}");
        communication.SendPublicChatMessage($"Check out {matchingUser.TwitchUserName} at twitch.tv/{matchingUser.TwitchUserName} - " +
            $"Their last stream was of {channelsInfo.Data[0].GameName}, entitled \"{channelsInfo.Data[0].Title}\"");
    }
}
