using NAudio.Wave;

namespace BGC.Audio.NAudio;

public class DisposableWaveProvider : IWaveProvider, IDisposable
{
    private readonly IWaveProvider waveProvider;
    private bool disposedValue;

    public DisposableWaveProvider(IWaveProvider waveProvider) => this.waveProvider = waveProvider;

    public WaveFormat WaveFormat => waveProvider.WaveFormat;

    public int Read(byte[] buffer, int offset, int count) => waveProvider.Read(buffer, offset, count);

    public long Seek(long offset, SeekOrigin origin)
    {
        if (waveProvider is WaveStream waveStream)
        {
            return waveStream.Seek(offset, SeekOrigin.Begin);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (waveProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
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
