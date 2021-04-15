using BGC.Audio;
using BGC.Audio.NAudio;
using NAudio.Wave;
using System.IO;

namespace TASagentTwitchBot.Core.Audio
{
    public static class AudioTools
    {
        public static DisposableWaveProvider GetWaveProvider(string filePath)
        {
            switch (Path.GetExtension(filePath).ToLowerInvariant())
            {
                case ".mp4":
                case ".avi":
                case ".wma":
                case ".aac":
                case ".m4a":
                    return new MediaFoundationReader(filePath).ToDisposableProvider();

                case ".mp3":
                    return new Mp3FileReaderBase(
                        filePath,
                        new Mp3FileReaderBase.FrameDecompressorBuilder(x => new AcmMp3FrameDecompressor(x))).ToDisposableProvider();

                case ".wav":
                case ".wave":
                    return new WaveFileReader(filePath).ToDisposableProvider();

                default:
                    return null;
            }
        }
    }
}
