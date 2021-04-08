using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BGC.Collections.Generic;
using BGC.Mathematics;

namespace BGC.Audio.Filters
{
    public class MultiChannelNoiseGateFilter : SimpleBGCFilter
    {
        public override int Channels => stream.Channels;

        public override int TotalSamples { get; }
        public override int ChannelSamples { get; }

        private readonly NonSilenceWindow nonSilenceWindow;
        private readonly SmoothingWindow smoothingWindow;
        private readonly RingBuffer<float> sampleRingBuffer;

        private readonly int latencySamples;

        private const int BUFFER_SIZE_PER_CHANNEL = 512;

        private readonly int bufferSize;
        private readonly float[] sampleBuffer;

        private readonly TransformRMSBehavior rmsBehavior;
        private readonly double minNonSilentDuration;

        private int bufferIndex = 0;
        private int bufferCount = 0;
        private int samplesRemaining = -1;

        public MultiChannelNoiseGateFilter(
            IBGCStream stream,
            double threshold = -50.0,
            double windowDuration = 0.1,
            double minNonSilentDuration = 0.07,
            double attackDuration = 0.05,
            TransformRMSBehavior rmsBehavior = TransformRMSBehavior.Passthrough)
            : base(stream)
        {
            this.minNonSilentDuration = minNonSilentDuration;

            threshold = Math.Pow(10.0, threshold / 20.0);
            int halfWindowSamples = (int)Math.Floor(windowDuration * SamplingRate * 0.5f);
            int windowSamples = 2 * halfWindowSamples + 1;
            int smoothingWindowSize = (int)Math.Floor(attackDuration * SamplingRate);
            latencySamples = halfWindowSamples + smoothingWindowSize;

            nonSilenceWindow = new NonSilenceWindow(
                nonSilentSize: windowSamples,
                maxWindowSize: (int)Math.Round(SamplingRate * 0.005),
                samplingRate: SamplingRate,
                channels: Channels,
                levelThreshold: (float)threshold);

            smoothingWindow = new SmoothingWindow(
                windowSize: smoothingWindowSize,
                channels: Channels);

            sampleRingBuffer = new RingBuffer<float>(Channels * latencySamples);

            bufferSize = BUFFER_SIZE_PER_CHANNEL * Channels;
            sampleBuffer = new float[bufferSize];

            this.rmsBehavior = rmsBehavior;

            if (stream.ChannelSamples == int.MaxValue)
            {
                ChannelSamples = int.MaxValue;
                TotalSamples = int.MaxValue;
            }
            else
            {
                ChannelSamples = stream.ChannelSamples + latencySamples;
                TotalSamples = Channels * ChannelSamples;
            }
        }

        public override int Read(float[] data, int offset, int count)
        {
            int samplesWritten = ReadBody(data, offset, count);

            while (samplesWritten < count && samplesRemaining == -1)
            {
                //Read in samples
                int read = stream.Read(sampleBuffer, 0, bufferSize);

                if (read < bufferSize)
                {
                    //Set rest to zero
                    Array.Clear(sampleBuffer, read, bufferSize - read);
                    samplesRemaining = read + latencySamples;
                }

                bufferIndex = 0;
                bufferCount = bufferSize;

                samplesWritten += ReadBody(data, offset + samplesWritten, count - samplesWritten);
            }

            return samplesWritten;
        }

        private int ReadBody(float[] data, int offset, int count)
        {
            int samplesWritten = Math.Min(count, bufferCount - bufferIndex);

            //Enforce and update samplesRemaining if it's set
            if (samplesRemaining != -1)
            {
                samplesWritten = Math.Min(samplesWritten, samplesRemaining);
                samplesRemaining -= samplesWritten;
            }

            for (int i = 0; i < samplesWritten; i++)
            {
                nonSilenceWindow.PushSample(sampleBuffer[bufferIndex + i]);
                smoothingWindow.PushSample(nonSilenceWindow.GetNonSilence() >= minNonSilentDuration);
                if (sampleRingBuffer.IsFull)
                {
                    data[offset + i] = sampleRingBuffer.Tail * (float)smoothingWindow.GetScalingFactor();
                }
                else
                {
                    data[offset + i] = 0f;
                }

                sampleRingBuffer.Push(sampleBuffer[bufferIndex + i]);
            }

            bufferIndex += samplesWritten;
            return samplesWritten;
        }

        public override void Reset()
        {
            bufferIndex = 0;
            bufferCount = 0;
            samplesRemaining = -1;

            stream.Reset();

            Array.Clear(sampleBuffer, 0, bufferSize);
        }

        public override void Seek(int position)
        {
            bufferIndex = 0;
            bufferCount = 0;
            samplesRemaining = -1;

            stream.Seek(position);

            Array.Clear(sampleBuffer, 0, bufferSize);
        }

