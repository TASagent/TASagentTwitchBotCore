using System.Collections.Generic;

namespace TASagentTwitchBot.Core.Audio.Effects
{
    public class FrequencyModulationEffectProvider : AudioEffectProviderBase
    {
        public FrequencyModulationEffectProvider() { }

        public override void RegisterHandler(Dictionary<string, EffectConstructionHandler> handlers)
        {
            //Register under all aliases
            handlers.Add("mod", BuildFrequencyModulationEffect);
            handlers.Add("modulate", BuildFrequencyModulationEffect);
            handlers.Add("modulation", BuildFrequencyModulationEffect);
            handlers.Add("modulater", BuildFrequencyModulationEffect);
            handlers.Add("freqmod", BuildFrequencyModulationEffect);
            handlers.Add("freqmodulate", BuildFrequencyModulationEffect);
            handlers.Add("freqmodulation", BuildFrequencyModulationEffect);
            handlers.Add("freqmodulater", BuildFrequencyModulationEffect);
            handlers.Add("frequencymod", BuildFrequencyModulationEffect);
            handlers.Add("frequencymodulate", BuildFrequencyModulationEffect);
            handlers.Add("frequencymodulater", BuildFrequencyModulationEffect);
            handlers.Add("frequencymodulation", BuildFrequencyModulationEffect);
        }

        private Effect BuildFrequencyModulationEffect(string[] effectArguments, Effect lastEffect)
        {
            if (effectArguments.Length != 3)
            {
                throw new EffectParsingException(
                    $"Incorrect argument count for FrequencyModulator. Expected: 2, Received: {effectArguments.Length - 1}");
            }

            double rate = SafeParseAndVerifyDouble(
                effectData: effectArguments,
                position: 0,
                minValue: 0.125,
                maxValue: 100.0,
                defaultValue: 0.0,
                parameterName: "FrequencyModulator ModulationRate");

            double depth = SafeParseAndVerifyDouble(
                effectData: effectArguments,
                position: 1,
                minValue: 1.0,
                maxValue: 5000.0,
                defaultValue: 0.0,
                parameterName: "FrequencyModulator ModulationDepth");

            return new FrequencyModulationEffect(rate, depth, lastEffect);
        }
    }
}
