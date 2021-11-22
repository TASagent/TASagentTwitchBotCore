namespace TASagentTwitchBot.Core.Audio.Effects;

public class NoiseVocoderEffectProvider : AudioEffectProviderBase
{
    public NoiseVocoderEffectProvider() { }

    public override void RegisterHandler(Dictionary<string, EffectConstructionHandler> handlers)
    {
        //Register under all aliases
        handlers.Add("vocode", BuildNoiseVocoderEffect);
        handlers.Add("vocoder", BuildNoiseVocoderEffect);
        handlers.Add("vocoded", BuildNoiseVocoderEffect);
        handlers.Add("noise", BuildNoiseVocoderEffect);
        handlers.Add("noisevocode", BuildNoiseVocoderEffect);
        handlers.Add("noisevocoded", BuildNoiseVocoderEffect);
        handlers.Add("noisevocoder", BuildNoiseVocoderEffect);
    }

    private Effect BuildNoiseVocoderEffect(string[] effectArguments, Effect? lastEffect)
    {
        if (effectArguments.Length > 2)
        {
            throw new EffectParsingException(
                $"Incorrect argument count for NoiseVocoder. Expected: 0 or 1, Received: {effectArguments.Length - 1}");
        }

        int bands = SafeParseAndVerifyInt(
            effectData: effectArguments,
            position: 0,
            minValue: 1,
            maxValue: 40,
            defaultValue: 22,
            parameterName: "NoiseVocoder BandCount");

        int fftSize = 1 << 13;

        return new NoiseVocodeEffect(bands, fftSize, lastEffect);
    }
}
