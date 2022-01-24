using NAudio.CoreAudioApi;
using NAudio.Wave;
using BGC.Audio;
using BGC.Audio.NAudio;

namespace TASagentTwitchBot.Core.Audio;

public abstract class AudioRequest
{
    protected bool cancel = false;

    public abstract Task PlayRequest(MMDevice outputDevice);
    public abstract IBGCStream CacheToBGCStream();

    public virtual void RequestCancel()
    {
        cancel = true;
    }

    public static AudioRequest? ParseCommand(string command)
    {
        if (command.StartsWith("!pause(") && command.EndsWith(")"))
        {
            if (!int.TryParse(command[7..^1], out int duration))
            {
                if (double.TryParse(command[7..^1], out double doubleDuration))
                {
                    doubleDuration = Math.Clamp(doubleDuration, 0, 10_000);
                    duration = (int)doubleDuration;
                }
                else
                {
                    BGC.Debug.LogWarning($"Unable to parse Pause command: {command}");
                    duration = 1_000;
                }
            }

            duration = Math.Clamp(duration, 0, 10_000);

            return new AudioDelay(duration);
        }

        return null;
    }
}

public class VideoFileAudioRequest : AudioRequest
{
    private readonly string filePath;

    public VideoFileAudioRequest(string filePath)
    {
        this.filePath = filePath;
    }

    public override IBGCStream CacheToBGCStream()
    {
        using DisposableWaveProvider audioStream = AudioTools.GetWaveProvider(filePath);
        return audioStream.ToBGCStream().SlowRangeFitter().StreamLevelScaler(-10).SafeCache();
    }


    public override async Task PlayRequest(MMDevice outputDevice)
    {
        using WasapiOut outputPlayer = new WasapiOut(outputDevice, AudioClientShareMode.Shared, true, 100);
        using DisposableWaveProvider audioStream = AudioTools.GetWaveProvider(filePath);

        cancel = false;

        outputPlayer.Init(audioStream.ToBGCStream().SlowRangeFitter().StreamLevelScaler(-10).ToSampleProvider());
        outputPlayer.Play();
        while (outputPlayer.PlaybackState == PlaybackState.Playing)
        {
            await Task.Delay(50);

            if (cancel && outputPlayer.PlaybackState == PlaybackState.Playing)
            {
                outputPlayer.Stop();
            }
        }

        await Task.Delay(100);
    }
}

public class AudioFileRequest : AudioRequest
{
    private readonly string filePath;
    private readonly Effects.Effect effectsChain;

    public AudioFileRequest(string filePath, Effects.Effect effectsChain)
    {
        this.filePath = filePath;
        this.effectsChain = effectsChain;
    }

    public override IBGCStream CacheToBGCStream()
    {
        using DisposableWaveProvider audioStream = AudioTools.GetWaveProvider(filePath);
        return effectsChain.ApplyEffects(audioStream.ToBGCStream().EnsureMono()).LimitStream().SafeCache();
    }

    public override async Task PlayRequest(MMDevice outputDevice)
    {
        using WasapiOut outputPlayer = new WasapiOut(outputDevice, AudioClientShareMode.Shared, true, 100);
        using DisposableWaveProvider audioStream = AudioTools.GetWaveProvider(filePath);

        cancel = false;

        outputPlayer.Init(audioStream.ApplyEffects(effectsChain));
        outputPlayer.Play();
        while (outputPlayer.PlaybackState == PlaybackState.Playing)
        {
            await Task.Delay(50);

            if (cancel && outputPlayer.PlaybackState == PlaybackState.Playing)
            {
                outputPlayer.Stop();
            }
        }

        await Task.Delay(100);
    }
}

public class SoundEffectRequest : AudioRequest
{
    private readonly SoundEffect soundEffect;

    public SoundEffectRequest(SoundEffect soundEffect)
    {
        this.soundEffect = soundEffect;
    }

    public override IBGCStream CacheToBGCStream()
    {
        using DisposableWaveProvider audioStream = AudioTools.GetWaveProvider(soundEffect.FilePath);
        return audioStream.ToBGCStream().SlowRangeFitter().StreamLevelScaler(-10).SafeCache();
    }

    public override async Task PlayRequest(MMDevice outputDevice)
    {
        using WasapiOut outputPlayer = new WasapiOut(outputDevice, AudioClientShareMode.Shared, true, 100);
        using DisposableWaveProvider audioStream = AudioTools.GetWaveProvider(soundEffect.FilePath);

        cancel = false;

        outputPlayer.Init(audioStream.ToBGCStream().SlowRangeFitter().StreamLevelScaler(-10).ToSampleProvider());
        outputPlayer.Play();
        while (outputPlayer.PlaybackState == PlaybackState.Playing)
        {
            await Task.Delay(50);

            if (cancel && outputPlayer.PlaybackState == PlaybackState.Playing)
            {
                outputPlayer.Stop();
            }
        }

        await Task.Delay(100);
    }
}

public class AudioDelay : AudioRequest
{
    private readonly int delayMS;

    public AudioDelay(int delayMS)
    {
        this.delayMS = delayMS;
    }

    public override IBGCStream CacheToBGCStream() =>
        new BGC.Audio.Synthesis.SilenceStream(1, (int)Math.Ceiling(44100 * 0.001 * delayMS));

    public override async Task PlayRequest(MMDevice outputDevice)
    {
        await Task.Delay(delayMS);
    }
}


public class BGCStreamRequest : AudioRequest
{
    private readonly IBGCStream stream;

    public BGCStreamRequest(IBGCStream stream)
    {
        this.stream = stream;
    }

    public override IBGCStream CacheToBGCStream() =>
        stream.SafeCache();

    public override async Task PlayRequest(MMDevice outputDevice)
    {
        using WasapiOut outputPlayer = new WasapiOut(outputDevice, AudioClientShareMode.Shared, true, 100);
        cancel = false;

        ISampleProvider sampleProvider = stream.ToSampleProvider();

        outputPlayer.Init(sampleProvider);
        outputPlayer.Play();
        while (outputPlayer.PlaybackState == PlaybackState.Playing)
        {
            await Task.Delay(50);

            if (cancel && outputPlayer.PlaybackState == PlaybackState.Playing)
            {
                outputPlayer.Stop();
            }
        }

        await Task.Delay(100);
    }

}


public class ConcatenatedAudioRequest : AudioRequest
{
    private readonly IEnumerable<AudioRequest> internalRequests;

    private AudioRequest? currentRequest = null;

    public ConcatenatedAudioRequest(IEnumerable<AudioRequest> requests)
    {
        internalRequests = requests;
    }

    public ConcatenatedAudioRequest(params AudioRequest[] requests)
    {
        internalRequests = requests;
    }

    public override IBGCStream CacheToBGCStream() =>
        new BGC.Audio.Filters.StreamConcatenator(internalRequests.Select(x => x.CacheToBGCStream()));

    public override void RequestCancel()
    {
        cancel = true;
        currentRequest?.RequestCancel();
    }

    public override async Task PlayRequest(MMDevice outputDevice)
    {
        cancel = false;

        foreach (AudioRequest request in internalRequests)
        {
            currentRequest = request;

            await request.PlayRequest(outputDevice);

            currentRequest = null;

            await Task.Delay(50);

            if (cancel)
            {
                break;
            }
        }
    }
}
