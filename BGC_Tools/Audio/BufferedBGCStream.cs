namespace BGC.Audio;

public sealed class BufferedBGCStream : Filters.SimpleBGCFilter
{
    public override int Channels => stream.Channels;
    public override int TotalSamples => stream.TotalSamples;
    public override int ChannelSamples => stream.ChannelSamples;

    private readonly BufferedData[] buffers;

    private int bufferIndex = 0;
    private Task? fillBufferTask;

    public BufferedBGCStream(
        IBGCStream stream,
        int bufferSize = 1024)
        : base(stream)
    {
        buffers = new BufferedData[]
        {
                new BufferedData(bufferSize),
                new BufferedData(bufferSize)
        };

        if (stream.Channels != 1 && stream.Channels != 2)
        {
            throw new StreamCompositionException($"Completely unexpected stream channel count: {stream.Channels}");
        }

        WarmUpBuffers();
    }


    public override int Read(float[] buffer, int offset, int count)
    {
        BufferedData srcBuffer = buffers[bufferIndex];
        if (!srcBuffer.IsValid)
        {
            return 0;
        }

        int samplesRead = srcBuffer.CopySamples(buffer, offset, count);

        while (samplesRead < count)
        {
            if (srcBuffer.EndOfStream)
            {
                return samplesRead;
            }

            // A buffer swap will be required
            WaitForBufferTask();

            bufferIndex = (bufferIndex + 1) % buffers.Length;
            srcBuffer = buffers[bufferIndex];

            if (!srcBuffer.EndOfStream)
            {
                StartFillBufferTask();
            }

            samplesRead += srcBuffer.CopySamples(buffer, offset + samplesRead, count - samplesRead);
        }

        return samplesRead;
    }

    private void WaitForBufferTask()
    {
        try
        {
            fillBufferTask?.Wait();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in stream reading task: {e}");
        }

        fillBufferTask = null;
    }

    private void WarmUpBuffers()
    {
        bufferIndex = 0;
        FillBuffer(buffers[0]);
        if (!buffers[0].EndOfStream)
        {
            StartFillBufferTask();
        }
    }

    private void StartFillBufferTask()
    {
        int fillBufferIndex = (bufferIndex + 1) % buffers.Length;
        fillBufferTask = Task.Run(() => FillBuffer(buffers[fillBufferIndex]));
    }

    private void FillBuffer(BufferedData buffer)
    {
        buffer.Size = stream.Read(buffer.Samples, 0, buffer.Samples.Length);
        buffer.Offset = 0;
    }

    public override IEnumerable<double> GetChannelRMS() => stream.GetChannelRMS();

    private class BufferedData
    {
        public float[] Samples { get; }
        public int Offset { get; set; } = 0;
        public int Size { get; set; } = 0;
        public bool EndOfStream => Size != Samples.Length;
        public int SamplesRemaining => Size - Offset;
        public bool IsValid => Size > 0;

        public BufferedData(int bufferSize)
        {
            Samples = new float[bufferSize]; // 250ms with 2 channels at 44100hz
        }

        public int CopySamples(float[] buffer, int offset, int count)
        {
            int samplesWritten = Math.Min(count, SamplesRemaining);

            //It seems that, sometimes, the float array buffer is just a wrapped byte buffer, and Array.Copy and Buffer.Copy fail.
            for (int i = 0; i < samplesWritten; i++)
            {
                buffer[i + offset] = Samples[i + Offset];
            }

            Offset += samplesWritten;

            return samplesWritten;
        }
    }
}
