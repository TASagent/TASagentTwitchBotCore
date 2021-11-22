using NAudio.Wave;


namespace BGC.Audio.NAudio;

public sealed class SampleProviderToBGCStream : IBGCStream
{
    private readonly ISampleProvider internalStream;

    public int Channels => internalStream.WaveFormat.Channels;

    public int TotalSamples { get; }
    public int ChannelSamples { get; }

    public float SamplingRate => internalStream.WaveFormat.SampleRate;

    public SampleProviderToBGCStream(
        ISampleProvider stream,
        int channelSamples = int.MaxValue)
    {
        internalStream = stream;

        ChannelSamples = channelSamples;

        if (channelSamples == int.MaxValue)
        {
            TotalSamples = int.MaxValue;
        }
        else
        {
            TotalSamples = Channels * channelSamples;
        }
    }

    public int Read(float[] buffer, int offset, int count) => internalStream.Read(buffer, offset, count);

    public void Initialize() { }

    public void Reset() => throw new NotSupportedException();

    public void Seek(int position) => throw new NotSupportedException();

    public IEnumerable<double> GetChannelRMS() => throw new NotSupportedException();

    public void Dispose() { }
}
