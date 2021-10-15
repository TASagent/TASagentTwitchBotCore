using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.Audio.Effects
{
    public interface IAudioEffectSystem
    {
        bool TryParse(string effectsChain, out Effect effect, out string errorMessage);
        Effect SafeParse(string effectsChain);
    }


    public class AudioEffectSystem : IAudioEffectSystem
    {
        private readonly ICommunication communication;
        private readonly IAudioEffectProvider[] effectProviders;
        private readonly Dictionary<string, EffectConstructionHandler> effectHandlers = new Dictionary<string, EffectConstructionHandler>();

        public AudioEffectSystem(
            ICommunication communication,
            IEnumerable<IAudioEffectProvider> effectProviders)
        {
            this.communication = communication;
            this.effectProviders = effectProviders.ToArray();

            foreach (IAudioEffectProvider effectProvider in this.effectProviders)
            {
                effectProvider.RegisterHandler(effectHandlers);
            }
        }

        public Effect SafeParse(string effectsChain)
        {
            if (string.IsNullOrWhiteSpace(effectsChain))
            {
                return new NoEffect();
            }

            effectsChain = effectsChain.Trim().ToLowerInvariant();

            Effect lastEffect = null;

            string[] splitChain = effectsChain.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (splitChain.Length == 0 || (splitChain.Length == 1 && splitChain[0] == "none"))
            {
                return new NoEffect();
            }

            try
            {
                foreach (string effectString in splitChain)
                {
                    string[] splitEffect = effectString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (effectHandlers.TryGetValue(splitEffect[0], out EffectConstructionHandler matchingHandler))
                    {
                        lastEffect = matchingHandler(splitEffect, lastEffect);
                    }
                    else
                    {
                        throw new EffectParsingException($"Unexpected effect name: {splitEffect[0]}");
                    }
                }
            }
            catch (EffectParsingException effectParsingException)
            {
                communication.SendWarningMessage(effectParsingException.ErrorMessage);
                return new NoEffect();
            }

            return lastEffect;
        }

        public bool TryParse(string effectsChain, out Effect effect, out string errorMessage)
        {
            effect = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(effectsChain))
            {
                effect = new NoEffect();
                return true;
            }

            effectsChain = effectsChain.Trim().ToLowerInvariant();

            string[] splitChain = effectsChain.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (splitChain.Length == 0 || (splitChain.Length == 1 && splitChain[0] == "none"))
            {
                effect = new NoEffect();
                return true;
            }

            try
            {
                foreach (string effectString in splitChain)
                {
                    string[] splitEffect = effectString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (effectHandlers.TryGetValue(splitEffect[0], out EffectConstructionHandler matchingHandler))
                    {
                        effect = matchingHandler(splitEffect, effect);
                    }
                    else
                    {
                        throw new EffectParsingException($"Unexpected effect name: {splitEffect[0]}");
                    }
                }
            }
            catch (EffectParsingException effectParsingException)
            {
                communication.SendWarningMessage(effectParsingException.ErrorMessage);
                effect = new NoEffect();
                errorMessage = effectParsingException.ErrorMessage;

                return false;
            }

            return true;
        }
    }

    public delegate Effect EffectConstructionHandler(string[] effectArguments, Effect lastEffect);

}
