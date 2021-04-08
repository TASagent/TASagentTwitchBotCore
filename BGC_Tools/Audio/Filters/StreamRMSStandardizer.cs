using System;
using System.Collections.Generic;
using System.Linq;

namespace BGC.Audio.Filters
{
    /// <summary>
    /// Scales the underlying stream to a fixed RMS
    /// </summary>
    public class StreamRMSStandardizer : SimpleBGCFilter
    {
        public override int Channels => stream.Channels;
        public override int TotalSamples => stream.TotalSamples;
        public override int ChannelSamples => stream.ChannelSamples;

        private readonly double rms;
        private float scalar = 1f;
        private IEnumerable<double> _channelRMS = null;

        public StreamRMSStandardizer(IBGCStream stream, double rms = (1.0 / 128.0))
            : base(stream)
        {
            this.rms = rms;
        }

        protected override void _Initialize()
        {
            IEnumerable<double> rmsValues = stream.GetChannelRMS();

            if (rmsValues.Any(double.IsNaN))
            {
                if (stream.ChannelSamples == int.MaxValue)
                {
                    throw new StreamCompositionException("Unable to scale inifinte stream with unknowable RMS.");
                }

                rmsValues = stream.CalculateRMS();
            }

            double maxRMS = rmsValues.Max();

            scalar = (float)(rms / maxRMS);

            //Protect against some NaN Poisoning
            if (float.IsNaN(scalar) || float.IsInfinity(scalar))
            {
                scalar = 1f;
            }

            _channelRMS = rmsValues.Select(x => x * scalar).ToList();
        }

        public override int Read(float[] data, int offset, int count)
        {
            if (!initialized)
            {
                Initialize();
            }

            int readSamples = stream.Read(data, offset, count);

            for (int i = 0; i < readSamples; i++)
            {
                data[i + offset] *= scalar;
            }

            return readSamples;
        }

        public override IEnumerable<double> GetChannelRMS()
        {
            if (!initialized)
            {
                Initialize();
            }

            return _channelRMS;
        }
    }
}
