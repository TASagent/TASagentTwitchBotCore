namespace BGC.Audio.Filters;

public class CompressorFilter : SimpleBGCFilter
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

    private float envelope;

    public CompressorFilter(
        IBGCStream stream,
        double ratio = 4.0,
        double threshold = -20.0,
        double attackDuration = 0.006,
        double releaseDuration = 0.060,
        double outputGain = 0.0,
        TransformRMSBehavior rmsBehavior = TransformRMSBehavior.Passthrough)
        : base(stream)
    {
        this.threshold = threshold;

        attackGain = (float)Math.Exp(-1.0 / (SamplingRate * attackDuration));
        releaseGain = (float)Math.Exp(-1.0 / (SamplingRate * releaseDuration));

        this.outputGain = (float)Math.Pow(10.0, outputGain / 20.0);

        slope = 1.0 - (1.0 / ratio);

        envelope = 0f;

        this.rmsBehavior = rmsBehavior;
    }

    public override int Read(float[] data, int offset, int count)
    {
        int samplesWritten = stream.Read(data, offset, count);

        double gain;
        float gainFactor;

        //Process Samples
        for (int i = 0; i < samplesWritten; i++)
        {
            float curLevel = Math.Abs(data[offset + i]);
            for (int c = 1; c < Channels; c++)
            {
                curLevel = Math.Max(curLevel, Math.Abs(data[offset + i + c]));
            }

            if (envelope < curLevel)
            {
                envelope = curLevel + attackGain * (envelope - curLevel);
            }
            else
            {
                envelope = curLevel + releaseGain * (envelope - curLevel);
            }

            gain = slope * (threshold - 20.0 * Math.Log10(envelope));

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
        envelope = 0;

        stream.Reset();
    }

    public override void Seek(int position)
    {
        envelope = 0;

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
