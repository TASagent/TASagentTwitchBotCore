﻿namespace BGC.Audio.Filters;

/// <summary>
/// Upchannels the stream, including only the indicated channels
/// </summary>
public class StreamSelectiveUpChanneler : SimpleBGCFilter
{
    public override int TotalSamples => stream.ChannelSamples == int.MaxValue ? int.MaxValue : Channels * ChannelSamples;
    public override int ChannelSamples => stream.ChannelSamples;

    public override int Channels => 2;

    private readonly AudioChannel channels;

    private const int BUFFER_SIZE = 512;
    private readonly float[] buffer = new float[BUFFER_SIZE];

    public StreamSelectiveUpChanneler(IBGCStream stream, AudioChannel channels)
        : base(stream)
    {
        if (stream.Channels != 1)
        {
            throw new StreamCompositionException("StreamSelectiveUpChanneler inner stream must have only one channel.");
        }

        this.channels = channels;
    }

    public override int Read(float[] data, int offset, int count)
    {
        int samplesRemaining = count;

        while (samplesRemaining > 0)
        {
            int samplesToRead = Math.Min(BUFFER_SIZE, samplesRemaining / 2);
            int samplesRead = stream.Read(buffer, 0, samplesToRead);

            if (samplesRead <= 0)
            {
                //We ran out of samples.
                break;
            }

            switch (channels)
            {
                case AudioChannel.Left:
                case AudioChannel.Right:
                    int channelAdj = (int)channels;
                    for (int i = 0; i < samplesRead; i++)
                    {
                        data[offset + 2 * i + channelAdj] = buffer[i];
                    }
                    break;

                case AudioChannel.Both:
                    for (int i = 0; i < samplesRead; i++)
                    {
                        data[offset + 2 * i] = buffer[i];
                        data[offset + 2 * i + 1] = buffer[i];
                    }
                    break;

                default:
                    Debug.LogError($"Unexpected AudioChannel: {channels}");
                    goto case AudioChannel.Both;
            }

            offset += 2 * samplesRead;
            samplesRemaining -= 2 * samplesRead;
        }

        return count - samplesRemaining;
    }

    private IEnumerable<double>? _channelRMS = null;
    public override IEnumerable<double> GetChannelRMS()
    {
        if (_channelRMS is null)
        {
            double[] rms = Enumerable.Repeat(stream.GetChannelRMS().First(), Channels).ToArray();

            switch (channels)
            {
                case AudioChannel.Left:
                    rms[1] = 0;
                    break;

                case AudioChannel.Right:
                    rms[0] = 0;
                    break;

                case AudioChannel.Both:
                    //nothing
                    break;

                default:
                    Debug.LogError($"Unexpected AudioChannel: {channels}");
                    goto case AudioChannel.Both;
            }

            _channelRMS = rms;
        }

        return _channelRMS;
    }
}
