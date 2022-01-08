
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

    public void RegisterCommands(ICommandRegistrar commandRegistrar)
    {
        commandRegistrar.RegisterScopedCommand("emoteeffects", "ignore", IgnoreEmote);
        commandRegistrar.RegisterScopedCommand("emoteeffect", "ignore", IgnoreEmote);
        commandRegistrar.RegisterScopedCommand("emotes", "ignore", IgnoreEmote);
        commandRegistrar.RegisterScopedCommand("emote", "ignore", IgnoreEmote);
        commandRegistrar.RegisterScopedCommand("emoteeffects", "hide", IgnoreEmote);
        commandRegistrar.RegisterScopedCommand("emoteeffect", "hide", IgnoreEmote);
        commandRegistrar.RegisterScopedCommand("emotes", "hide", IgnoreEmote);
        commandRegistrar.RegisterScopedCommand("emote", "hide", IgnoreEmote);

        commandRegistrar.RegisterScopedCommand("emoteeffects", "unignore", UnignoreEmote);
        commandRegistrar.RegisterScopedCommand("emoteeffect", "unignore", UnignoreEmote);
        commandRegistrar.RegisterScopedCommand("emotes", "unignore", UnignoreEmote);
        commandRegistrar.RegisterScopedCommand("emote", "unignore", UnignoreEmote);
        commandRegistrar.RegisterScopedCommand("emoteeffects", "show", UnignoreEmote);
        commandRegistrar.RegisterScopedCommand("emoteeffect", "show", UnignoreEmote);
        commandRegistrar.RegisterScopedCommand("emotes", "show", UnignoreEmote);
        commandRegistrar.RegisterScopedCommand("emote", "show", UnignoreEmote);

        commandRegistrar.RegisterScopedCommand("emoteeffects", "refresh", RefreshEmote);
        commandRegistrar.RegisterScopedCommand("emoteeffect", "refresh", RefreshEmote);
        commandRegistrar.RegisterScopedCommand("emotes", "refresh", RefreshEmote);
        commandRegistrar.RegisterScopedCommand("emote", "refresh", RefreshEmote);
        commandRegistrar.RegisterScopedCommand("emoteeffects", "reload", RefreshEmote);
        commandRegistrar.RegisterScopedCommand("emoteeffect", "reload", RefreshEmote);
        commandRegistrar.RegisterScopedCommand("emotes", "reload", RefreshEmote);
        commandRegistrar.RegisterScopedCommand("emote", "reload", RefreshEmote);

    }

    private Task IgnoreEmote(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, IgnoreEmote requires at least one emote to ignore.");
            return Task.CompletedTask;
        }

        foreach (string emote in remainingCommand)
        {
            emoteEffectListener.IgnoreEmote(emote);
        }

        return Task.CompletedTask;
    }

    private Task UnignoreEmote(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, UnignoreEmote requires at least one emote to ignore.");
            return Task.CompletedTask;
        }

        foreach (string emote in remainingCommand)
        {
            emoteEffectListener.UnignoreEmote(emote);
        }

        return Task.CompletedTask;
    }

    private Task RefreshEmote(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        emoteEffectListener.RefreshEmotes();
        return Task.CompletedTask;
    }
}
