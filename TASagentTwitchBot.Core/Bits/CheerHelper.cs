using System.Text.RegularExpressions;

using TASagentTwitchBot.Core.API.Twitch;

namespace TASagentTwitchBot.Core.Bits;

[AutoRegister]
public interface ICheerHelper
{
    Task<string?> GetAnimatedCheerURL(string prefix, int quantity, bool dark = true);
    Task<string?> GetCheerImageURL(string message, int quantity);
    Task<string?> GetCheermoteURL(string prefix);
}

public class CheerHelper : ICheerHelper
{
    private readonly Config.BotConfiguration botConfig;
    private readonly ICommunication communication;
    private readonly IBotTokenValidator botTokenValidator;
    private readonly HelixHelper helixHelper;

    private readonly Task initializationTask;

    private static readonly Regex cheerFinder = new Regex(@"(?:\s|^)([a-zA-Z]+)(\d+)(?:\s|$)");

    private readonly Dictionary<string, List<TwitchCheermotes.Datum.Tier>> cheerLookup = new Dictionary<string, List<TwitchCheermotes.Datum.Tier>>();

    public CheerHelper(
        Config.BotConfiguration botConfig,
        ICommunication communication,
        IBotTokenValidator botTokenValidator,
        HelixHelper helixHelper)
    {
        this.botConfig = botConfig;
        this.communication = communication;
        this.botTokenValidator = botTokenValidator;
        this.helixHelper = helixHelper;

        initializationTask = Initialize();
    }

    private async Task Initialize()
    {
        bool tokenValidated = await botTokenValidator.WaitForValidationAsync();

        if (!tokenValidated)
        {
            communication.SendErrorMessage("Unable to fetch Cheermotes due to failed acquisition of Bot token. Aborting.");
            throw new Exception("Unable to fetch Cheermotes due to failed acquisition of Bot token. Aborting.");
        }

        TwitchCheermotes? cheermotes = await helixHelper.GetCheermotes(botConfig.BroadcasterId) ??
            throw new Exception("Unable to fetch Cheermotes");

        foreach (TwitchCheermotes.Datum cheermote in cheermotes.Data)
        {
            cheerLookup.Add(cheermote.Prefix.ToLower(), cheermote.Tiers);
        }
    }

    public async Task<string?> GetCheerImageURL(string message, int quantity)
    {
        string? imageURL = null;

        foreach (Match match in cheerFinder.Matches(message))
        {
            string prefix = match.Groups[1].Value;

            imageURL = await GetAnimatedCheerURL(
                prefix: prefix,
                quantity: quantity,
                dark: true);

            if (!string.IsNullOrWhiteSpace(imageURL))
            {
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(imageURL))
        {
            //Fallback value
            imageURL = await GetAnimatedCheerURL(
                prefix: "Cheer",
                quantity: quantity,
                dark: true);
        }

        return imageURL;
    }

    public async Task<string?> GetCheermoteURL(string prefix)
    {
        if (!initializationTask.IsCompleted)
        {
            communication.SendDebugMessage($"Waiting for initialization for static Cheermote URL: {prefix}");
            await initializationTask;
        }

        if (cheerLookup.TryGetValue(prefix.ToLower(), out List<TwitchCheermotes.Datum.Tier>? tierList))
        {
            return tierList[0].Images.Dark.Static.SmallishURL;
        }

        return null;
    }


    public async Task<string?> GetAnimatedCheerURL(string prefix, int quantity, bool dark = true)
    {
        if (!initializationTask.IsCompleted)
        {
            communication.SendDebugMessage($"Waiting for initialization for Animated Cheer URL: {prefix}{quantity}");
            await initializationTask;
        }

        if (cheerLookup.TryGetValue(prefix.ToLower(), out List<TwitchCheermotes.Datum.Tier>? tierList))
        {
            TwitchCheermotes.Datum.Tier? lastMatch = null;

            for (int i = 0; i < tierList.Count; i++)
            {
                if (quantity >= tierList[i].MinBits)
                {
                    lastMatch = tierList[i];
                }
                else
                {
                    break;
                }
            }

            if (lastMatch is not null)
            {
                if (dark)
                {
                    return lastMatch.Images.Dark.Animated.HugeURL;
                }
                else
                {
                    return lastMatch.Images.Light.Animated.HugeURL;
                }
            }
        }

        return null;
    }
}
