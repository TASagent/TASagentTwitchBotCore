namespace BGC.Audio.Filters;

public class StreamLimiter : SimpleBGCFilter
{
    public override int Channels => stream.Channels;
    public override int TotalSamples => stream.TotalSamples;
    public override int ChannelSamples => stream.ChannelSamples;

    const float LIM_THRESH = 0.95f;
    const double LIM_RANGE = (1.0 - LIM_THRESH);
    const double INV_LIM_RANGE = 1.0 / (1.0 - LIM_THRESH);
    const double TWO_LIM_RANGE_OVER_PI = 2.0 * LIM_RANGE / Math.PI;

    public StreamLimiter(IBGCStream stream)
        : base(stream)
    {
    }

    /// <summary>
    /// Read from this sample provider
    /// </summary>
    public override int Read(float[] data, int offset, int count)
    {
        int read = stream.Read(data, offset, count);

        for (int i = offset; i < offset + read; i++)
        {
            if (data[i] > LIM_THRESH)
            {
                data[i] = (float)(Math.Atan((data[i] - LIM_THRESH) * INV_LIM_RANGE) * TWO_LIM_RANGE_OVER_PI + LIM_THRESH);
            }
            else if (data[i] < -LIM_THRESH)
            {
                data[i] = -(float)(Math.Atan((LIM_THRESH - data[i]) * INV_LIM_RANGE) * TWO_LIM_RANGE_OVER_PI + LIM_THRESH);
            }
        }

        return read;
    }


    public override IEnumerable<double> GetChannelRMS() => stream.GetChannelRMS();

}
