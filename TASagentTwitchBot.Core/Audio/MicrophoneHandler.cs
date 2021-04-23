#define USE_STANDARD_DEBUG_OUTPUT 

using System;
using System.Linq;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using BGC.Mathematics;

namespace TASagentTwitchBot.Core.Audio
{
    public interface IMicrophoneHandler : IDisposable
    {
        void Start();
        void ResetVoiceStream();

        List<string> GetOutputDevices();
        List<string> GetInputDevices();

        string GetCurrentVoiceOutputDevice();
        string GetCurrentVoiceInputDevice();

        void UpdateVoiceEffect(Effects.Effect effect);
        bool UpdateVoiceOutputDevice(string device);
        bool UpdateVoiceInputDevice(string device);

        void BumpPitch(bool up);
        void SetPitch(double pitchFactor);
        string GetCurrentEffect();
    }

    /// <summary>
    /// Coordinates different requested display features so they don't collide
    /// </summary>
    public class MicrophoneHandler : IMicrophoneHandler, IDisposable
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly ICommunication communication;

        private bool disposedValue;

        private MMDevice targetInputDevice;
        private MMDevice targetOutputDevice;
        private WasapiOut outputDevice;
        private BufferedWasapiQueuer recordingStream;

        private Effects.Effect currentEffect = null;
        private Effects.PitchShiftEffect lastPitchShiftEffect = null;

        public MicrophoneHandler(
            Config.IBotConfigContainer botConfigContainer,
            ICommunication communication)
        {
            this.communication = communication;
            botConfig = botConfigContainer.BotConfig;
        }

        public void Start()
        {
            UpdateVoiceEffect(new Effects.NoEffect());
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

        public string GetCurrentVoiceOutputDevice() =>
            targetOutputDevice?.FriendlyName ?? botConfig.VoiceOutputDevice;

        public string GetCurrentVoiceInputDevice() =>
            targetInputDevice?.FriendlyName ?? botConfig.VoiceInputDevice;

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

            communication.SendDebugMessage($"New Voice Effect: {effect.GetEffectsChain()}");
        }

        public bool UpdateVoiceOutputDevice(string device)
        {
            if (string.IsNullOrEmpty(device))
            {
                return false;
            }

            if (targetOutputDevice is not null && targetOutputDevice.FriendlyName == device)
            {
                //Tried to set to current device
                return true;
            }

            MMDevice newOutputDevice = GetOutputDevice(device);

            if (newOutputDevice is null)
            {
                //Device not found
                return false;
            }

            CleanUpActiveStream();

            //Switch devices
            targetOutputDevice?.Dispose();
            targetOutputDevice = newOutputDevice;

            ResetVoiceStream();

            return true;
        }

        public bool UpdateVoiceInputDevice(string device)
        {
            if (string.IsNullOrEmpty(device))
            {
                return false;
            }

            if (targetInputDevice is not null && targetInputDevice.FriendlyName == device)
            {
                //Tried to set to current device
                return true;
            }

            MMDevice newInputDevice = GetInputDevice(device);

            if (newInputDevice is null)
            {
                //Device not found
                return false;
            }

            CleanUpActiveStream();

            //Switch devices
            targetInputDevice?.Dispose();
            targetInputDevice = newInputDevice;

            ResetVoiceStream();

            return true;
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
#if DEBUG && USE_STANDARD_DEBUG_OUTPUT
                targetOutputDevice = GetDefaultOutputDevice();
#else
                targetOutputDevice = GetOutputDevice(botConfig.VoiceOutputDevice);
#endif
                if (targetOutputDevice is null)
                {
                    targetOutputDevice = GetDefaultOutputDevice();
                    
                    if (targetOutputDevice is null)
                    {
                        //Failed to get a device
                        communication.SendErrorMessage("Unable to initialize voice output device.");
                        return;
                    }
                    else
                    {
                        communication.SendWarningMessage($"Audio output device {botConfig.VoiceOutputDevice} not found. " +
                            $"Fell back to default audio output device: {targetOutputDevice.DeviceFriendlyName}");
                    }
                }
            }

            if (targetInputDevice is null)
            {
                //Set up device
                targetInputDevice = GetInputDevice(botConfig.VoiceInputDevice);
                if (targetInputDevice is null)
                {
                    targetInputDevice = GetDefaultInputDevice();

                    if (targetInputDevice is null)
                    {
                        //Failed to get a device
                        communication.SendErrorMessage("Unable to initialize voice input device.");
                        return;
                    }
                    else
                    {
                        communication.SendWarningMessage($"Audio input device {botConfig.VoiceInputDevice} not found. " +
                            $"Fell back to default audio input device: {targetInputDevice.DeviceFriendlyName}");
                    }
                }
            }

            CleanUpActiveStream();

            if (botConfig.MicConfiguration.Enabled)
            {
                outputDevice = new WasapiOut(targetOutputDevice, AudioClientShareMode.Shared, true, 10);

                recordingStream = new BufferedWasapiQueuer(targetInputDevice, 1000);
                outputDevice.Init(recordingStream.ApplyMicrophoneEffects(botConfig.MicConfiguration, currentEffect));
                outputDevice.Play();
            }
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

        private static MMDevice GetDefaultOutputDevice()
        {
            using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        private static MMDevice GetOutputDevice(string defaultAudioOutputDevice)
        {
            using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            return enumerator
                    .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                    .FirstOrDefault(x => x.FriendlyName == defaultAudioOutputDevice);
        }

        private static MMDevice GetDefaultInputDevice()
        {
            using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
        }

        private static MMDevice GetInputDevice(string defaultAudioInputDevice)
        {
            using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            return enumerator
                .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                .FirstOrDefault(x => x.FriendlyName == defaultAudioInputDevice);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    recordingStream?.Dispose();
                    recordingStream = null;

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
}
