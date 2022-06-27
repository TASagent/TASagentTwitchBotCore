using NAudio.CoreAudioApi;

namespace TASagentTwitchBot.Core.Audio;

[AutoRegister]
public interface IAudioPlayer
{
    void DemandPlayAudioImmediate(AudioRequest audioRequest);

    void RequestCancel();

    Task PlayAudioRequest(AudioRequest audioRequest);
}


public class NAudioPlayer : IAudioPlayer
{
    private readonly ICommunication communication;
    private readonly INAudioDeviceManager audioDeviceManager;

    public NAudioPlayer(
        ICommunication communication,
        INAudioDeviceManager audioDeviceManager)
    {
        this.communication = communication;
        this.audioDeviceManager = audioDeviceManager;
    }

    private AudioRequest? currentAudioRequest = null;

    public async Task PlayAudioRequest(AudioRequest audioRequest)
    {
        try
        {
            using MMDevice? effectDevice = audioDeviceManager.GetAudioDevice(AudioDeviceType.EffectOutput);
            using MMDevice? ttsDevice = audioDeviceManager.GetAudioDevice(AudioDeviceType.TTSOutput);

            if (effectDevice is null)
            {
                //Failed to get a device
                return;
            }

            currentAudioRequest = audioRequest;
            await audioRequest.PlayRequest(effectDevice, ttsDevice ?? effectDevice);
            currentAudioRequest = null;
        }
        catch (Exception e)
        {
            communication.SendErrorMessage(e.ToString());
        }
    }

    public async void DemandPlayAudioImmediate(AudioRequest audioRequest)
    {
        try
        {
            using MMDevice? effectDevice = audioDeviceManager.GetAudioDevice(AudioDeviceType.EffectOutput);
            using MMDevice? ttsDevice = audioDeviceManager.GetAudioDevice(AudioDeviceType.TTSOutput);

            if (effectDevice is null)
            {
                //Failed to get a device
                return;
            }

            await audioRequest.PlayRequest(effectDevice, ttsDevice ?? effectDevice);
        }
        catch (Exception e)
        {
            communication.SendErrorMessage(e.ToString());
        }
    }

    public void RequestCancel() => currentAudioRequest?.RequestCancel();
}
