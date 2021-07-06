using System;
using System.Collections.Generic;
using BGC.Audio.Filters;

namespace TASagentTwitchBot.Core.Audio.Effects
{
    public class ChorusEffectProvider : AudioEffectProviderBase
    {
        public ChorusEffectProvider() { }

        public override void RegisterHandler(Dictionary<string, EffectConstructionHandler> handlers)
        {
            //Register under all aliases
            handlers.Add("chor", BuildChorusEffect);
            handlers.Add("chorus", BuildChorusEffect);
            handlers.Add("choruseffect", BuildChorusEffect);
        }

        private Effect BuildChorusEffect(string[] effectArguments, Effect lastEffect)
        {
            if (effectArguments.Length == 2 || effectArguments.Length > 5)
            {
                throw new EffectParsingException(
                    $"Incorrect argument count for ChorusEffect. Expected: 0, 2, or 3, Received: {effectArguments.Length - 1}");
            }

            int minDelay = SafeParseAndVerifyInt(
                effectData: effectArguments,
                position: 0,
                minValue: 0,
                maxValue: 200,
                defaultValue: 40,
                parameterName: "ChorusEffect MinDelay");

            int maxDelay = SafeParseAndVerifyInt(
                effectData: effectArguments,
                position: 1,
                minValue: 0,
                maxValue: 200,
                defaultValue: 60,
                parameterName: "ChorusEffect MaxDelay");

            if (maxDelay < minDelay)
            {
                //swap
                (maxDelay, minDelay) = (minDelay, maxDelay);
            }

            double rate = SafeParseAndVerifyDouble(
                effectData: effectArguments,
                position: 2,
                minValue: 0.125,
                maxValue: 100.0,
                defaultValue: 0.25,
                parameterName: "ChorusEffect Rate");

            ChorusEffector.DelayType delayType = SafeParseAndVerifyEnum(
                effectData: effectArguments,
                position: 3,
                translationDelegate: ChorusEffect.TranslateDelayType,
                isValidDelegate: x => x != ChorusEffector.DelayType.MAX,
                defaultValue: ChorusEffector.DelayType.Sine,
                notValidError: "Must be \"Sine\" or \"Triangle\"",
                parameterName: "ChorusEffect DelayType");

            return new ChorusEffect(minDelay, maxDelay, rate, delayType, lastEffect);
        }
    }
}
