using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TASagentTwitchBot.Core.API.Twitch;

namespace TASagentTwitchBot.Core.Bits
{
    public class CheerHelper
    {
        private readonly HelixHelper helixHelper;
        private readonly Config.BotConfiguration botConfig;

        private bool hasData = false;

        private static readonly Regex cheerFinder = new Regex(@"([a-zA-Z]+)(\d+)");

        private readonly Dictionary<string, List<TwitchCheermotes.Datum.Tier>> cheerLookup = new Dictionary<string, List<TwitchCheermotes.Datum.Tier>>();

        public CheerHelper(
            Config.IBotConfigContainer botConfigContainer,
            HelixHelper helixHelper)
        {
            botConfig = botConfigContainer.BotConfig;
            this.helixHelper = helixHelper;
        }

        public async Task<string> GetCheerImageURL(string message, int quantity)
        {
            string imageURL = null;

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


        public async Task<string> GetAnimatedCheerURL(string prefix, int quantity, bool dark = true)
        {
            if (!hasData)
            {
                TwitchCheermotes cheermotes = await helixHelper.GetCheermotes(botConfig.BroadcasterId);

                foreach (TwitchCheermotes.Datum cheermote in cheermotes.Data)
                {
                    cheerLookup.Add(cheermote.Prefix.ToLower(), cheermote.Tiers);
                }

                hasData = true;
            }

            prefix = prefix.ToLower();

            if (cheerLookup.ContainsKey(prefix))
            {
                List<TwitchCheermotes.Datum.Tier> tierList = cheerLookup[prefix];

                TwitchCheermotes.Datum.Tier lastMatch = null;

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

                if (lastMatch != null)
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
}
