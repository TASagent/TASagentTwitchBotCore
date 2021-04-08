//#define AUDIO_PROFILING

using System;
using NAudio.Wave;
using BGC.Mathematics;


#if AUDIO_PROFILING
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#endif

namespace BGC.Audio
{
    public sealed class BGCStreamToSampleProvider : ISampleProvider
    {
        private readonly IBGCStream internalStream;
        public WaveFormat WaveFormat { get; }

        private float[] internalBuffer = new float[512];

        private int bufferIndex = 0;
        private int bufferCount = 0;

        public BGCStreamToSampleProvider(IBGCStream stream)
        {
            internalStream = stream;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat((int)stream.SamplingRate, stream.Channels);
        }

#if AUDIO_PROFILING
        List<double> durations = new List<double>(100);
        List<int> sampleCounts = new List<int>(100);
#endif

        public int Read(float[] buffer, int offset, int count)
        {
#if AUDIO_PROFILING
            sampleCounts.Add(count);

            Stopwatch sw = new Stopwatch();
            sw.Start();
#endif

            int samplesWritten = ReadBody(buffer, offset, count);

            if (samplesWritten < count)
            {
                if (internalBuffer.Length < count)
                {
                    //Resize buffer
                    internalBuffer = new float[count.CeilingToPowerOfTwo()];
                }

                int read = internalStream.Read(internalBuffer, 0, internalBuffer.Length);

                if (read > 0)
                {
                    bufferIndex = 0;
                    bufferCount = read;

                    samplesWritten += ReadBody(buffer, offset + samplesWritten, count - samplesWritten);
                }
            }

#if AUDIO_PROFILING
            sw.Stop();

            durations.Add(sw.Elapsed.TotalMilliseconds);

            if (durations.Count >= 100)
            {
                Debug.LogWarning($"100 audio passes: {durations.Average()} ms for {sampleCounts.Average()} samples. Max {durations.Max()} ms. Best rate {sampleCounts.Average() / (internalStream.Channels * 0.001 * durations.Average())}");
                //Debug.LogWarning($"100 passes took {durations.Average()} ms to deliver {sampleCounts.Average()} samples for an average max rate of {sampleCounts.Average() / (0.001 * durations.Average())}");
                durations.Clear();
                sampleCounts.Clear();
            }
#endif

            return samplesWritten;
        }

        private int ReadBody(float[] buffer, int offset, int count)
        {
            int samplesWritten = Math.Max(0, Math.Min(count, bufferCount - bufferIndex));

            //It seems that, sometimes, the float array buffer is just a wrapped byte buffer, and Array.Copy and Buffer.Copy fail.
            for (int i = 0; i < samplesWritten; i++)
            {
                buffer[i + offset] = internalBuffer[i + bufferIndex];
            }

            bufferIndex += samplesWritten;

            return samplesWritten;
        }
    }
}
