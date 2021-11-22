namespace TASagentTwitchBot.Core.Audio.Effects;

public interface IAudioEffectProvider
{
    void RegisterHandler(Dictionary<string, EffectConstructionHandler> handlers);
}

public abstract class AudioEffectProviderBase : IAudioEffectProvider
{
    public abstract void RegisterHandler(Dictionary<string, EffectConstructionHandler> handlers);

    protected static int SafeParseAndVerifyInt(
        string[] effectData,
        int position,
        int minValue,
        int maxValue,
        int defaultValue,
        string parameterName)
    {
        if (position + 1 >= effectData.Length)
        {
            return defaultValue;
        }

        string parameterData = effectData[position + 1];

        if (!int.TryParse(parameterData, out int value))
        {
            throw new EffectParsingException(
                $"Unable to parse {parameterName}. Received: {parameterData}");
        }

        if (value < minValue || value > maxValue)
        {
            throw new EffectParsingException(
                $"Invalid {parameterName}. Must be in the range [{minValue},{maxValue}]. Received: {parameterData}");
        }

        return value;
    }

    protected static double SafeParseAndVerifyDouble(
        string[] effectData,
        int position,
        double minValue,
        double maxValue,
        double defaultValue,
        string parameterName)
    {
        if (position + 1 >= effectData.Length)
        {
            return defaultValue;
        }

        string parameterData = effectData[position + 1];

        if (!double.TryParse(parameterData, out double value))
        {
            throw new EffectParsingException(
                $"Unable to parse {parameterName}. Received: {parameterData}");
        }

        if (value < minValue || value > maxValue)
        {
            throw new EffectParsingException(
                $"Invalid {parameterName}. Must be in the range [{minValue},{maxValue}]. Received: {parameterData}");
        }

        return value;
    }

    protected static double SafeParseAndVerifyDouble(
        string[] effectData,
        int position,
        Func<double, bool> isValidDelegate,
        double defaultValue,
        string notValidError,
        string parameterName)
    {
        if (position + 1 >= effectData.Length)
        {
            return defaultValue;
        }

        string parameterData = effectData[position + 1];

        if (!double.TryParse(parameterData, out double value))
        {
            throw new EffectParsingException(
                $"Unable to parse {parameterName}. Received: {parameterData}");
        }

        if (!isValidDelegate(value))
        {
            throw new EffectParsingException(
                $"Invalid {parameterName}. {notValidError}. Received: {parameterData}");
        }

        return value;
    }

    protected static T SafeParseAndVerifyEnum<T>(
        string[] effectData,
        int position,
        Func<string, T> translationDelegate,
        Func<T, bool> isValidDelegate,
        T defaultValue,
        string notValidError,
        string parameterName)
    {
        if (position + 1 >= effectData.Length)
        {
            return defaultValue;
        }

        string parameterData = effectData[position + 1];

        T value = translationDelegate(parameterData);

        if (!isValidDelegate(value))
        {
            throw new EffectParsingException(
                $"Invalid {parameterName}. {notValidError}. Received: {parameterData}");
        }

        return value;
    }
}
