using System;
using NAudio.Wave;

namespace BGC.Audio.NAudio
{
    public static class NAudioStreamExtensions
    {
        public static DisposableWaveProvider ToDisposableProvider(this IWaveProvider waveProvider) =>
            new DisposableWaveProvider(waveProvider);

        public static IBGCStream ToBGCStream(
            this IWaveProvider waveProvider,
            int channelSamples = int.MaxValue) =>
            new WaveProviderToBGCStream(waveProvider, channelSamples);

        public static IBGCStream ToBGCStream(
            this ISampleProvider sampleProvider,
            int channelSamples = int.MaxValue) =>
            new SampleProviderToBGCStream(sampleProvider, channelSamples);

        public static ISampleProvider ToSampleProvider(this IBGCStream stream) => new BGCStreamToSampleProvider(stream);

        public static ISampleProvider ToBufferedSampleProvider(this IBGCStream stream, int bufferSize = 1024) =>
            new BufferedBGCStreamToSampleProvider(stream, bufferSize);
    }
}
