namespace TASagentTwitchBot.Core.Audio.Effects;

public class EchoEffectProvider : AudioEffectProviderBase
{
    public EchoEffectProvider() { }

    public override void RegisterHandler(Dictionary<string, EffectConstructionHandler> handlers)
    {
        //Register under all aliases
        handlers.Add("echo", BuildEchoEffect);
    }

    private Effect BuildEchoEffect(string[] effectArguments, Effect? lastEffect)
    {
        if (effectArguments.Length >= 4)
        {
            throw new EffectParsingException(
                $"Incorrect argument count for EchoEffect. Expected: 0, 1, or 2, Received: {effectArguments.Length - 1}");
        }

        int delay = SafeParseAndVerifyInt(
            effectData: effectArguments,
            position: 0,
            minValue: 10,
            maxValue: 2000,
            defaultValue: 200,
            parameterName: "EchoEffect Delay");

        double residual = SafeParseAndVerifyDouble(
            effectData: effectArguments,
            position: 1,
            minValue: 0.01,
            maxValue: 0.5,
            defaultValue: 0.3,
            parameterName: "EchoEffect Residual");

        return new EchoEffect(delay, residual, lastEffect);
    }
}
