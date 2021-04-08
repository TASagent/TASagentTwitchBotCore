using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BGC.Mathematics;

namespace BGC.Audio.Filters
{
    /// <summary>
    /// Performs continuous Vocoding on the underlying stream
    /// </summary>
    public class Vocoder : SimpleBGCFilter
    {
        public override int Channels => stream.Channels;
        public override int TotalSamples => stream.TotalSamples;
        public override int ChannelSamples => stream.ChannelSamples;

        private readonly IBGCStream carrierStream;

        private readonly float[] inputBuffer;
        private readonly float[] carrierBuffer;
        private readonly double[] window;

        private readonly Complex64[] carrierFFTBuffer;
        private readonly Complex64[] signalFFTBuffer;

        private readonly Complex64[][] amplitudeBuffers;
        private readonly Complex64[][] carrierBandBuffers;

        private readonly double[] outputAccumulation;
        private readonly float[] cachedSampleBuffer;

        private readonly double[] bandFrequencies;

        private readonly int fftSize;
        private readonly int overlapRatio;
        private readonly int stepSize;
        private readonly int overlapSize;
        private readonly double outputFactor;

        private int bufferIndex = 0;
        private int bufferCount = 0;
        private readonly TransformRMSBehavior rmsBehavior;

        private int frameLag = 0;
        private int samplesHandled = 0;

        public Vocoder(
            IBGCStream stream,
            IBGCStream carrierStream,
            double freqLowerBound = 50.0,
            double freqUpperBound = 16000.0,
            int bandCount = 22,
            int fftSize = 4096,
            int overlapRatio = 4,
            TransformRMSBehavior rmsBehavior = TransformRMSBehavior.Passthrough)
            : base(stream)
        {
            if (stream.Channels != 1)
            {
                throw new StreamCompositionException(
                    $"Vocoder requires a mono input stream. Input stream has {stream.Channels} channels.");
            }

            if (carrierStream.Channels != 1)
            {
                throw new StreamCompositionException(
                    $"Vocoder requires a mono carrier stream. Carrier stream has {carrierStream.Channels} channels.");
            }

            if (stream.SamplingRate != carrierStream.SamplingRate)
            {
                throw new StreamCompositionException(
                    $"Vocoder requires the sampling rate of the stream and carrierStream to match. {stream.SamplingRate} vs {carrierStream.SamplingRate}.");
            }

            this.carrierStream = carrierStream;

            this.fftSize = fftSize;
            this.overlapRatio = overlapRatio;
            stepSize = fftSize / overlapRatio;
            overlapSize = fftSize - stepSize;

            inputBuffer = new float[fftSize];
            carrierBuffer = new float[fftSize];
            outputAccumulation = new double[fftSize];
            cachedSampleBuffer = new float[stepSize];

            carrierFFTBuffer = new Complex64[fftSize];
            signalFFTBuffer = new Complex64[fftSize];

            amplitudeBuffers = new Complex64[bandCount][];
            carrierBandBuffers = new Complex64[bandCount][];

            for (int i = 0; i < bandCount; i++)
            {
                amplitudeBuffers[i] = new Complex64[fftSize];
                carrierBandBuffers[i] = new Complex64[fftSize];
            }

            initialized = false;

            double[] windowTemplate = Windowing.GetHalfWindow64(Windowing.Function.BlackmanHarris, fftSize / 2);

            window = new double[fftSize];
            for (int i = 0; i < fftSize / 2; i++)
            {
                window[i] = windowTemplate[i];
                window[fftSize - i - 1] = windowTemplate[i];
            }

            this.rmsBehavior = rmsBehavior;

            bandFrequencies = GetExponentialDistribution(freqLowerBound, freqUpperBound, bandCount).ToArray();

            outputFactor = 0.5 * Math.Sqrt(fftSize) / overlapRatio;
        }

        protected override void _Initialize()
        {
            frameLag = overlapRatio;
        }

