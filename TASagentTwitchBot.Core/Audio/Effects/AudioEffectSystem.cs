using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.Audio.Effects
{
    public interface IAudioEffectSystem
    {
        Effect Parse(string effectsChain);
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

            foreach (IAudioEffectProvider effectProvider in effectProviders)
            {
                effectProvider.RegisterHandler(effectHandlers);
            }
        }

        public Effect Parse(string effectsChain)
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

                    if (!effectHandlers.ContainsKey(splitEffect[0]))
                    {
                        throw new EffectParsingException($"Unexpected effect name: {splitEffect[0]}");
                    }

                    lastEffect = effectHandlers[splitEffect[0]](splitEffect, lastEffect);
                }
            }
            catch (EffectParsingException effectParsingException)
            {
                communication.SendWarningMessage(effectParsingException.ErrorMessage);
                return new NoEffect();
            }

            return lastEffect;
        }
    }

    public delegate Effect EffectConstructionHandler(string[] effectArguments, Effect lastEffect);

}
