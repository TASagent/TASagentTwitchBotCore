using System;
using System.Collections.Generic;

namespace TASagentTwitchBot.Core.Audio.Effects
{
    public class FrequencyShiftEffectProvider : AudioEffectProviderBase
    {
        public FrequencyShiftEffectProvider() { }

        public override void RegisterHandler(Dictionary<string, EffectConstructionHandler> handlers)
        {
            //Register under all aliases
            handlers.Add("shift", BuildFrequencyShifterEffect);
            handlers.Add("shifter", BuildFrequencyShifterEffect);
            handlers.Add("fshift", BuildFrequencyShifterEffect);
            handlers.Add("fshifter", BuildFrequencyShifterEffect);
            handlers.Add("freqshift", BuildFrequencyShifterEffect);
            handlers.Add("freqshifter", BuildFrequencyShifterEffect);
            handlers.Add("frequencyshift", BuildFrequencyShifterEffect);
            handlers.Add("frequencyshifter", BuildFrequencyShifterEffect);
        }

        private Effect BuildFrequencyShifterEffect(string[] effectArguments, Effect lastEffect)
        {
            if (effectArguments.Length != 2)
            {
                throw new EffectParsingException(
                    $"Incorrect argument count for FrequencyShift. Expected: 1, Received: {effectArguments.Length - 1}");
            }

            double shift = SafeParseAndVerifyDouble(
                effectData: effectArguments,
                position: 0,
                isValidDelegate: x => Math.Abs(x) >= 10.0 && Math.Abs(x) <= 1000.0,
                defaultValue: 0.0,
                notValidError: "Must be in the range [-1000,-10] or [10,1000]",
                parameterName: "FrequencyShift ShiftValue");

            return new FrequencyShiftEffect(shift, lastEffect);
        }
    }
}
