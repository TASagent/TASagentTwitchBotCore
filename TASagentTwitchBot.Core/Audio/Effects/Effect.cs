using BGC.Audio;
using BGC.Audio.NAudio;
using BGC.Audio.Filters;

namespace TASagentTwitchBot.Core.Audio.Effects;

public abstract class Effect
{
    protected readonly Effect? prior;

    public Effect(Effect? prior)
    {
        this.prior = prior;
    }

    protected abstract int RequestedLatency { get; }

    public IEnumerable<Effect> GetEffects()
    {
        if (prior is not null)
        {
            return prior.GetEffects().Append(this);
        }

        return new Effect[] { this };
    }

    public int GetRequestedLatency()
    {
        if (prior is null)
        {
            return RequestedLatency;
        }

        return RequestedLatency + prior.GetRequestedLatency();
    }

    public IBGCStream ApplyEffects(IBGCStream input)
    {
        if (prior is null)
        {
            return ApplyEffect(input);
        }

        return ApplyEffect(prior.ApplyEffects(input));
    }

    public string GetEffectsChain()
    {
        string priorStrings;

        if (prior is not null)
        {
            priorStrings = prior.GetEffectsChain() + ",";
        }
        else
        {
            priorStrings = "";
        }

        return priorStrings + GetEffectString();
    }

    public abstract Effect GetClone();

    protected abstract string GetEffectString();

    protected abstract IBGCStream ApplyEffect(IBGCStream input);


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

public class NoEffect : Effect
{
    protected override int RequestedLatency => 0;

    public NoEffect() : base(null) { }

    protected override IBGCStream ApplyEffect(IBGCStream input) => input;

    protected override string GetEffectString() => "None";

    public override Effect GetClone() => new NoEffect();
}


public class FrequencyShiftEffect : Effect
{
    protected override int RequestedLatency => 5;
    private readonly double shift;

    public FrequencyShiftEffect(double shift, Effect? prior)
        : base(prior)
    {
        this.shift = shift;
    }

    protected override string GetEffectString() => $"FreqShift {shift:N1}";

    protected override IBGCStream ApplyEffect(IBGCStream input) => input.FrequencyShift(shift);

    public override Effect GetClone() => new FrequencyShiftEffect(shift, prior?.GetClone());
}

public class ChorusEffect : Effect
{
    protected override int RequestedLatency => Math.Min(20, maxDelay);

    private readonly int minDelay;
    private readonly int maxDelay;
    private readonly double rate;

    private readonly ChorusEffector.DelayType delayType;

    public ChorusEffect(Effect? prior)
        : this(40, 60, 0.25, ChorusEffector.DelayType.Sine, prior)
    {
    }

    public ChorusEffect(int minDelay, int maxDelay, Effect? prior)
        : this(minDelay, maxDelay, 0.25, ChorusEffector.DelayType.Sine, prior)
    {
    }

    public ChorusEffect(int minDelay, int maxDelay, double rate, ChorusEffector.DelayType delayType, Effect? prior)
        : base(prior)
    {
        this.minDelay = minDelay;
        this.maxDelay = maxDelay;
        this.rate = rate;
        this.delayType = delayType;
    }

    public static ChorusEffector.DelayType TranslateDelayType(string input) =>
        (input.ToLowerInvariant()) switch
        {
            "sin" or "sine" => ChorusEffector.DelayType.Sine,
            "triangle" or "tri" => ChorusEffector.DelayType.Triangle,
            _ => ChorusEffector.DelayType.MAX,
        };

    public static string TranslateDelayType(ChorusEffector.DelayType input) =>
        (input) switch
        {
            ChorusEffector.DelayType.Sine => "Sine",
            ChorusEffector.DelayType.Triangle => "Triangle",
            _ => "undefined",
        };


    protected override string GetEffectString() => $"Chorus {minDelay} {maxDelay} {rate:N5} {TranslateDelayType(delayType)}";

    protected override IBGCStream ApplyEffect(IBGCStream input) => input.ChorusEffector(0.001 * minDelay, 0.001 * maxDelay, rate);

