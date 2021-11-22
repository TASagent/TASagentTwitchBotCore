namespace BGC.Audio.Filters;

public class ExpanderFilter : SimpleBGCFilter
{
    public override int Channels => stream.Channels;

    public override int TotalSamples => stream.TotalSamples;
    public override int ChannelSamples => stream.ChannelSamples;

    private readonly double threshold;

    private readonly float attackGain;
    private readonly float releaseGain;

    private readonly float outputGain;

    private readonly double slope;

    private readonly TransformRMSBehavior rmsBehavior;

    private readonly float rmscoef;

    private float lastGain;
    private float runningAverage;

    public ExpanderFilter(
        IBGCStream stream,
        double ratio = 2.0,
        double threshold = -40.0,
        double attackDuration = 0.010,
        double releaseDuration = 0.050,
        double outputGain = 0.0,
        TransformRMSBehavior rmsBehavior = TransformRMSBehavior.Passthrough)
        : base(stream)
    {
        this.threshold = threshold;

        attackGain = (float)Math.Exp(-1.0 / (SamplingRate * attackDuration));
        releaseGain = (float)Math.Exp(-1.0 / (SamplingRate * releaseDuration));

        this.outputGain = (float)Math.Pow(10.0, outputGain / 20.0);

        slope = 1.0 - ratio;

        lastGain = 0f;
        runningAverage = 0f;

        rmscoef = (float)Math.Pow(2, -100.0 / SamplingRate);

        this.rmsBehavior = rmsBehavior;
    }

    public override int Read(float[] data, int offset, int count)
    {
        int samplesWritten = stream.Read(data, offset, count);

        float env;
        float envdB;
        float gain;
        float gainFactor;

        //Process Samples
        for (int i = 0; i < samplesWritten; i++)
        {
            float curLevel = Math.Abs(data[offset + i]);
            for (int c = 1; c < Channels; c++)
            {
                curLevel = Math.Max(curLevel, Math.Abs(data[offset + i + c]));
            }

            runningAverage = rmscoef * runningAverage + (1 - rmscoef) * curLevel * curLevel;
            env = (float)Math.Sqrt(Math.Max(0, runningAverage));
            envdB = (env == 0.0f) ? float.NegativeInfinity : (20.0f * (float)Math.Log10(env));
            gain = threshold - envdB > 0f ? Math.Max((float)(slope * (threshold - envdB)), -60f) : 0f;

            if (gain > lastGain)
            {
                gain = attackGain * lastGain + (1f - attackGain) * gain;
            }
            else
            {
                gain = releaseGain * lastGain + (1f - releaseGain) * gain;
            }

            //Save gain for next sample
            lastGain = gain;

            gainFactor = (float)Math.Pow(10.0, Math.Min(0, gain) / 20.0);

            for (int c = 0; c < Channels; c++)
            {
                data[offset + i + c] *= gainFactor * outputGain;
            }

            i += Channels - 1;
        }

        return samplesWritten;
    }

    public override void Reset()
    {
        lastGain = 0;
        runningAverage = 0f;

        stream.Reset();
    }

    public override void Seek(int position)
    {
        lastGain = 0;
        runningAverage = 0f;

        stream.Seek(position);
    }

    private IEnumerable<double>? _channelRMS = null;
    public override IEnumerable<double> GetChannelRMS()
    {
        if (_channelRMS is null)
        {
            switch (rmsBehavior)
            {
                case TransformRMSBehavior.Recalculate:
                    _channelRMS = this.CalculateRMS();
                    break;

                case TransformRMSBehavior.Passthrough:
                    _channelRMS = stream.GetChannelRMS();
                    break;

                default:
                    throw new Exception($"Unexpected rmsBehavior: {rmsBehavior}");
            }
        }

        return _channelRMS;
    }
}
