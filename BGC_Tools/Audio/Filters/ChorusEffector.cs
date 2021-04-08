using System;
using System.Collections.Generic;
using System.Linq;
using BGC.Mathematics;
using BGC.Audio.Envelopes;

namespace BGC.Audio.Filters
{
    /// <summary>
    /// Applies a chorus effect.
    /// </summary>
    public class ChorusEffector : SimpleBGCFilter
    {
        public override int Channels => stream.Channels;

        public override int TotalSamples { get; }
        public override int ChannelSamples { get; }

        private int bufferCount = 0;
        private int bufferIndex = 0;
        private int adjIndexA = 0;
        private int adjIndexB = 0;
        private int samplesRemaining = -1;

        private const int MIN_BUFFER_SIZE = 512;
        private float[] newBuffer;
        private float[] oldBuffer;
        private readonly int[] adjustments;

        private readonly int bufferSize;
        private readonly int channelSampleAdjustment;

        private readonly TransformRMSBehavior rmsBehavior;

        public enum DelayType
        {
            Sine = 0,
            Triangle,
            MAX
        }

        public ChorusEffector(
            IBGCStream stream,
            double minDelay = 0.040,
            double maxDelay = 0.060,
            double rate = 0.25,
            DelayType delayType = DelayType.Sine,
            TransformRMSBehavior rmsBehavior = TransformRMSBehavior.Passthrough)
            : base(stream)
        {
            int adjustmentSamples = (int)Math.Round(SamplingRate / rate);

            adjustments = new int[Channels * adjustmentSamples];

            double centerDelay = 0.5 * (maxDelay + minDelay);
            double delayAmplitude = 0.5 * (maxDelay - minDelay);

            switch (delayType)
            {
                case DelayType.Sine:
                    for (int samp = 0; samp < adjustmentSamples; samp++)
                    {
                        for (int chan = 0; chan < Channels; chan++)
                        {
                            adjustments[samp * Channels + chan] = (int)Math.Round(stream.SamplingRate * (centerDelay + delayAmplitude * Math.Sin(2.0 * Math.PI * samp / adjustmentSamples)));
                        }
                    }
                    break;

                case DelayType.Triangle:
                    {
                        int peakSample = adjustmentSamples / 4;
                        int valleySample = 3 * peakSample;

                        for (int samp = 0; samp < adjustmentSamples; samp++)
                        {
                            double triangleValue;
                            if (samp < peakSample)
                            {
                                //Initial Rise
                                triangleValue = GeneralMath.Lerp(0, 1.0, samp / (double)peakSample);

                            }
                            else if (samp < valleySample)
                            {
                                triangleValue = GeneralMath.Lerp(1.0, -1.0, (samp - peakSample) / (double)(valleySample - peakSample));
                            }
                            else
                            {
                                triangleValue = GeneralMath.Lerp(-1.0, 0.0, (samp - valleySample) / (double)(adjustmentSamples - valleySample));
                            }


                            for (int chan = 0; chan < Channels; chan++)
                            {
                                adjustments[samp * Channels + chan] = (int)Math.Round(stream.SamplingRate * (centerDelay + delayAmplitude * triangleValue));
                            }
                        }
                    }
                    break;

                default:
                    throw new StreamCompositionException($"Unexpected DelayType: {delayType}");
            }


            adjIndexA = 0;
            adjIndexB = adjustments.Length / 2;

            int maxAdjustment = adjustments.Max();

            bufferSize = Math.Max(MIN_BUFFER_SIZE, Channels * maxAdjustment.CeilingToPowerOfTwo());

            newBuffer = new float[bufferSize];
            oldBuffer = new float[bufferSize];

            this.rmsBehavior = rmsBehavior;

            channelSampleAdjustment = (int)Math.Ceiling(maxDelay * stream.SamplingRate);

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
            adjIndexA = 0;
            adjIndexB = adjustments.Length / 2;
            bufferIndex = 0;
            bufferCount = 0;
            samplesRemaining = -1;

            stream.Reset();

            Array.Clear(oldBuffer, 0, bufferSize);
            Array.Clear(newBuffer, 0, bufferSize);
        }

        public override void Seek(int position)
        {
            position = GeneralMath.Clamp(position, 0, ChannelSamples);
            adjIndexA = 0;
            adjIndexB = adjustments.Length / 2;
            bufferIndex = 0;
            bufferCount = 0;
            samplesRemaining = -1;

            stream.Seek(position);

            Array.Clear(oldBuffer, 0, bufferSize);
            Array.Clear(newBuffer, 0, bufferSize);
        }

        public override int Read(float[] data, int offset, int count)
        {
            int samplesWritten = ReadBody(data, offset, count);

            while (samplesWritten < count && samplesRemaining == -1)
            {
                //Read in samples
                //Swap buffers
                (oldBuffer, newBuffer) = (newBuffer, oldBuffer);

                int read = stream.Read(newBuffer, 0, bufferSize);

                if (read < bufferSize)
                {
                    //Set rest to zero
                    Array.Clear(newBuffer, read, bufferSize - read);
                    samplesRemaining = read + Channels * channelSampleAdjustment;
                }

                bufferIndex = 0;
                bufferCount = bufferSize;

                samplesWritten += ReadBody(data, offset + samplesWritten, count - samplesWritten);
            }

            return samplesWritten;
        }

        private int ReadBody(float[] buffer, int offset, int count)
        {
            const float amplitude = 0.5f;
            //Write the minimum between the numer of samples available in the buffer and the requested count
            int samplesWritten = Math.Min(count, bufferCount - bufferIndex);

            //Enforce and update samplesRemaining if it's set
            if (samplesRemaining != -1)
            {
                samplesWritten = Math.Min(samplesWritten, samplesRemaining);
                samplesRemaining -= samplesWritten;
            }

            for (int i = 0; i < samplesWritten; i++)
            {
                buffer[offset + i] = amplitude * newBuffer[bufferIndex + i];

                int newIndexA = bufferIndex + i - adjustments[adjIndexA];
                int newIndexB = bufferIndex + i - adjustments[adjIndexB];

                if (newIndexA < 0)
                {
                    buffer[offset + i] += amplitude * oldBuffer[bufferSize + newIndexA];
                }
                else
                {
                    buffer[offset + i] += amplitude * newBuffer[newIndexA];
                }

                if (newIndexB < 0)
                {
                    buffer[offset + i] += amplitude * oldBuffer[bufferSize + newIndexB];
                }
                else
                {
                    buffer[offset + i] += amplitude * newBuffer[newIndexB];
                }

                adjIndexA++;
                adjIndexB++;

                if (adjIndexA >= adjustments.Length)
                {
                    adjIndexA = 0;
                }

                if (adjIndexB >= adjustments.Length)
                {
                    adjIndexB = 0;
                }
            }

            bufferIndex += samplesWritten;

            return samplesWritten;
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
}