    public override Effect GetClone() => new ChorusEffect(minDelay, maxDelay, rate, delayType, prior?.GetClone());
}

public class EchoEffect : Effect
{
    protected override int RequestedLatency => Math.Min(20, delay);

    private readonly int delay;
    private readonly double residual;

    public EchoEffect(Effect? prior)
        : this(200, 0.3, prior)
    {
    }

    public EchoEffect(int delay, double residual, Effect? prior)
        : base(prior)
    {
        this.delay = delay;
        this.residual = residual;
    }

    protected override string GetEffectString() => $"Echo {delay} {residual:N5}";

    protected override IBGCStream ApplyEffect(IBGCStream input) => input.EchoEffector(0.001 * delay, residual);

    public override Effect GetClone() => new EchoEffect(delay, residual, prior?.GetClone());
}

public class ReverbEffect : Effect
{
    protected override int RequestedLatency => 20;

    private readonly ReverbIRF reverbIRF;

    public ReverbEffect(
        ReverbIRF reverbIRF,
        Effect? prior)
        : base(prior)
    {
        this.reverbIRF = reverbIRF;
    }

    protected override string GetEffectString() => $"Reverb {reverbIRF.Name}";

    protected override IBGCStream ApplyEffect(IBGCStream input)
    {
        using DisposableWaveProvider irFileReader = AudioTools.GetWaveProvider(reverbIRF.FilePath);

        IBGCStream filter = irFileReader.ToBGCStream().StereoStreamScaler(1.0 / reverbIRF.Gain).SafeCache();

        return input.EnsureMono().MultiConvolve(filter);
    }

    public override Effect GetClone() => new ReverbEffect(reverbIRF, prior?.GetClone());
}

public class PitchShiftEffect : Effect
{
    private PitchShiftFilter? lastPitchShiftFilter = null;

    protected override int RequestedLatency => 5;

    private double pitchValue;
    public double PitchFactor
    {
        get => pitchValue;
        set
        {
            pitchValue = value;
            if (lastPitchShiftFilter is not null)
            {
                lastPitchShiftFilter.PitchFactor = pitchValue;
            }
        }
    }

    public PitchShiftEffect(double pitchValue, Effect? prior)
        : base(prior)
    {
        this.pitchValue = pitchValue;
    }

    protected override string GetEffectString() => $"PitchShift {pitchValue:N5}";

    protected override IBGCStream ApplyEffect(IBGCStream input)
    {
        lastPitchShiftFilter = new PitchShiftFilter(input, PitchFactor);
        return lastPitchShiftFilter;
    }

    public override Effect GetClone() => new PitchShiftEffect(pitchValue, prior?.GetClone());
}

public class NoiseVocodeEffect : Effect
{
    private readonly int bands;
    private readonly int fftSize;
    protected override int RequestedLatency => 50;

    public NoiseVocodeEffect(int bands, Effect? prior)
        : this(bands, 1 << 13, prior)
    {
    }

    public NoiseVocodeEffect(int bands, int fftSize, Effect? prior)
        : base(prior)
    {
        this.bands = bands;
        this.fftSize = fftSize;
    }

    protected override string GetEffectString() => $"Vocode {bands}";

    protected override IBGCStream ApplyEffect(IBGCStream input) => input.NoiseVocode(bandCount: bands, fftSize: fftSize, overlapRatio: 4);

    public override Effect GetClone() => new NoiseVocodeEffect(bands, fftSize, prior?.GetClone());
}

public class FrequencyModulationEffect : Effect
{
    private readonly double rate;
    private readonly double depth;
    protected override int RequestedLatency => 5;

    public FrequencyModulationEffect(double rate, double depth, Effect? prior)
        : base(prior)
    {
        this.rate = rate;
        this.depth = depth;
    }

    protected override string GetEffectString() => $"Modulate {rate:N3} {depth:N2}";

    protected override IBGCStream ApplyEffect(IBGCStream input) => input.FrequencyModulation(rate, depth);
    public override Effect GetClone() => new FrequencyModulationEffect(rate, depth, prior?.GetClone());
}

public class EffectParsingException : Exception
{
    public string ErrorMessage { get; }

    public EffectParsingException(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }
}
