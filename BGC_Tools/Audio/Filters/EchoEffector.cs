using BGC.Mathematics;

namespace BGC.Audio.Filters;

/// <summary>
/// Applies an echo effect.
/// </summary>
public class EchoEffector : SimpleBGCFilter
{
    public override int Channels => stream.Channels;

    public override int TotalSamples { get; }
    public override int ChannelSamples { get; }

    private int bufferCount = 0;
    private int bufferIndex = 0;
    private int samplesRemaining = -1;
    private int echoBufferIndex = 0;
    private bool samplesDepleted = false;
    private bool echoTail = false;

    private const int READ_BUFFER_SIZE = 512;
    private readonly float[] readBuffer = new float[READ_BUFFER_SIZE];
    private readonly float[] echoBuffer;

    private readonly float residual;
    private readonly float factor;

    private readonly int echoBufferSize;
    private readonly int channelSampleAdjustment;

    private readonly TransformRMSBehavior rmsBehavior;

    public EchoEffector(
        IBGCStream stream,
        double delay = 0.20,
        double residual = 0.30,
        TransformRMSBehavior rmsBehavior = TransformRMSBehavior.Passthrough)
        : base(stream)
    {
        //If we echo until -60dB
        double cutoff = Math.Pow(10.0, -60.0 / 20.0);

        if (residual < 0.01 || residual > 0.5)
        {
            Debug.LogWarning($"Clamped residual value {residual} between 0.01 and 0.5");
            residual = GeneralMath.Clamp(residual, 0.01, 0.5);
        }

        if (delay < 0.01 || delay > 2.0)
        {
            Debug.LogWarning($"Clamped delay value {delay} between 0.01 and 2.0");
            delay = GeneralMath.Clamp(delay, 0.01, 2.0);
        }

        this.residual = (float)residual;
        factor = 1f - this.residual;

        //decay ^ x = cutoff
        //x = log (cutoff) / log(decay)

        int cutoffFrames = (int)Math.Ceiling(Math.Log(cutoff) / Math.Log(residual));
        channelSampleAdjustment = (int)Math.Round(SamplingRate * delay * cutoffFrames);

        echoBufferSize = Channels * (int)Math.Round(SamplingRate * delay);
        echoBuffer = new float[echoBufferSize];

        this.rmsBehavior = rmsBehavior;

        if (stream.ChannelSamples == int.MaxValue)
        {
            ChannelSamples = int.MaxValue;
            TotalSamples = int.MaxValue;
        }
        else
        {
            ChannelSamples = stream.ChannelSamples + channelSampleAdjustment;
            TotalSamples = Channels * ChannelSamples;
        }
    }

    public override void Reset()
    {
        echoBufferIndex = 0;
        bufferIndex = 0;
        bufferCount = 0;
        samplesRemaining = -1;

        samplesDepleted = false;
        echoTail = false;

        stream.Reset();

        Array.Clear(readBuffer, 0, READ_BUFFER_SIZE);
        Array.Clear(echoBuffer, 0, echoBufferSize);
    }

    public override void Seek(int position)
    {
        position = GeneralMath.Clamp(position, 0, ChannelSamples);
        echoBufferIndex = 0;
        bufferIndex = 0;
        bufferCount = 0;
        samplesRemaining = -1;

        samplesDepleted = false;
        echoTail = false;

        stream.Seek(position);

        Array.Clear(readBuffer, 0, READ_BUFFER_SIZE);
        Array.Clear(echoBuffer, 0, echoBufferSize);
    }

    public override int Read(float[] data, int offset, int count)
    {
        int samplesWritten = 0;

        if (!echoTail)
        {
            samplesWritten = ReadBody(data, offset, count);
        }

        while (samplesWritten < count && !samplesDepleted)
        {
            //Read in samples
            bufferCount = stream.Read(readBuffer, 0, READ_BUFFER_SIZE);
            bufferIndex = 0;

            if (bufferCount < READ_BUFFER_SIZE)
            {
                //Read past end
                samplesRemaining = Channels * channelSampleAdjustment;
                samplesDepleted = true;
            }

            samplesWritten += ReadBody(data, offset + samplesWritten, count - samplesWritten);
        }

        if (echoTail)
        {
            samplesWritten += ReadTail(data, offset + samplesWritten, count - samplesWritten);
        }

        return samplesWritten;
    }


    private int ReadBody(float[] buffer, int offset, int count)
    {
        //Write the minimum between the numer of samples available in the buffer and the requested count
        int samplesReady = Math.Min(count, bufferCount - bufferIndex);
        int samplesWritten = 0;

        while (samplesWritten < samplesReady)
        {
            int echoSamplesReady = Math.Min(samplesReady - samplesWritten, echoBufferSize - echoBufferIndex);

            for (int i = 0; i < echoSamplesReady; i++)
            {
                buffer[offset + i] = factor * readBuffer[bufferIndex + i] + echoBuffer[echoBufferIndex + i];
                echoBuffer[echoBufferIndex + i] = residual * buffer[offset + i];
            }

            samplesWritten += echoSamplesReady;
            bufferIndex += echoSamplesReady;
            echoBufferIndex += echoSamplesReady;
            offset += echoSamplesReady;

            if (echoBufferIndex >= echoBufferSize)
            {
                echoBufferIndex = 0;
            }
        }

        if (samplesDepleted && samplesWritten < count)
        {
            echoTail = true;
        }

        return samplesReady;
    }

    private int ReadTail(float[] buffer, int offset, int count)
    {
        //Write the minimum between the numer of samples available and the requested count
        int samplesReady = Math.Min(count, samplesRemaining);
        samplesRemaining -= samplesReady;

        int samplesWritten = 0;

        while (samplesWritten < samplesReady)
        {
            int echoSamplesReady = Math.Min(samplesReady - samplesWritten, echoBufferSize - echoBufferIndex);

            for (int i = 0; i < echoSamplesReady; i++)
            {
                buffer[offset + i] = echoBuffer[echoBufferIndex + i];
                echoBuffer[echoBufferIndex + i] *= residual;
            }

            samplesWritten += echoSamplesReady;
            echoBufferIndex += echoSamplesReady;
            offset += echoSamplesReady;

            if (echoBufferIndex >= echoBufferSize)
            {
                echoBufferIndex = 0;
            }
        }

        return samplesReady;
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

                    if (_channelRMS.Any(double.IsNaN) && ChannelSamples != int.MaxValue)
                    {
                        goto case TransformRMSBehavior.Recalculate;
                    }
                    break;

                default:
                    throw new Exception($"Unexpected rmsBehavior: {rmsBehavior}");
            }
        }

        return _channelRMS;
    }
}
