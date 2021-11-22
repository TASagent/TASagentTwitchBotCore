﻿using BGC.Mathematics;

namespace BGC.Audio.AnalyticStreams;

public sealed class BGCStreamConverter : IBGCStream
{
    private readonly IAnalyticStream stream;

    public int Channels => 1;

    public int TotalSamples => stream.Samples;

    public int ChannelSamples => stream.Samples;

    public float SamplingRate => (float)stream.SamplingRate;

    private IEnumerable<double>? _channelRMS = null;
    public IEnumerable<double> GetChannelRMS() => _channelRMS ??= new double[] { stream.GetRMS() };

    private const int BUFFER_SIZE = 512;
    private readonly Complex64[] buffer = new Complex64[BUFFER_SIZE];

    public BGCStreamConverter(IAnalyticStream stream)
    {
        this.stream = stream;
    }

    void IBGCStream.Initialize() => stream.Initialize();

    public int Read(float[] data, int offset, int count)
    {
        int samplesRemaining = count;

        while (samplesRemaining > 0)
        {
            int samplesRequested = Math.Min(BUFFER_SIZE, samplesRemaining);
            int samplesRead = stream.Read(buffer, 0, samplesRequested);

            if (samplesRead <= 0)
            {
                break;
            }

            for (int i = 0; i < samplesRead; i++)
            {
                data[offset + i] = (float)buffer[i].Real;
            }

            samplesRemaining -= samplesRead;
            offset += samplesRead;
        }

        return count - samplesRemaining;
    }

    public void Reset() => stream.Reset();

    public void Seek(int position) => stream.Seek(position);

    public void Dispose()
    {
        stream?.Dispose();
    }
}
