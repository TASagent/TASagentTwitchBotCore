
using TASagentTwitchBot.Core.Commands;

namespace TASagentTwitchBot.Core.EmoteEffects;

public class EmoteEffectSystem : ICommandContainer
{
    private readonly ICommunication communication;
    private readonly IEmoteEffectListener emoteEffectListener;

    public EmoteEffectSystem(
        ICommunication communication,
        IEmoteEffectListener emoteEffectListener)
    {
        this.communication = communication;
        this.emoteEffectListener = emoteEffectListener;
    }

    public IEnumerable<string> GetPublicCommands()
    {
        yield break;
    }

    public void RegisterCommands(
        Dictionary<string, CommandHandler> commands,
        Dictionary<string, HelpFunction> helpFunctions,
        Dictionary<string, SetFunction> setFunctions,
        Dictionary<string, GetFunction> getFunctions)
    {
        commands.Add("emoteeffect", EmoteEffectCommandHandler);
    }

    private Task EmoteEffectCommandHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (remainingCommand.Length == 0)
        {
            communication.SendPublicChatMessage(
                $"@{chatter.User.TwitchUserName}, EmoteEffect commands available: Ignore, Unignore, Reload.");
            return Task.CompletedTask;
        }

        switch (remainingCommand[0].ToLowerInvariant())
        {
            case "ignore":
                if (remainingCommand.Length < 2)
                {
                    communication.SendPublicChatMessage(
                        $"@{chatter.User.TwitchUserName}, IgnoreEmote requires at least one emote to ignore.");
                    return Task.CompletedTask;
                }

                foreach (string emote in remainingCommand[1..])
                {
                    emoteEffectListener.IgnoreEmote(emote);
                }
                break;

            case "unignore":
                if (remainingCommand.Length < 2)
                {
                    communication.SendPublicChatMessage(
                        $"@{chatter.User.TwitchUserName}, UnignoreEmote requires at least one emote to unignore.");
                    return Task.CompletedTask;
                }

                foreach (string emote in remainingCommand[1..])
                {
                    emoteEffectListener.UnignoreEmote(emote);
                }
                break;

            case "refresh":
            case "reload":
                emoteEffectListener.RefreshEmotes();
                break;

            default:
                communication.SendPublicChatMessage(
                    $"@{chatter.User.TwitchUserName}, unrecognized EmoteEffect command ({remainingCommand[0]}).");
                break;
        }

        return Task.CompletedTask;
    }
}
