using System;
using System.Collections.Generic;

namespace TASagentTwitchBot.Core.Audio.Effects
{
    public class ReverbEffectProvider : AudioEffectProviderBase
    {
        private readonly ISoundEffectSystem soundEffectSystem;

        public ReverbEffectProvider(
            ISoundEffectSystem soundEffectSystem)
        {
            this.soundEffectSystem = soundEffectSystem;
        }

        public override void RegisterHandler(Dictionary<string, EffectConstructionHandler> handlers)
        {
            //Register under all aliases
            handlers.Add("reverb", BuildReverbEffect);
            handlers.Add("rev", BuildReverbEffect);
        }

        private Effect BuildReverbEffect(string[] effectArguments, Effect lastEffect)
        {
            if (effectArguments.Length > 2)
            {
                throw new EffectParsingException(
                    $"Incorrect argument count for ReverbEffect. Expected: 0 or 1, Received: {effectArguments.Length - 1}");
            }

            List<string> reverbEffects = soundEffectSystem.GetReverbEffects();

            if (reverbEffects.Count == 0)
            {
                throw new EffectParsingException("No reverb effects registered. Cannot apply.");
            }

            ReverbIRF reverbIRF;

            if (effectArguments.Length >= 2)
            {
                //Supplied reverbType
                reverbIRF = soundEffectSystem.GetReverbEffectByAlias(effectArguments[1]);

                if (reverbIRF is null)
                {
                    throw new EffectParsingException($"Reverb Effect not recognized: {effectArguments[1]}.");
                }
            }
            else
            {
                //Grab the first as the default, I guess
                reverbIRF = soundEffectSystem.GetReverbEffectByName(reverbEffects[0]);

                if (reverbIRF is null)
                {
                    throw new EffectParsingException($"No reverb effects registered!");
                }
            }


            return new ReverbEffect(reverbIRF, lastEffect);
        }
    }
}
