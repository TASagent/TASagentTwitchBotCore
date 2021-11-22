#define USE_STANDARD_DEBUG_OUTPUT 

using NAudio.CoreAudioApi;

namespace TASagentTwitchBot.Core.Audio;

public interface IAudioPlayer
{
    string GetCurrentEffectOutputDevice();
    bool UpdateEffectOutputDevice(string device);

    void DemandPlayAudioImmediate(AudioRequest audioRequest);

    void RequestCancel();

    Task PlayAudioRequest(AudioRequest audioRequest);
}


public class AudioPlayer : IAudioPlayer
{
    private readonly string defaultAudioDevice;
    private readonly ICommunication communication;

    private string overrideDevice = "";

    public AudioPlayer(
        Config.BotConfiguration botConfig,
        ICommunication communication)
    {
        this.communication = communication;
        defaultAudioDevice = botConfig.EffectOutputDevice;
    }

    private AudioRequest? currentAudioRequest = null;

    public async Task PlayAudioRequest(AudioRequest audioRequest)
    {
        try
        {
            using MMDevice targetDevice = GetAudioDevice();

            if (targetDevice is null)
            {
                //Failed to get a device
                return;
            }

            currentAudioRequest = audioRequest;
            await audioRequest.PlayRequest(targetDevice);
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
            using MMDevice targetDevice = GetAudioDevice();

            if (targetDevice is null)
            {
                //Failed to get a device
                return;
            }

            await audioRequest.PlayRequest(targetDevice);
        }
        catch (Exception e)
        {
            communication.SendErrorMessage(e.ToString());
        }
    }

    public void RequestCancel() => currentAudioRequest?.RequestCancel();

    public bool UpdateEffectOutputDevice(string device)
    {
        if (string.IsNullOrEmpty(device))
        {
            return false;
        }

        if (GetCurrentEffectOutputDevice() == device)
        {
            //Tried to set to current device
            return true;
        }

        if (!IsOutputDeviceValid(device))
        {
            //Unable to set device
            return false;
        }

        overrideDevice = device;

        return true;
    }

    public string GetCurrentEffectOutputDevice()
    {
        if (!string.IsNullOrEmpty(overrideDevice))
        {
            return overrideDevice;
        }

#if DEBUG && USE_STANDARD_DEBUG_OUTPUT
            return GetDefaultOutputDeviceName();
#else
        return defaultAudioDevice;
#endif
    }

    private MMDevice GetAudioDevice()
    {
        MMDevice? targetDevice = null;

        if (!string.IsNullOrEmpty(overrideDevice))
        {
            targetDevice = GetOutputDevice(overrideDevice);

            if (targetDevice is null)
            {
                communication.SendWarningMessage($"Unable to initialize override audio output device {overrideDevice}. Falling back to default.");
            }
        }

        if (targetDevice is null)
        {
            //Use default because override was unspecified or failed
#if DEBUG && USE_STANDARD_DEBUG_OUTPUT
                targetDevice = GetDefaultOutputDevice();
#else
            targetDevice = GetOutputDevice(defaultAudioDevice);
#endif

            if (targetDevice is null)
            {
                targetDevice = GetDefaultOutputDevice();

                if (targetDevice is null)
                {
                    communication.SendErrorMessage("Unable to initialize effect output device.");
                }
                else
                {
                    communication.SendWarningMessage($"Audio output device {defaultAudioDevice} not found. " +
                        $"Fell back to default audio output device: {targetDevice.DeviceFriendlyName}");
                }
            }
        }

        return targetDevice!;
    }

#if DEBUG && USE_STANDARD_DEBUG_OUTPUT
        private static string GetDefaultOutputDeviceName()
        {
            using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)!.FriendlyName;
        }
#endif

    private static MMDevice GetDefaultOutputDevice()
    {
        using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    private static MMDevice? GetOutputDevice(string audioOutputDevice)
    {
        using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        return enumerator
                .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .FirstOrDefault(x => x.FriendlyName == audioOutputDevice);
    }

    private static bool IsOutputDeviceValid(string audioOutputDevice)
    {
        using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
        return enumerator
                .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .Any(x => x.FriendlyName == audioOutputDevice);
    }
}
