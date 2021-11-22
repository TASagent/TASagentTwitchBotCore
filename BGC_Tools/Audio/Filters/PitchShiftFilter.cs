/****************************************************************************
*
* NAME: smbPitchShift.cpp
* VERSION: 1.2
* HOME URL: http://blogs.zynaptiq.com/bernsee
* KNOWN BUGS: none
*
* SYNOPSIS: Routine for doing pitch shifting while maintaining
* duration using the Short Time Fourier Transform.
*
* DESCRIPTION: The routine takes a pitchShift factor value which is between 0.5
* (one octave down) and 2. (one octave up). A value of exactly 1 does not change
* the pitch. numSampsToProcess tells the routine how many samples in indata[0...
* numSampsToProcess-1] should be pitch shifted and moved to outdata[0 ...
* numSampsToProcess-1]. The two buffers can be identical (ie. it can process the
* data in-place). fftFrameSize defines the FFT frame size used for the
* processing. Typical values are 1024, 2048 and 4096. It may be any value <=
* MAX_FRAME_LENGTH but it MUST be a power of 2. osamp is the STFT
* oversampling factor which also determines the overlap between adjacent STFT
* frames. It should at least be 4 for moderate scaling ratios. A value of 32 is
* recommended for best quality. sampleRate takes the sample rate for the signal 
* in unit Hz, ie. 44100 for 44.1 kHz audio. The data passed to the routine in 
* indata[] should be in the range [-1.0, 1.0), which is also the output range 
* for the data, make sure you scale the data accordingly (for 16bit signed integers
* you would have to divide (and multiply) by 32768). 
*
* COPYRIGHT 1999-2015 Stephan M. Bernsee <s.bernsee [AT] zynaptiq [DOT] com>
*
* 						The Wide Open License (WOL)
*
* Permission to use, copy, modify, distribute and sell this software and its
* documentation for any purpose is hereby granted without fee, provided that
* the above copyright notice and this license appear in all source copies. 
* THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT EXPRESS OR IMPLIED WARRANTY OF
* ANY KIND. See http://www.dspguru.com/wol.htm for more information.
*
*****************************************************************************/

//This was adapted to C# from the above
//Available at http://blogs.zynaptiq.com/bernsee/pitch-shifting-using-the-ft/

namespace BGC.Audio.Filters;

public class PitchShiftFilter : SimpleBGCFilter
{
    /// <summary>
    /// Pitch Factor (0.5 = octave down, 1.0 = normal, 2.0 = octave up)
    /// </summary>
    public double PitchFactor { get; set; }

    public override int Channels => stream.Channels;
    public override int TotalSamples => stream.TotalSamples;
    public override int ChannelSamples => stream.ChannelSamples;

    //Shifter objects
    private readonly int fftSize;
    private readonly int overlapRatio;
    private readonly int stepSize;
    private readonly int overlapSize;

    private int outputIndex = 0;
    private int outputSampleCount = 0;

    private readonly float[] inputBuffer;
    private readonly float[] fftBuffer;
    private readonly double[] window;
    private readonly float[] processedSampleBuffer;
    private readonly float[] outputAccumulator;
    private readonly float[] phaseLast;
    private readonly float[] phaseSum;
    private readonly float[] analysisFrequency;
    private readonly float[] analysisMagnitude;
    private readonly float[] synthesisFrequency;
    private readonly float[] synthesisMagnitude;

    public PitchShiftFilter(
        IBGCStream stream,
        double pitchFactor,
        int fftSize = 4096,
        int overlapRatio = 4)
        : base(stream)
    {
        if (stream.Channels != 1)
        {
            throw new StreamCompositionException($"PitchShifterFilter only supports one channel.");
        }

        this.fftSize = fftSize;
        this.overlapRatio = overlapRatio;
        PitchFactor = pitchFactor;
        stepSize = fftSize / overlapRatio;
        overlapSize = fftSize - stepSize;

        inputBuffer = new float[fftSize];
        fftBuffer = new float[2 * fftSize];
        processedSampleBuffer = new float[stepSize];
        outputAccumulator = new float[fftSize];
        phaseLast = new float[fftSize / 2 + 1];
        phaseSum = new float[fftSize / 2 + 1];

        analysisFrequency = new float[fftSize / 2 + 1];
        analysisMagnitude = new float[fftSize / 2 + 1];
        synthesisFrequency = new float[fftSize / 2 + 1];
        synthesisMagnitude = new float[fftSize / 2 + 1];

        window = new double[fftSize];
        double twoPiOverFFTSize = 2.0 * Math.PI / fftSize;
        for (int i = 0; i < fftSize; i++)
        {
            window[i] = 0.5 - 0.5 * Math.Cos(twoPiOverFFTSize * i);
        }
    }

