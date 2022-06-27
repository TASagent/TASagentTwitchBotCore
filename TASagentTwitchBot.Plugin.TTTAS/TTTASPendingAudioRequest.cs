
using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Audio.Effects;

namespace TASagentTwitchBot.Plugin.TTTAS;

public class TTTASPendingAudioRequest : AudioFileRequest
{
    public readonly TTTASProvider.PendingRecording pendingRecording;

    public Task WaitForReadyAsync() => pendingRecording.WaitForReadyAsync();

    public TTTASPendingAudioRequest(
        TTTASProvider.PendingRecording recording,
        Effect effectsChain)
        : base(recording.FilePath, true, effectsChain)
    {
        pendingRecording = recording;
    }
}
