using NAudio.Wave;
using BGC.Audio.NAudio;

namespace TASagentTwitchBot.Core.Audio;

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

            case ".ogg":
                return new NAudio.Vorbis.VorbisWaveReader(filePath).ToDisposableProvider();

            default:
                throw new NotSupportedException($"Filetype not supported: {filePath}");
        }
    }


    public static AudioRequest? JoinRequests(int delayMS, params AudioRequest?[] audioRequests)
    {
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        List<AudioRequest> audioRequestList = new List<AudioRequest>(audioRequests?.Where(x => x is not null) ?? Array.Empty<AudioRequest?>());
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

        if (audioRequestList.Count == 0)
        {
            return null;
        }

        if (audioRequestList.Count == 1)
        {
            return audioRequestList[0];
        }

        if (delayMS > 0)
        {
            for (int i = audioRequestList.Count - 1; i > 0; i--)
            {
                audioRequestList.Insert(i, new AudioDelay(delayMS));
            }
        }

        return new ConcatenatedAudioRequest(audioRequestList);
    }

    public static AudioRequest? JoinRequests(int delayMS, IEnumerable<AudioRequest?> audioRequests)
    {
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        List<AudioRequest> audioRequestList = new List<AudioRequest>(audioRequests?.Where(x => x is not null) ?? Array.Empty<AudioRequest?>());
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

        if (audioRequestList.Count == 0)
        {
            return null;
        }

        if (audioRequestList.Count == 1)
        {
            return audioRequestList[0];
        }

        if (delayMS > 0)
        {
            for (int i = audioRequestList.Count - 1; i > 0; i--)
            {
                audioRequestList.Insert(i, new AudioDelay(delayMS));
            }
        }

        return new ConcatenatedAudioRequest(audioRequestList);
    }
}
