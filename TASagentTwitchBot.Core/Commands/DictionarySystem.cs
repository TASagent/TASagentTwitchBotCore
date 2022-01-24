using System.Globalization;
using TASagentTwitchBot.Core.API.Dictionary;

namespace TASagentTwitchBot.Core.Commands;

public class DictionarySystem : ICommandContainer
{
    private readonly ICommunication communication;
    private readonly DictionaryHelper dictionaryHelper;

    public DictionarySystem(
        ICommunication communication,
        DictionaryHelper dictionaryHelper)
    {
        this.communication = communication;
        this.dictionaryHelper = dictionaryHelper;
    }

    public void RegisterCommands(ICommandRegistrar commandRegistrar)
    {
        commandRegistrar.RegisterGlobalCommand("define", DefineHandler);
    }

    public IEnumerable<string> GetPublicCommands()
    {
        yield return "define";
    }

    private async Task DefineHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand.Length == 0)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, I need a word to define!");
            return;
        }

        List<DictionaryInfo>? definition = await dictionaryHelper.GetDefinition(remainingCommand[0]);

        if (definition is null || definition.Count == 0)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no definition for {remainingCommand[0]} found.");
            return;
        }

        string definitionOutput = "";

        for (int i = 0; i < Math.Min(2, definition.Count); i++)
        {
            for (int j = 0; j < Math.Min(2, definition[i].Meanings.Count); j++)
            {
                for (int k = 0; k < Math.Min(2, definition[i].Meanings[j].Definitions.Count); k++)
                {
                    definitionOutput += $"**{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(definition[i].Word)}** ({definition[i].Meanings[j].PartOfSpeech}). {definition[i].Meanings[j].Definitions[k].Definition} ";
                }
            }
        }

        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}: {definitionOutput}");
    }
}