    /// <summary>
    /// Read from this sample provider
    /// </summary>
    public override int Read(float[] data, int offset, int count)
    {
        if (PitchFactor == 1f)
        {
            //Nothing to do
            return stream.Read(data, offset, count);
        }

        int samplesWritten = ReadBody(data, offset, count);

        while (samplesWritten < count)
        {
            int read = stream.Read(inputBuffer, overlapSize, stepSize);

            if (read <= 0)
            {
                //Done
                break;
            }
            else if (read < stepSize)
            {
                //Set rest to zero
                Array.Clear(inputBuffer, overlapSize + read, inputBuffer.Length - overlapSize - read);
            }

            outputIndex = 0;
            outputSampleCount = read;

            PitchShiftFrame();

            //Shift input samples
            Array.Copy(
                sourceArray: inputBuffer,
                sourceIndex: stepSize,
                destinationArray: inputBuffer,
                destinationIndex: 0,
                length: overlapSize);

            samplesWritten += ReadBody(data, offset + samplesWritten, count - samplesWritten);
        }

        return samplesWritten;
    }

    public override void Seek(int position)
    {
        base.Seek(position);
        outputIndex = 0;
        outputSampleCount = 0;
        Array.Clear(inputBuffer, 0, inputBuffer.Length);
        Array.Clear(outputAccumulator, 0, outputAccumulator.Length);
        Array.Clear(processedSampleBuffer, 0, processedSampleBuffer.Length);
        Array.Clear(phaseLast, 0, phaseLast.Length);
        Array.Clear(phaseSum, 0, phaseSum.Length);
    }

    public override void Reset()
    {
        base.Reset();
        outputIndex = 0;
        outputSampleCount = 0;
        Array.Clear(inputBuffer, 0, inputBuffer.Length);
        Array.Clear(outputAccumulator, 0, outputAccumulator.Length);
        Array.Clear(processedSampleBuffer, 0, processedSampleBuffer.Length);
        Array.Clear(phaseLast, 0, phaseLast.Length);
        Array.Clear(phaseSum, 0, phaseSum.Length);
    }

    private int ReadBody(float[] data, int offset, int count)
    {
        int samplesWritten = Math.Min(count, outputSampleCount - outputIndex);

        for (int i = 0; i < samplesWritten; i++)
        {
            data[offset + i] = Limiter(processedSampleBuffer[outputIndex + i]);
        }

        outputIndex += samplesWritten;

        return samplesWritten;
    }

    private static float Limiter(float sample)
    {
        const float LIM_THRESH = 0.95f;
        const float LIM_RANGE = (1f - LIM_THRESH);
        const double TWO_OVER_PI = 2.0 / Math.PI;

        float res;
        if (sample > LIM_THRESH)
        {
            res = (sample - LIM_THRESH) / LIM_RANGE;
            res = (float)(Math.Atan(res) * TWO_OVER_PI * LIM_RANGE + LIM_THRESH);
        }
        else if (sample < -LIM_THRESH)
        {
            res = -(sample + LIM_THRESH) / LIM_RANGE;
            res = -(float)(Math.Atan(res) * TWO_OVER_PI * LIM_RANGE + LIM_THRESH);
        }
        else
        {
            res = sample;
        }

        return res;
    }

    public override IEnumerable<double> GetChannelRMS() => stream.GetChannelRMS();

