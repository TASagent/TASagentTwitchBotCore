using NAudio.CoreAudioApi;

namespace TASagentTwitchBot.Core.Audio;

[AutoRegister]
public interface INAudioDeviceManager : IAudioDeviceManager
{
    MMDevice? GetAudioDevice(AudioDeviceType audioType);
}

public class NAudioDeviceManager : IAudioDeviceManager, INAudioDeviceManager
{
    private readonly Config.BotConfiguration botConfig;
    private readonly ICommunication communication;

    private readonly Dictionary<AudioDeviceType, string> deviceOverrides = new Dictionary<AudioDeviceType, string>();

    private readonly List<IAudioDeviceUpdateListener> updateListeners = new List<IAudioDeviceUpdateListener>();

    public NAudioDeviceManager(
        Config.BotConfiguration botConfig,
        ICommunication communication)
    {
        this.botConfig = botConfig;
        this.communication = communication;
    }

    public void RegisterUpdateListener(IAudioDeviceUpdateListener listener)
    {
        updateListeners.Add(listener);
    }

    public List<string> GetOutputDevices()
    {
        using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

        return enumerator
            .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(x => x.FriendlyName)
            .ToList();
    }

    public List<string> GetInputDevices()
    {
        using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

        return enumerator
            .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(x => x.FriendlyName)
            .ToList();
    }

    public string GetAudioDeviceName(AudioDeviceType audioType)
    {
        if (deviceOverrides.ContainsKey(audioType))
        {
            return deviceOverrides[audioType];
        }

        switch (audioType)
        {
            case AudioDeviceType.DefaultOutput:
            case AudioDeviceType.DefaultInput:
                return GetAudioDevice(audioType)!.FriendlyName;

            case AudioDeviceType.VoiceOutput: return botConfig.VoiceOutputDevice;
            case AudioDeviceType.EffectOutput: return botConfig.EffectOutputDevice;
            case AudioDeviceType.TTSOutput: return botConfig.TTSOutputDevice;
            case AudioDeviceType.MidiOutput: return botConfig.MidiOutputDevice;

            case AudioDeviceType.MicrophoneInput: return botConfig.VoiceInputDevice;

            default:
                throw new NotSupportedException($"AudioDeviceType not supported {audioType}");
        }
    }


    public bool OverrideAudioDevice(AudioDeviceType audioType, string audioDevice)
    {
        if (string.IsNullOrEmpty(audioDevice))
        {
            deviceOverrides.Remove(audioType);
            return true;
        }

        if (deviceOverrides.ContainsKey(audioType) && deviceOverrides[audioType] == audioDevice)
        {
            //Tried to set to current device
            return true;
        }

        if (!IsAudioDeviceValid(audioType, audioDevice))
        {
            //Unable to set device
            return false;
        }

        deviceOverrides[audioType] = audioDevice;

        foreach (IAudioDeviceUpdateListener? listener in updateListeners)
        {
            listener.NotifyAudioDeviceUpdate(audioType);
        }

        return true;
    }


    public bool IsAudioDeviceValid(AudioDeviceType audioType, string audioDevice)
    {
        using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

        switch (audioType)
        {
            case AudioDeviceType.DefaultOutput:
            case AudioDeviceType.VoiceOutput:
            case AudioDeviceType.EffectOutput:
            case AudioDeviceType.TTSOutput:
            case AudioDeviceType.MidiOutput:
                return enumerator
                    .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                    .Any(x => x.FriendlyName == audioDevice);

            case AudioDeviceType.DefaultInput:
            case AudioDeviceType.MicrophoneInput:
                return enumerator
                    .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                    .Any(x => x.FriendlyName == audioDevice);

            default:
                communication.SendErrorMessage($"Unsupported AudioDeviceType {audioType}.");
                return false;
        }
    }

    public MMDevice? GetAudioDevice(AudioDeviceType audioType)
    {
        switch (audioType)
        {
            case AudioDeviceType.DefaultOutput:
                {
                    using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                    return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                }

            case AudioDeviceType.DefaultInput:
                {
                    using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                    return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                }

            case AudioDeviceType.VoiceOutput:
            case AudioDeviceType.EffectOutput:
            case AudioDeviceType.TTSOutput:
            case AudioDeviceType.MidiOutput:
                {
                    string audioDeviceName = GetAudioDeviceName(audioType);
                    if (string.IsNullOrEmpty(audioDeviceName))
                    {
                        return null;
                    }

                    using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                    MMDevice? identifiedDevice = enumerator
                        .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                        .FirstOrDefault(x => x.FriendlyName == audioDeviceName);

                    if (identifiedDevice is null)
                    {
                        communication.SendErrorMessage($"Saved AudioDevice \"{audioDeviceName}\" cannot be found for audioType {audioType}.");
                    }

                    return identifiedDevice;
                }

            case AudioDeviceType.MicrophoneInput:
                {
                    string audioDeviceName = GetAudioDeviceName(audioType);
                    if (string.IsNullOrEmpty(audioDeviceName))
                    {
                        return null;
                    }

                    using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                    MMDevice? identifiedDevice = enumerator
                        .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                        .FirstOrDefault(x => x.FriendlyName == audioDeviceName);

                    if (identifiedDevice is null)
                    {
                        communication.SendErrorMessage($"Saved AudioDevice \"{audioDeviceName}\" cannot be found for audioType {audioType}.");
                    }

                    return identifiedDevice;
                }


            default:
                communication.SendErrorMessage($"Unsupported AudioDeviceType {audioType}.");
                return null;
        }
    }
}
