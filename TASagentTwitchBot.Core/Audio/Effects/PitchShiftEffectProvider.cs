namespace TASagentTwitchBot.Core.Audio.Effects;

public class PitchShiftEffectProvider : AudioEffectProviderBase
{
    public PitchShiftEffectProvider() { }

    public override void RegisterHandler(Dictionary<string, EffectConstructionHandler> handlers)
    {
        //Register under all aliases
        handlers.Add("pitch", BuildPitchShifterEffect);
        handlers.Add("pshift", BuildPitchShifterEffect);
        handlers.Add("pshifter", BuildPitchShifterEffect);
        handlers.Add("pitchshift", BuildPitchShifterEffect);
        handlers.Add("pitchshifter", BuildPitchShifterEffect);
    }

    private Effect BuildPitchShifterEffect(string[] effectArguments, Effect? lastEffect)
    {
        if (effectArguments.Length != 2)
        {
            throw new EffectParsingException(
                $"Incorrect argument count for PitchShift. Expected: 1, Received: {effectArguments.Length - 1}");
        }

        double pitchValue = SafeParseAndVerifyDouble(
            effectData: effectArguments,
            position: 0,
            minValue: 0.125,
            maxValue: 8.0,
            defaultValue: 0.0,
            parameterName: "PitchShift ShiftValue");

        return new PitchShiftEffect(pitchValue, lastEffect);
    }
}