        private IEnumerable<double> _channelRMS = null;
        public override IEnumerable<double> GetChannelRMS()
        {
            if (_channelRMS == null)
            {
                switch (rmsBehavior)
                {
                    case TransformRMSBehavior.Recalculate:
                        _channelRMS = this.CalculateRMS();
                        break;

                    case TransformRMSBehavior.Passthrough:
                        _channelRMS = stream.GetChannelRMS();
                        break;

                    default:
                        throw new Exception($"Unexpected rmsBehavior: {rmsBehavior}");
                }
            }

            return _channelRMS;
        }


        /// <summary>
        /// A sliding window that maintains its maximum absolute value
        /// </summary>
        class MaxWindow
        {
            private readonly int windowSize;
            private readonly RingBuffer<float> buffer;
            private readonly RingBuffer<int> indices;

            private int sampleCount;

            public MaxWindow(int windowSize)
            {
                this.windowSize = windowSize;
                buffer = new RingBuffer<float>(windowSize);
                indices = new RingBuffer<int>(windowSize);
            }

            public void PushSample(float sample)
            {
                sample = Math.Abs(sample);

                while (indices.Count > 0 && GetSample(indices.Head) <= sample)
                {
                    indices.Pop();
                }
                while (indices.Count > 0 && indices.Tail <= sampleCount - windowSize)
                {
                    indices.PopBack();
                }
                indices.Push(sampleCount++);
                buffer.Push(sample);
            }

            private float GetSample(int index)
            {
                return buffer[sampleCount - index - 1];
            }

            public float GetLevel() => GetSample(indices.Tail);
        }


        /// <summary>
        /// A sliding window that knows at each moment how much non-silence it contains.
        /// </summary>
        class NonSilenceWindow
        {
            private readonly RingBuffer<bool> buffer;
            private readonly MaxWindow maxWindow;
            private readonly double samplingRate;
            private readonly int channels;
            // a threshold above which the sound is considered non-silent
            private readonly float levelThreshold;

            private int nonSilentSamples = 0;

            public NonSilenceWindow(
                int nonSilentSize,
                int maxWindowSize,
                double samplingRate,
                int channels,
                float levelThreshold)
            {
                buffer = new RingBuffer<bool>(channels * nonSilentSize);
                maxWindow = new MaxWindow(channels * maxWindowSize);
                this.samplingRate = samplingRate;
                this.channels = channels;
                this.levelThreshold = levelThreshold;
            }

            public void PushSample(float sample)
            {
                maxWindow.PushSample(sample);
                if (buffer.IsFull && buffer.PopBack())
                {
                    nonSilentSamples--;
                }

                bool nextNonSilent = maxWindow.GetLevel() >= levelThreshold;
                buffer.Push(nextNonSilent);
                if (nextNonSilent)
                {
                    nonSilentSamples++;
                }
            }

            // Get the total amount of non-silence inside the window in seconds
            public double GetNonSilence()
            {
                return nonSilentSamples / (samplingRate * channels);
            }
        }


        /// <summary>
        /// A window that smoothes the transition between the open and closed states of the gate.
        /// The state of the gate is represented by a bool: true = open, false = closed.
        /// When the gate moves from open to closed(true -> false), the gate closes smoothly after that.
        /// When the gate moves from closed to open (false -> true), this event is anticipated ahead of time and the transition is again smoothed.
        /// This function could probably be optimized by introducing more states and avoiding multiplications when the gate remains
        /// open or closed for a long time.
        /// </summary>
        class SmoothingWindow
        {
            private const float FLOOR = 1e-4f; // -80 dB

            private readonly float factor;
            private readonly int windowSize;

            // The current scaling factor applied to the sound samples.
            private float currentCoeff = 1;

            // Are we currently rising (true) or falling (false)?
            private bool gateRising = true;

            // The number of samples since we've last seen the gate open.
            // If it's more than the window size, we may begin to decrease the scaling factor.
            private int samplesSinceOpen = 0;

            public SmoothingWindow(
                int windowSize,
                int channels)
            {
                this.windowSize = windowSize * channels;
                factor = (float)Math.Exp(-Math.Log(FLOOR) / (windowSize * channels));
            }

            // Push a new sample (is the gate open?)
            public void PushSample(bool open)
            {
                if (open)
                {
                    samplesSinceOpen = 0;
                    gateRising = true;
                }
                else
                {
                    samplesSinceOpen++;
                    if (samplesSinceOpen > windowSize)
                    {
                        gateRising = false;
                    }
                }

                if (gateRising)
                {
                    currentCoeff = Math.Min(Math.Max(currentCoeff, FLOOR) * factor, 1f);
                }
                else
                {
                    currentCoeff = currentCoeff / factor;
                    if (currentCoeff < FLOOR)
                    {
                        currentCoeff = 0f;
                    }
                }
            }
            // Get the current scaling factor (with the latency equal to the
            // attack/decay duration)
            public double GetScalingFactor() => currentCoeff;
        }
    }
}
