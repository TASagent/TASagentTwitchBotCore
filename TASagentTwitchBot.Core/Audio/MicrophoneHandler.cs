using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.MediaFoundation;
using BGC.Mathematics;
using System.Reflection.PortableExecutable;

namespace TASagentTwitchBot.Core.Audio;

[AutoRegister]
public interface IMicrophoneHandler : IDisposable
{
    void ResetVoiceStream();

    void UpdateVoiceEffect(Effects.Effect effect);

    void BumpPitch(bool up);
    void SetPitch(double pitchFactor);
    string GetCurrentEffect();

    void RecordVoiceStream(string filePath);
    void StopRecordingVoiceStream();
}

/// <summary>
/// Coordinates different requested display features so they don't collide
/// </summary>
public class NAudioMicrophoneHandler : IMicrophoneHandler, IAudioDeviceUpdateListener, IStartupListener, IShutdownListener, IDisposable
{
    private readonly Config.BotConfiguration botConfig;
    private readonly ICommunication communication;
    private readonly INAudioDeviceManager audioDeviceManager;

    private bool disposedValue;

    private MMDevice? targetInputDevice;
    private MMDevice? targetOutputDevice;
    private WasapiOut? outputDevice;
    private BufferedWasapiQueuer? recordingStream;

    private BufferedWasapiQueuer? fileRecordingStream;
    private string? fileRecordingPath;

    private Effects.Effect? currentEffect = null;
    private Effects.PitchShiftEffect? lastPitchShiftEffect = null;

    public NAudioMicrophoneHandler(
        Config.BotConfiguration botConfig,
        ApplicationManagement applicationManagement,
        ICommunication communication,
        INAudioDeviceManager audioDeviceManager)
    {
        this.communication = communication;
        this.botConfig = botConfig;
        this.audioDeviceManager = audioDeviceManager;

        applicationManagement.RegisterShutdownListener(this);
        audioDeviceManager.RegisterUpdateListener(this);

        //Start
        UpdateVoiceEffect(new Effects.NoEffect());
    }

    public void UpdateVoiceEffect(Effects.Effect effect)
    {
        if (effect is null)
        {
            effect = new Effects.NoEffect();
        }

        currentEffect = effect;

        //Get the PitchShiftEffect, if any
        lastPitchShiftEffect = effect.GetEffects().OfType<Effects.PitchShiftEffect>().FirstOrDefault();

        ResetVoiceStream();

        if (botConfig.MicConfiguration.Enabled)
        {
            communication.SendDebugMessage($"New Voice Effect: {effect.GetEffectsChain()}");
        }
    }

    void IAudioDeviceUpdateListener.NotifyAudioDeviceUpdate(AudioDeviceType audioDevice)
    {
        switch (audioDevice)
        {
            case AudioDeviceType.VoiceOutput:
                {
                    MMDevice? newOutputDevice = audioDeviceManager.GetAudioDevice(audioDevice);

                    CleanUpActiveStream();

                    targetOutputDevice?.Dispose();
                    targetOutputDevice = newOutputDevice;

                    ResetVoiceStream();
                }
                break;

            case AudioDeviceType.MicrophoneInput:
                {
                    MMDevice? newInputDevice = audioDeviceManager.GetAudioDevice(audioDevice);

                    CleanUpActiveStream();

                    targetInputDevice?.Dispose();
                    targetInputDevice = newInputDevice;

                    ResetVoiceStream();
                }
                break;

            default:
                //Do nothing
                return;
        }
    }

    private void CleanUpActiveStream()
    {
        if (recordingStream is not null)
        {
            //Clean up last effect
            recordingStream.StopRecording();
            recordingStream.Dispose();
            recordingStream = null;
        }

        if (fileRecordingStream is not null)
        {
            //Clean up last effect
            fileRecordingStream.StopRecording();
            fileRecordingStream.Dispose();
            fileRecordingStream = null;
        }

        if (outputDevice is not null)
        {
            outputDevice.Stop();
            outputDevice.Dispose();
            outputDevice = null;
        }
    }

    public void ResetVoiceStream()
    {
        if (targetOutputDevice is null)
        {
            //Set up device
            targetOutputDevice = audioDeviceManager.GetAudioDevice(AudioDeviceType.VoiceOutput);
        }

        if (targetInputDevice is null)
        {
            //Set up device
            targetInputDevice = audioDeviceManager.GetAudioDevice(AudioDeviceType.MicrophoneInput);
        }

        CleanUpActiveStream();

        if (targetOutputDevice is not null && targetInputDevice is not null && botConfig.MicConfiguration.Enabled)
        {
            outputDevice = new WasapiOut(targetOutputDevice, AudioClientShareMode.Shared, true, 10);

            recordingStream = new BufferedWasapiQueuer(targetInputDevice, 1000);
            outputDevice.Init(recordingStream.ApplyMicrophoneEffects(botConfig.MicConfiguration, currentEffect));
            outputDevice.Play();
        }
    }

    public void RecordVoiceStream(string filePath)
    {
        if (targetInputDevice is null)
        {
            //Set up device
            targetInputDevice = audioDeviceManager.GetAudioDevice(AudioDeviceType.MicrophoneInput);

            if (targetInputDevice is null)
            {
                communication.SendErrorMessage("Unable to initialize voice input device.");
                return;
            }
        }

        if (fileRecordingStream is not null)
        {
            fileRecordingStream.StopRecording();
            fileRecordingStream.Dispose();
            fileRecordingStream = null;
        }

        fileRecordingPath = filePath;
        fileRecordingStream = new BufferedWasapiQueuer(targetInputDevice, 10000);
    }

    public void StopRecordingVoiceStream()
    {
        if (fileRecordingStream is null)
        {
            return;
        }

        fileRecordingStream.StopRecording();
        MediaFoundationApi.Startup();

        using MediaFoundationResampler resampler = new MediaFoundationResampler(
            fileRecordingStream.ApplyMicrophoneEffects(botConfig.MicConfiguration, new Effects.NoEffect()).ToWaveProvider(),
            new WaveFormat(44100, 16, 2));

        MediaFoundationEncoder.EncodeToMp3(
            resampler,
            fileRecordingPath);

        MediaFoundationApi.Shutdown();

        fileRecordingStream.Dispose();
        fileRecordingStream = null;
    }

    public void BumpPitch(bool up)
    {
        if (lastPitchShiftEffect is not null)
        {
            if (up)
            {
                lastPitchShiftEffect.PitchFactor *= 1.1;
            }
            else
            {
                lastPitchShiftEffect.PitchFactor /= 1.1;
            }
        }
    }

    public void SetPitch(double pitchFactor)
    {
        pitchFactor = GeneralMath.Clamp(pitchFactor, 0.1, 10.0);

        if (lastPitchShiftEffect is not null)
        {
            lastPitchShiftEffect.PitchFactor = pitchFactor;
        }
    }

    public string GetCurrentEffect() => currentEffect?.GetEffectsChain() ?? "None";

    void IShutdownListener.NotifyShuttingDown()
    {
        CleanUpActiveStream();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                recordingStream?.Dispose();
                recordingStream = null;

                fileRecordingStream?.Dispose();
                fileRecordingStream = null;

                outputDevice?.Dispose();
                outputDevice = null;

                targetOutputDevice?.Dispose();
                targetOutputDevice = null;

                targetInputDevice?.Dispose();
                targetInputDevice = null;
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
