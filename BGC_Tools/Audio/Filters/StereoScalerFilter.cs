namespace BGC.Audio.Filters;

/// <summary>
/// Scales an underlying stream to the desired level (dB SPL)
/// </summary>
public class StereoScalerFilter : SimpleBGCFilter
{
    public override int Channels => stream.Channels;
    public override int TotalSamples => stream.TotalSamples;
    public override int ChannelSamples => stream.ChannelSamples;

    private readonly float leftFactor;
    private readonly float rightFactor;

    public StereoScalerFilter(
        IBGCStream stream,
        double leftFactor,
        double rightFactor)
        : base(stream)
    {
        if (stream.Channels != 2)
        {
            throw new StreamCompositionException("StereoScalerFilter inner stream must have two channels.");
        }

        this.leftFactor = (float)leftFactor;
        this.rightFactor = (float)rightFactor;
    }

    public override int Read(float[] data, int offset, int count)
    {
        if (!initialized)
        {
            Initialize();
        }

        int samplesRead = stream.Read(data, offset, count);

        for (int i = 0; i < samplesRead; i += 2)
        {
            data[offset + i] *= leftFactor;
            data[offset + i + 1] *= rightFactor;
        }

        return samplesRead;
    }

    private IEnumerable<double>? _channelRMS = null;
    public override IEnumerable<double> GetChannelRMS()
    {
        if (_channelRMS is null)
        {
            double[] innerRMS = stream.GetChannelRMS().ToArray();
            innerRMS[0] *= Math.Abs(leftFactor);
            innerRMS[1] *= Math.Abs(rightFactor);

            _channelRMS = innerRMS;
        }

        return _channelRMS;
    }
}