        public override int Read(float[] data, int offset, int count)
        {
            if (!initialized)
            {
                Initialize();
            }

            int samplesWritten = ReadBody(data, offset, count);

            while (samplesWritten < count)
            {
                int readStream = stream.Read(inputBuffer, overlapSize, stepSize);
                int readCarrier = carrierStream.Read(carrierBuffer, overlapSize, stepSize);

                if (readStream <= 0 && samplesHandled <= 0)
                {
                    //Done, No samples left to work with
                    break;
                }
                else if (readStream <= 0)
                {
                    //We are in buffer-dumping window
                    //Set rest of inputBuffer to zero
                    Array.Clear(inputBuffer, overlapSize, stepSize);
                }
                else if (readStream < stepSize)
                {
                    //Near or at the end
                    //Set rest of inputBuffer to zero
                    Array.Clear(inputBuffer, overlapSize + readStream, inputBuffer.Length - overlapSize - readStream);
                }

                if (readCarrier <= 0)
                {
                    //We are in buffer-dumping window
                    //Set rest of inputBuffer to zero
                    Array.Clear(carrierBuffer, overlapSize, stepSize);
                }
                else if (readCarrier < stepSize)
                {
                    //Near or at the end
                    //Set rest of inputBuffer to zero
                    Array.Clear(carrierBuffer, overlapSize + readCarrier, carrierBuffer.Length - overlapSize - readCarrier);
                }

                //Copy in the input data
                for (int i = 0; i < fftSize; i++)
                {
                    signalFFTBuffer[i] = inputBuffer[i] * window[i];
                    carrierFFTBuffer[i] = carrierBuffer[i];
                }

                //FFT
                Task.WaitAll(
                    Task.Run(() => Fourier.Forward(signalFFTBuffer)),
                    Task.Run(() => Fourier.Forward(carrierFFTBuffer)));

                //For each band...

                Parallel.For(
                    fromInclusive: 0,
                    toExclusive: bandFrequencies.Length - 1,
                    body: (int band) =>
                    {
                        int lowerBound = FrequencyDomain.GetComplexFrequencyBin(fftSize, bandFrequencies[band], SamplingRate);
                        int upperBound = FrequencyDomain.GetComplexFrequencyBin(fftSize, bandFrequencies[band + 1], SamplingRate);

                        Complex64[] amplitudeBuffer = amplitudeBuffers[band];
                        Complex64[] carrierBandBuffer = carrierBandBuffers[band];

                        //Copy over band just the relevant frequency band
                        for (int i = lowerBound; i < upperBound; i++)
                        {
                            amplitudeBuffer[i] = 2.0 * signalFFTBuffer[i];
                            carrierBandBuffer[i] = 2.0 * carrierFFTBuffer[i];
                        }

                        Complex64 zero = Complex64.Zero;


                        //Clear rest of buffers
                        for (int i = 0; i < lowerBound; i++)
                        {
                            amplitudeBuffer[i] = zero;
                            carrierBandBuffer[i] = zero;
                        }

                        for (int i = upperBound; i < amplitudeBuffer.Length; i++)
                        {
                            amplitudeBuffer[i] = zero;
                            carrierBandBuffer[i] = zero;
                        }

                        //IFFT
                        Task.WaitAll(
                            Task.Run(() => Fourier.Inverse(amplitudeBuffer)),
                            Task.Run(() => Fourier.Inverse(carrierBandBuffer)));

                        for (int i = 0; i < amplitudeBuffer.Length; i++)
                        {
                            outputAccumulation[i] += outputFactor * window[i] * carrierBandBuffer[i].Real * amplitudeBuffer[i].Magnitude;
                        }
                    });


                samplesHandled += Math.Min(readStream, readCarrier);

                if (--frameLag <= 0)
                {
                    bufferIndex = 0;
                    bufferCount = Math.Min(stepSize, samplesHandled);
                    samplesHandled -= bufferCount;

                    //Copy output samples to output buffer
                    for (int sample = 0; sample < bufferCount; sample++)
                    {
                        cachedSampleBuffer[sample] = (float)outputAccumulation[sample];
                    }
                }

                //Slide over input samples
                Array.Copy(
                    sourceArray: inputBuffer,
                    sourceIndex: stepSize,
                    destinationArray: inputBuffer,
                    destinationIndex: 0,
                    length: overlapSize);

                //Slide over carrier samples
                Array.Copy(
                    sourceArray: carrierBuffer,
                    sourceIndex: stepSize,
                    destinationArray: carrierBuffer,
                    destinationIndex: 0,
                    length: overlapSize);

                //Slide output samples
                Array.Copy(
                    sourceArray: outputAccumulation,
                    sourceIndex: stepSize,
                    destinationArray: outputAccumulation,
                    destinationIndex: 0,
                    length: overlapSize);

                //Clear empty output accumulation region
                Array.Clear(outputAccumulation, overlapSize, stepSize);

                samplesWritten += ReadBody(data, offset + samplesWritten, count - samplesWritten);
            }

            return samplesWritten;
        }

        private int ReadBody(float[] buffer, int offset, int count)
        {
            int samplesWritten = Math.Max(0, Math.Min(count, bufferCount - bufferIndex));

            Array.Copy(
                sourceArray: cachedSampleBuffer,
                sourceIndex: bufferIndex,
                destinationArray: buffer,
                destinationIndex: offset,
                length: samplesWritten);

            bufferIndex += samplesWritten;

            return samplesWritten;
        }

        private void ClearBuffers()
        {
            bufferIndex = 0;
            bufferCount = 0;
            samplesHandled = 0;
            frameLag = overlapRatio;

            Array.Clear(cachedSampleBuffer, 0, cachedSampleBuffer.Length);
            Array.Clear(outputAccumulation, 0, outputAccumulation.Length);
            Array.Clear(inputBuffer, 0, inputBuffer.Length);
            Array.Clear(carrierBuffer, 0, carrierBuffer.Length);
        }

        public override void Reset()
        {
            ClearBuffers();
            stream.Reset();
            carrierStream.Reset();
        }

        public override void Seek(int position)
        {
            ClearBuffers();
            stream.Seek(position);
            carrierStream.Seek(position);
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

        #region Helper Generator

        private static IEnumerable<double> GetExponentialDistribution(
            double freqLowerBound,
            double freqUpperBound,
            int bandCount)
        {
            double freqRatio = Math.Pow((freqUpperBound / freqLowerBound), 1.0 / bandCount);
            if (double.IsNaN(freqRatio) || double.IsInfinity(freqRatio))
            {
                freqRatio = 1.0;
            }

            double freq = freqLowerBound;

            for (int carrierTone = 0; carrierTone < bandCount + 1; carrierTone++)
            {
                yield return freq;

                freq *= freqRatio;
            }
        }

        #endregion Helper Generator
    }
}