    private void PitchShiftFrame()
    {
        double magn, phase, temp, real, imag;

        double freqPerBin = (double)SamplingRate / fftSize;

        int qpd;
        int fftFrameSizeHalf = fftSize / 2;

        double expct = 2.0 * Math.PI * stepSize / fftSize;

        //Window and interleave
        for (int k = 0; k < fftSize; k++)
        {
            fftBuffer[2 * k] = (float)(inputBuffer[k] * window[k]);
            fftBuffer[2 * k + 1] = 0f;
        }

        //
        // ANALYSIS
        //

        //FFT
        ShortTimeFourierTransform(fftBuffer, -1);

        //Analysis Step
        for (int k = 0; k <= fftFrameSizeHalf; k++)
        {
            real = fftBuffer[2 * k];
            imag = fftBuffer[2 * k + 1];

            magn = 2.0 * Math.Sqrt(real * real + imag * imag);
            phase = Math.Atan2(imag, real);

            //Phase difference
            temp = phase - phaseLast[k];
            phaseLast[k] = (float)phase;

            //Subtract expected phase difference
            temp -= k * expct;

            //Map delta phase into +/- Pi interval
            qpd = (int)(temp / Math.PI);
            if (qpd >= 0)
            {
                qpd += qpd & 1;
            }
            else
            {
                qpd -= qpd & 1;
            }

            temp -= Math.PI * qpd;

            //Get deviation from bin frequency from the +/- Pi interval
            temp = overlapRatio * temp / (2.0 * Math.PI);

            //Compute the k-th partials' true frequency
            temp = k * freqPerBin + temp * freqPerBin;

            //Store magnitude and true frequency in analysis arrays
            analysisMagnitude[k] = (float)magn;
            analysisFrequency[k] = (float)temp;
        }

        //
        // Processing
        //

        Array.Clear(
            array: synthesisFrequency,
            index: 0,
            length: synthesisFrequency.Length);

        Array.Clear(
            array: synthesisMagnitude,
            index: 0,
            length: synthesisMagnitude.Length);

        int index;
        for (int k = 0; k <= fftFrameSizeHalf; k++)
        {
            index = (int)(k * PitchFactor);
            if (index <= fftFrameSizeHalf)
            {
                synthesisMagnitude[index] += analysisMagnitude[k];
                synthesisFrequency[index] = (float)(analysisFrequency[k] * PitchFactor);
            }
        }

        //
        // Synthesis
        //

        for (int k = 0; k <= fftFrameSizeHalf; k++)
        {
            //Get magnitude and true frequency from synthesis arrays
            magn = synthesisMagnitude[k];
            temp = synthesisFrequency[k];

            //Subtract bin mid frequency
            temp -= k * freqPerBin;

            //Get bin deviation from freq deviation
            temp /= freqPerBin;

            //Take osamp into account
            temp = 2.0 * Math.PI * temp / overlapRatio;

            //Add the overlap phase advance back in
            temp += k * expct;

            //Accumulate delta phase to get bin phase
            phaseSum[k] += (float)temp;
            phase = phaseSum[k];

            //Get real and imag part and re-interleave
            fftBuffer[2 * k] = (float)(magn * Math.Cos(phase));
            fftBuffer[2 * k + 1] = (float)(magn * Math.Sin(phase));
        }

        //Clear negative frequencies
        Array.Clear(
            array: fftBuffer,
            index: fftSize + 2,
            length: fftSize - 2);

        //IFFT
        ShortTimeFourierTransform(fftBuffer, 1);

        //Window and add to output accumulator
        for (int k = 0; k < fftSize; k++)
        {
            outputAccumulator[k] += (float)(2.0 * window[k] * fftBuffer[2 * k] / (fftFrameSizeHalf * overlapRatio));
        }

        //Copy out samples
        Array.Copy(
            sourceArray: outputAccumulator,
            sourceIndex: 0,
            destinationArray: processedSampleBuffer,
            destinationIndex: 0,
            length: stepSize);

        //Shift accumulator samples
        Array.Copy(
            sourceArray: outputAccumulator,
            sourceIndex: stepSize,
            destinationArray: outputAccumulator,
            destinationIndex: 0,
            length: overlapSize);

        Array.Clear(outputAccumulator, overlapSize, stepSize);
    }

    private static void ShortTimeFourierTransform(
        float[] fftBuffer,
        int sign)
    {
        int fftLength = fftBuffer.Length;

        //temporary variables
        float temp, ur, ui, arg, tr, ti, wr, wi;

        //reused loop variables
        int i, j, k, bitm;

        for (i = 2; i < fftLength - 2; i += 2)
        {
            j = 0;
            for (bitm = 2; bitm < fftLength; bitm <<= 1)
            {
                if ((i & bitm) != 0)
                {
                    j++;
                }

                j <<= 1;
            }

            if (i < j)
            {
                temp = fftBuffer[i];
                fftBuffer[i] = fftBuffer[j];
                fftBuffer[j] = temp;

                temp = fftBuffer[i + 1];
                fftBuffer[i + 1] = fftBuffer[j + 1];
                fftBuffer[j + 1] = temp;
            }
        }

        int max = (int)(Math.Log(fftLength) / Math.Log(2.0) - 0.5);

        int le2;
        int le = 2;
        for (k = 0; k < max; k++)
        {
            le <<= 1;
            le2 = le >> 1;
            ur = 1f;
            ui = 0f;
            arg = (float)Math.PI / (le2 >> 1);
            wr = (float)Math.Cos(arg);
            wi = (float)(sign * Math.Sin(arg));

            for (j = 0; j < le2; j += 2)
            {
                for (i = j; i < fftLength; i += le)
                {
                    tr = fftBuffer[i + le2] * ur - fftBuffer[i + le2 + 1] * ui;
                    ti = fftBuffer[i + le2] * ui + fftBuffer[i + le2 + 1] * ur;
                    fftBuffer[i + le2] = fftBuffer[i] - tr;
                    fftBuffer[i + le2 + 1] = fftBuffer[i + 1] - ti;
                    fftBuffer[i] += tr;
                    fftBuffer[i + 1] += ti;
                }

                tr = ur * wr - ui * wi;
                ui = ur * wi + ui * wr;
                ur = tr;
            }
        }
    }
}
