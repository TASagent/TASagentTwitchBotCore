namespace TASagentTwitchBot.Core.Audio;

[AutoRegister]
public interface IAudioDeviceManager
{
    List<string> GetOutputDevices();
    List<string> GetInputDevices();

    bool IsAudioDeviceValid(AudioDeviceType audioType, string audioDevice);

    string GetAudioDeviceName(AudioDeviceType audioType);

    bool OverrideAudioDevice(AudioDeviceType audioType, string audioDevice);

    void RegisterUpdateListener(IAudioDeviceUpdateListener listener);
}

[AutoRegister]
public interface IAudioDeviceUpdateListener
{
    void NotifyAudioDeviceUpdate(AudioDeviceType audioType);
}

public enum AudioDeviceType
{
    DefaultOutput,
    VoiceOutput,
    EffectOutput,
    TTSOutput,
    MidiOutput,

    DefaultInput,
    MicrophoneInput
}
