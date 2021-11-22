using NAudio.Wave;


namespace BGC.Audio.NAudio;

public sealed class WaveProviderToBGCStream : IBGCStream
{
    private readonly IWaveProvider internalWaveProvider;
    private readonly ISampleProvider internalSampleProvider;
    private bool disposedValue;

    public int Channels => internalSampleProvider.WaveFormat.Channels;

    public int TotalSamples { get; }
    public int ChannelSamples { get; }

    public float SamplingRate => internalSampleProvider.WaveFormat.SampleRate;

    public WaveProviderToBGCStream(
        IWaveProvider stream,
        int channelSamples = int.MaxValue)
    {
        internalWaveProvider = stream;
        internalSampleProvider = stream.ToSampleProvider();

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

    public int Read(float[] buffer, int offset, int count) => internalSampleProvider.Read(buffer, offset, count);

    public void Initialize() { }

    public void Reset()
    {
        if (internalWaveProvider is WaveStream waveStream)
        {
            waveStream.Seek(0, System.IO.SeekOrigin.Begin);
        }
        else if (internalWaveProvider is DisposableWaveProvider disposableWaveStream)
        {
            disposableWaveStream.Seek(0, System.IO.SeekOrigin.Begin);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public void Seek(int position)
    {
        if (internalWaveProvider is WaveStream waveStream)
        {
            waveStream.Seek(position / 2, System.IO.SeekOrigin.Begin);
        }
        else if (internalWaveProvider is DisposableWaveProvider disposableWaveStream)
        {
            disposableWaveStream.Seek(position / 2, System.IO.SeekOrigin.Begin);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public IEnumerable<double> GetChannelRMS() => throw new NotSupportedException();

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (internalWaveProvider is IDisposable disposableStream)
                {
                    disposableStream.Dispose();
                }
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
