namespace BGC.Audio.Filters;

/// <summary>
/// Scales an underlying stream to the desired level (dB SPL)
/// </summary>
public class MonoScalerFilter : SimpleBGCFilter
{
    public override int Channels => stream.Channels;
    public override int TotalSamples => stream.TotalSamples;
    public override int ChannelSamples => stream.ChannelSamples;

    private readonly float factor;

    public MonoScalerFilter(
        IBGCStream stream,
        float factor)
        : base(stream)
    {
        if (stream.Channels != 1)
        {
            throw new StreamCompositionException("MonoScalerFilter inner stream must have one channel.");
        }

        this.factor = factor;
    }

    public override int Read(float[] data, int offset, int count)
    {
        if (!initialized)
        {
            Initialize();
        }

        int samplesRead = stream.Read(data, offset, count);

        for (int i = 0; i < samplesRead; i++)
        {
            data[offset + i] *= factor;
        }

        return samplesRead;
    }

    private IEnumerable<double>? _channelRMS = null;
    public override IEnumerable<double> GetChannelRMS()
    {
        if (_channelRMS is null)
        {
            double[] innerRMS = stream.GetChannelRMS().ToArray();
            innerRMS[0] *= Math.Abs(factor);

            _channelRMS = innerRMS;
        }

        return _channelRMS;
    }
}
