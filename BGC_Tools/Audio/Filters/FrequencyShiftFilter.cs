using System;
using System.Collections.Generic;
using BGC.Mathematics;

using static System.Math;

namespace BGC.Audio.Filters
{
    /// <summary>
    /// Applies FrequencyShift to the underlying stream, in the frequency domain.
    /// Initial Analytic signal generation based on 
    /// "An Efficient Analytic Signal Generator" by Clay S. Turner
    /// http://www.claysturner.com/dsp/asg.pdf
    /// </summary>
    public class FrequencyShiftFilter : SimpleBGCFilter
    {
        private const double A = 0.00125;
        private const double W_1 = 0.49875;
        private const double W_2 = 0.00125;
        private const int FILTER_LENGTH = 129;

        public override int Channels => stream.Channels;
        public override int TotalSamples => stream.TotalSamples;
        public override int ChannelSamples => stream.ChannelSamples;

        private readonly IBGCStream convStream;

        private readonly double frequencyShift;

        private Complex64 partial;
        private readonly double cyclePartial;
        private readonly Complex64[] shifterSamples;

        private int shifterPosition = 0;
        private int cycles = 0;

        private const int BUFFER_SIZE = 512;
        private readonly float[] buffer = new float[BUFFER_SIZE];

        public FrequencyShiftFilter(
            IBGCStream stream,
            double frequencyShift)
            : base(stream)
        {
            if (stream.Channels != 1)
            {
                throw new StreamCompositionException(
                    $"FrequencyShiftFilter requires a mono input stream.  Input stream has {stream.Channels} channels");
            }

            this.frequencyShift = frequencyShift;

            double[] realConvolutionFilter = new double[FILTER_LENGTH];
            double[] imagConvolutionFilter = new double[FILTER_LENGTH];

            const double twoPiSq = 2.0 * PI * PI;
            const double fourASq = 4.0 * A * A;
            const double piSq = PI * PI;
            const double piOv4 = PI / 4.0;
            const double piOv4A = PI / (4.0 * A);
            double N_0 = (FILTER_LENGTH - 1.0) / 2.0;

            for (int i = 1; i < FILTER_LENGTH - 1; i++)
            {
                if (i == N_0)
                {
                    continue;
                }

                double t = 2.0 * PI * (i - N_0);
                double prefactor = twoPiSq * Cos(A * t) / (t * (fourASq * t * t - piSq));

                realConvolutionFilter[i] = prefactor * (Sin(W_1 * t + piOv4) - Sin(W_2 * t + piOv4));
            }

            realConvolutionFilter[0] = A * (Sin(piOv4A * (A - 2.0 * W_1)) - Sin(piOv4A * (A - 2.0 * W_2)));
            realConvolutionFilter[FILTER_LENGTH - 1] = A * (Sin(piOv4A * (A + 2.0 * W_2)) - Sin(piOv4A * (A + 2.0 * W_1)));

            if (FILTER_LENGTH % 2 == 1)
            {
                realConvolutionFilter[(int)N_0] = Sqrt(2.0) * (W_2 - W_1);
            }

            for (int i = 0; i < FILTER_LENGTH; i++)
            {
                imagConvolutionFilter[i] = realConvolutionFilter[FILTER_LENGTH - 1 - i];
            }

            convStream = new MultiConvolutionFilter(stream, realConvolutionFilter, imagConvolutionFilter);

            double sampleCount = Abs(SamplingRate / this.frequencyShift);
            int intSampleCount = (int)Ceiling(sampleCount) - 1;

            cyclePartial = (2 * PI * this.frequencyShift / SamplingRate) * (intSampleCount - sampleCount);

            cycles = 0;
            partial = Complex64.FromPolarCoordinates(
                magnitude: 1.0,
                phase: cycles * cyclePartial);

            shifterSamples = new Complex64[intSampleCount];

            for (int i = 0; i < intSampleCount; i++)
            {
                shifterSamples[i] = Complex64.FromPolarCoordinates(
                    magnitude: 1.0,
                    phase: Sign(this.frequencyShift) * 2 * PI * i / sampleCount);
            }
        }

        public override int Read(float[] data, int offset, int count)
        {
            int samplesRemaining = count;

            while (samplesRemaining > 0)
            {
                int maxReadCount = Min(2 * samplesRemaining, BUFFER_SIZE);

                int sampleReadCount = convStream.Read(buffer, 0, maxReadCount);

                if (sampleReadCount == 0)
                {
                    break;
                }

                sampleReadCount /= 2;

                for (int i = 0; i < sampleReadCount; i++)
                {
                    data[offset + i] = (float)(new Complex64(buffer[2 * i], buffer[2 * i + 1]) * partial).RealProduct(shifterSamples[shifterPosition++]);
                    if (shifterPosition == shifterSamples.Length)
                    {
                        shifterPosition = 0;
                        cycles++;
                        partial = Complex64.FromPolarCoordinates(
                            magnitude: 1.0,
                            phase: cycles * cyclePartial);
                    }
                }

                samplesRemaining -= sampleReadCount;
                offset += sampleReadCount;
            }

            return count - samplesRemaining;
        }

        public override void Reset()
        {
            shifterPosition = 0;
            cycles = 0;
            partial = Complex64.FromPolarCoordinates(
                magnitude: 1.0,
                phase: cycles * cyclePartial);
            convStream.Reset();
        }

        public override void Seek(int position)
        {
            position = GeneralMath.Clamp(position, 0, ChannelSamples);
            convStream.Seek(position);
            cycles = position / shifterSamples.Length;
            partial = Complex64.FromPolarCoordinates(
                magnitude: 1.0,
                phase: cycles * cyclePartial);
            shifterPosition = position % shifterSamples.Length;
        }

        public override IEnumerable<double> GetChannelRMS() => stream.GetChannelRMS();
    }
}
