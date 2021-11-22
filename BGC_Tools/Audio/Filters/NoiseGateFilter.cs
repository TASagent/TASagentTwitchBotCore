namespace BGC.Audio.Filters;

public class NoiseGateFilter : SimpleBGCFilter
{
    public override int Channels => stream.Channels;

    public override int TotalSamples => stream.TotalSamples;
    public override int ChannelSamples => stream.ChannelSamples;

    private readonly double openThreshold;
    private readonly double closeThreshold;

    private readonly float attackRate;
    private readonly float releaseRate;

    private readonly double decayRate;
    private readonly double holdDuration;

    private readonly double inverseSampleRate;

    private readonly TransformRMSBehavior rmsBehavior;

    private bool isOpen;
    private float attenuation;
    private double level;
    private double heldTime;

    public NoiseGateFilter(
        IBGCStream stream,
        double openThreshold = -26.0,
        double closeThreshold = -32.0,
        double attackDuration = 0.025,
        double holdDuration = 0.200,
        double releaseDuration = 0.150,
        TransformRMSBehavior rmsBehavior = TransformRMSBehavior.Passthrough)
        : base(stream)
    {
        this.openThreshold = Math.Pow(10.0, openThreshold / 20.0);
        this.closeThreshold = Math.Pow(10.0, closeThreshold / 20.0);

        attackRate = (float)(1.0 / (attackDuration * SamplingRate));
        releaseRate = (float)(1.0 / (releaseDuration * SamplingRate));

        double thresholdDiff = this.openThreshold - this.closeThreshold;
        double minDecayPeriod = (1.0 / 75.0) * SamplingRate;

        decayRate = thresholdDiff / minDecayPeriod;
        this.holdDuration = holdDuration;

        isOpen = false;
        attenuation = 0;
        level = 0;
        heldTime = 0;

        inverseSampleRate = 1.0 / SamplingRate;

        this.rmsBehavior = rmsBehavior;
    }

    public override int Read(float[] data, int offset, int count)
    {
        int samplesWritten = stream.Read(data, offset, count);

        //Process Samples
        for (int i = 0; i < samplesWritten; i++)
        {
            float curLevel = Math.Abs(data[offset + i]);
            for (int c = 1; c < Channels; c++)
            {
                curLevel = Math.Max(curLevel, Math.Abs(data[offset + i + c]));
            }

            if (curLevel > openThreshold && !isOpen)
            {
                isOpen = true;
            }

            if (level < closeThreshold && isOpen)
            {
                heldTime = 0;
                isOpen = false;
            }

            level = Math.Max(level, curLevel) - decayRate;

            if (isOpen)
            {
                attenuation = Math.Min(1, attenuation + attackRate);
            }
            else
            {
                heldTime += inverseSampleRate;
                if (heldTime > holdDuration)
                {
                    attenuation = Math.Max(0, attenuation - releaseRate);
                }
            }

            for (int c = 0; c < Channels; c++)
            {
                data[offset + i + c] *= attenuation;
            }

            i += Channels - 1;
        }

        return samplesWritten;
    }

    public override void Reset()
    {
        isOpen = false;
        attenuation = 0;
        level = 0;
        heldTime = 0;

        stream.Reset();
    }

    public override void Seek(int position)
    {
        isOpen = false;
        attenuation = 0;
        level = 0;
        heldTime = 0;

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
