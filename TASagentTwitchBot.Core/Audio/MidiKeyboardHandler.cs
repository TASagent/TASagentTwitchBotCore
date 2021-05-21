#define USE_STANDARD_DEBUG_OUTPUT 

using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;
using NAudio.Midi;
using NAudio.Wave;
using BGC.Audio;
using BGC.Audio.NAudio;
using BGC.Audio.Midi;

namespace TASagentTwitchBot.Core.Audio
{
    public class MidiKeyboardHandler : IDisposable
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly ICommunication communication;
        private readonly ISoundEffectSystem soundEffectsSystem;

        private MMDevice targetInputDevice;
        private MMDevice targetOutputDevice;
        private WasapiOut outputDevice;
        private MidiIn currentMidiDevice;
        //private MidiOut currentMidiDeviceOut;
        private string currentMidiDeviceName;

        private bool disposedValue;

        private readonly Dictionary<int, MidiBinding> bindings = new Dictionary<int, MidiBinding>()
        {
            { 1, new SimpleInstrumentBinding(Instrument.Organ) }
        };

        private enum Instrument
        {
            Organ,
            SquareWaves,
            Sawtooth,
            Flex,
            VocodedVoice,
            MAX
        }

        public MidiKeyboardHandler(
            Config.BotConfiguration botConfig,
            ICommunication communication,
            ISoundEffectSystem soundEffectsSystem)
        {
            this.botConfig = botConfig;
            this.communication = communication;
            this.soundEffectsSystem = soundEffectsSystem;
        }

        private void MidiMessageReceived(object sender, MidiInMessageEventArgs e)
        {
            if (e.MidiEvent is NoteOnEvent noteOnEvent)
            {
                if (bindings.ContainsKey(noteOnEvent.Channel))
                {
                    bindings[noteOnEvent.Channel].HandleNoteOn(noteOnEvent.NoteNumber);
                }
            }
            else if (e.MidiEvent is NoteEvent noteEvent)
            {
                if (bindings.ContainsKey(noteEvent.Channel))
                {
                    bindings[noteEvent.Channel].HandleNoteOff(noteEvent.NoteNumber);
                }
            }
            else if (e.MidiEvent is ControlChangeEvent controlChange)
            {
                if (bindings.ContainsKey(controlChange.Channel))
                {
                    bindings[controlChange.Channel].HandleController((int)controlChange.Controller, controlChange.ControllerValue);
                }
            }
            else
            {
                communication.SendDebugMessage($"Received Midi Message: {e.MidiEvent}");
            }
        }

        public List<string> GetMidiDevices()
        {
            List<string> devices = new List<string>();

            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                devices.Add(MidiIn.DeviceInfo(i).ProductName);
            }

            return devices;
        }

        public string GetCurrentMidiDevice()
        {
            if (currentMidiDevice is null || currentMidiDeviceName is null)
            {
                return "";
            }

            return currentMidiDeviceName;
        }

        public bool UpdateCurrentMidiDevice(string name)
        {
            if (currentMidiDevice is not null)
            {
                try
                {
                    currentMidiDevice.Stop();
                    currentMidiDevice.Dispose();
                    currentMidiDevice = null;
                    currentMidiDeviceName = "";
                }
                catch (Exception ex)
                {
                    communication.SendWarningMessage($"Exception closing Midi Device: {ex}");
                    currentMidiDevice = null;
                    currentMidiDeviceName = "";
                }

                //try
                //{
                //    currentMidiDeviceOut?.Close();
                //    currentMidiDeviceOut?.Dispose();
                //    currentMidiDeviceOut = null;
                //}
                //catch (Exception ex)
                //{
                //    communication.SendWarningMessage($"Exception closing Midi Out Device: {ex}");
                //    currentMidiDeviceOut = null;
                //}
            }

            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            int newDevice = -1;
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                if (MidiIn.DeviceInfo(i).ProductName == name)
                {
                    newDevice = i;
                    break;
                }
            }

            if (newDevice == -1)
            {
                //Not found
                return false;
            }

            try
            {
                currentMidiDeviceName = MidiIn.DeviceInfo(newDevice).ProductName;
                currentMidiDevice = new MidiIn(newDevice);
                currentMidiDevice.MessageReceived += MidiMessageReceived;
                currentMidiDevice.ErrorReceived += MidiErrorReceived;
                currentMidiDevice.Start();
            }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"Exception starting Midi Device: {ex}");
                currentMidiDevice = null;
                currentMidiDeviceName = "";
                return false;
            }

            RecreateAudioStream();

            //Success
            return true;
        }

        private void MidiErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            communication.SendErrorMessage($"Midi Error received: {e.MidiEvent}");
        }

        public bool BindToSoundEffect(string soundEffectName)
        {
            if (string.IsNullOrWhiteSpace(soundEffectName))
            {
                return false;
            }

            if (soundEffectName.StartsWith('/'))
            {
                soundEffectName = soundEffectName[1..];
            }

            SoundEffect soundEffect = soundEffectsSystem.GetSoundEffectByAlias(soundEffectName);

            if (soundEffect is null)
            {
                return false;
            }

            bindings[1].Dispose();
            bindings[1] = new SoundEffectBinding(soundEffect.FilePath);

            RecreateAudioStream();

            return true;
        }

        public bool BindToInstrument(string instrumentString)
        {
            Instrument instrument = ParseInstrument(instrumentString);

            switch (instrument)
            {
                case Instrument.Organ:
                case Instrument.SquareWaves:
                case Instrument.Sawtooth:
                    bindings[1].Dispose();
                    bindings[1] = new SimpleInstrumentBinding(instrument);
                    break;

                case Instrument.VocodedVoice:
                    {
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
                                    return false;
                                }
                                else
                                {
                                    communication.SendWarningMessage($"Audio input device {botConfig.VoiceInputDevice} not found. " +
                                        $"Fell back to default audio input device: {targetInputDevice.DeviceFriendlyName}");
                                }
                            }
                        }

                        bindings[1].Dispose();
                        bindings[1] = new VocodedVoiceBinding();
                    }
                    break;

                case Instrument.Flex:
                    bindings[1].Dispose();
                    bindings[1] = new FlexInstrumentBinding();
                    break;


                case Instrument.MAX:
                    //Could not parse
                    return false;

                default:
                    //Unsupported
                    communication.SendErrorMessage($"Unsupported Midi Instrument for binding: {instrument}");
                    return false;
            }

            RecreateAudioStream();

            return true;
        }

        private static Instrument ParseInstrument(string instrumentString)
        {
            if (!Enum.TryParse(instrumentString, out Instrument instrument))
            {
                instrument = Instrument.MAX;
            }

            return instrument;
        }

        public List<string> GetSupportedInstruments()
        {
            List<string> instruments = new List<string>();

            for (Instrument instrument = 0; instrument < Instrument.MAX; instrument++)
            {
                instruments.Add(instrument.ToString());
            }

            return instruments;
        }

        private void CleanUpActiveStream()
        {
            bindings[1].CleanUp();

            if (outputDevice is not null)
            {
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
            }
        }

        private void RecreateAudioStream()
        {
            if (targetOutputDevice is null)
            {
                //Set up device
#if DEBUG && USE_STANDARD_DEBUG_OUTPUT
                targetOutputDevice = GetDefaultOutputDevice();
#else
                targetOutputDevice = GetOutputDevice(botConfig.EffectOutputDevice);
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
                        communication.SendWarningMessage($"Audio output device {botConfig.EffectOutputDevice} not found. " +
                            $"Fell back to default audio output device: {targetOutputDevice.DeviceFriendlyName}");
                    }
                }
            }

            CleanUpActiveStream();

            outputDevice = new WasapiOut(targetOutputDevice, AudioClientShareMode.Shared, true, 10);
            outputDevice.Init(bindings[1].CreateOutputStream(this).ToBufferedSampleProvider(256));
            outputDevice.Play();
        }

        public string GetCurrentMidiOutputDevice() =>
            targetOutputDevice?.FriendlyName ??
#if DEBUG && USE_STANDARD_DEBUG_OUTPUT
                GetDefaultOutputDevice().FriendlyName;
#else
                botConfig.EffectOutputDevice;
#endif

        public bool UpdateCurrentMidiOutputDevice(string device)
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

            RecreateAudioStream();

            return true;
        }

        private static MMDevice GetDefaultOutputDevice()
        {
            using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        private static MMDevice GetOutputDevice(string audioDevice)
        {
            using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            return enumerator
                    .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                    .FirstOrDefault(x => x.FriendlyName == audioDevice);
        }

        private static MMDevice GetDefaultInputDevice()
        {
            using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
        }

        private static MMDevice GetInputDevice(string inputDevice)
        {
            using MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            return enumerator
                .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                .FirstOrDefault(x => x.FriendlyName == inputDevice);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (MidiBinding binding in bindings.Values)
                    {
                        binding.Dispose();
                    }

                    bindings.Clear();

                    currentMidiDevice?.Dispose();
                    currentMidiDevice = null;

                    outputDevice?.Dispose();
                    outputDevice = null;

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

        #region Bindings

        private abstract class MidiBinding : IDisposable
        {
            private bool disposedValue;

            public abstract void HandleNoteOn(int key);
            public abstract void HandleNoteOff(int key);
            public virtual void HandleController(int controller, int value) { }

            public abstract IBGCStream CreateOutputStream(MidiKeyboardHandler midiKeyboardHandler);

            public abstract void CleanUp();
            protected abstract void ChildDisposal();


            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        ChildDisposal();
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

        private class SoundEffectBinding : MidiBinding
        {
            private readonly string filePath;
            private readonly IBGCStream cachedNoteStream;
            private RollingStreamAdder rollingStreamAdder = null;

            public SoundEffectBinding(string filePath)
            {
                this.filePath = filePath;

                using DisposableWaveProvider audioFile = AudioTools.GetWaveProvider(filePath);
                cachedNoteStream = audioFile.ToBGCStream().SlowRangeFitter().StreamLevelScaler(-10).SafeCache();
            }

            public override IBGCStream CreateOutputStream(MidiKeyboardHandler _)
            {
                if (rollingStreamAdder is not null)
                {
                    rollingStreamAdder.Terminate();
                    rollingStreamAdder.Dispose();
                    rollingStreamAdder = null;
                }

                rollingStreamAdder = new RollingStreamAdder(cachedNoteStream.Channels, cachedNoteStream.SamplingRate);

                return rollingStreamAdder.LimitStream();
            }

            public override void HandleNoteOn(int key)
            {
                rollingStreamAdder?.AddStream(
                    stream: cachedNoteStream.Cache().PitchShift(Math.Pow(2, (key - 60) / 12.0)),
                    key);
            }

            public override void HandleNoteOff(int key)
            {
                rollingStreamAdder?.ReleaseNote(key);
            }

            public override void CleanUp()
            {
                if (rollingStreamAdder is not null)
                {
                    rollingStreamAdder.Terminate();
                    rollingStreamAdder.Dispose();
                    rollingStreamAdder = null;
                }
            }

            protected override void ChildDisposal()
            {
                if (rollingStreamAdder is not null)
                {
                    rollingStreamAdder.Terminate();
                    rollingStreamAdder.Dispose();
                    rollingStreamAdder = null;
                }

                cachedNoteStream.Dispose();
            }

            public override string ToString() => $"SoundEffect Binding: {filePath}";
        }

        private class SimpleInstrumentBinding : MidiBinding
        {
            private readonly Instrument instrument;
            private RollingStreamAdder rollingStreamAdder = null;
            private readonly Random randomizer = new Random();

            public SimpleInstrumentBinding(Instrument instrument)
            {
                this.instrument = instrument;

                switch (instrument)
                {
                    case Instrument.Organ:
                    case Instrument.SquareWaves:
                    case Instrument.Sawtooth:
                        //Pass - this is an acceptable instrument
                        break;

                    default:
                        throw new NotSupportedException($"Simple Instrument Binding does not support {instrument}");
                }
            }

            public override IBGCStream CreateOutputStream(MidiKeyboardHandler _)
            {
                if (rollingStreamAdder is not null)
                {
                    rollingStreamAdder.Terminate();
                    rollingStreamAdder.Dispose();
                    rollingStreamAdder = null;
                }

                rollingStreamAdder = new RollingStreamAdder(1, 44100f);

                return rollingStreamAdder.LimitStream();
            }

            private IBGCStream GetNote(int key)
            {
                double freq = 262 * Math.Pow(2, (key - 60) / 12.0);
                return instrument switch
                {
                    Instrument.Organ => new BGC.Audio.Filters.StreamAdder(
                        //Fundamental
                        new BGC.Audio.Synthesis.TriangleWave(0.1, freq, 2.0 * Math.PI * randomizer.NextDouble()),

                        //Upper Harmonics
                        new BGC.Audio.Synthesis.TriangleWave(0.05, 2 * freq, 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.TriangleWave(0.025, 4 * freq, 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.TriangleWave(0.0125, 8 * freq, 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.TriangleWave(0.00625, 16 * freq, 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.TriangleWave(0.003125, 32 * freq, 2.0 * Math.PI * randomizer.NextDouble()),

                        //Lower Harmonics
                        new BGC.Audio.Synthesis.TriangleWave(0.06, 0.5 * freq, 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.TriangleWave(0.036, 0.25 * freq, 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.TriangleWave(0.0216, 0.125 * freq, 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.TriangleWave(0.01296, 0.0625 * freq, 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.TriangleWave(0.007776, 0.03125 * freq, 2.0 * Math.PI * randomizer.NextDouble())),

                    Instrument.Sawtooth => new BGC.Audio.Filters.StreamAdder(
                        //Fundamental
                        new BGC.Audio.Synthesis.TriangleWave(0.1, freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0),

                        //Upper Harmonics
                        new BGC.Audio.Synthesis.TriangleWave(0.05, 2 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0),
                        new BGC.Audio.Synthesis.TriangleWave(0.025, 4 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0),
                        new BGC.Audio.Synthesis.TriangleWave(0.0125, 8 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0),
                        new BGC.Audio.Synthesis.TriangleWave(0.00625, 16 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0),
                        new BGC.Audio.Synthesis.TriangleWave(0.003125, 32 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0),

                        //Lower Harmonics
                        new BGC.Audio.Synthesis.TriangleWave(0.01, 0.5 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0),
                        new BGC.Audio.Synthesis.TriangleWave(0.001, 0.25 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0),
                        new BGC.Audio.Synthesis.TriangleWave(0.0001, 0.125 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0),
                        new BGC.Audio.Synthesis.TriangleWave(0.00001, 0.0625 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0),
                        new BGC.Audio.Synthesis.TriangleWave(0.000001, 0.003125 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0),

                        //Fifth and Harmonics
                        new BGC.Audio.Synthesis.TriangleWave(0.01, 1.5 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0),
                        new BGC.Audio.Synthesis.TriangleWave(0.005, 2 * 1.5 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0),
                        new BGC.Audio.Synthesis.TriangleWave(0.0025, 4 * 1.5 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0)),

                    Instrument.SquareWaves => new BGC.Audio.Filters.StreamAdder(
                        //Fundamental
                        new BGC.Audio.Synthesis.SquareWave(0.1, freq, phase: 2.0 * Math.PI * randomizer.NextDouble()),

                        //Upper Harmonics
                        new BGC.Audio.Synthesis.SquareWave(0.04, 2 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.SquareWave(0.016, 4 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.SquareWave(0.0064, 8 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.SquareWave(0.00256, 16 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.SquareWave(0.001024, 32 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble()),

                        //Lower Harmonics
                        new BGC.Audio.Synthesis.SquareWave(0.05, 0.5 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.SquareWave(0.025, 0.25 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.SquareWave(0.0125, 0.125 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.SquareWave(0.00625, 0.0625 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.SquareWave(0.003125, 0.03125 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble()),

                        //Fifth and Harmonics
                        new BGC.Audio.Synthesis.SquareWave(0.06, 1.5 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.SquareWave(0.024, 2 * 1.5 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble()),
                        new BGC.Audio.Synthesis.SquareWave(0.0096, 4 * 1.5 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble())),

                    _ => throw new NotSupportedException($"Simple Instrument Binding does not support {instrument}"),
                };
            }

            public override void HandleNoteOn(int key)
            {
                rollingStreamAdder?.AddStream(
                    stream: GetNote(key),
                    key);
            }

            public override void HandleNoteOff(int key)
            {
                rollingStreamAdder?.ReleaseNote(key);
            }

            public override void CleanUp()
            {
                if (rollingStreamAdder is not null)
                {
                    rollingStreamAdder.Terminate();
                    rollingStreamAdder.Dispose();
                    rollingStreamAdder = null;
                }
            }

            protected override void ChildDisposal()
            {
                if (rollingStreamAdder is not null)
                {
                    rollingStreamAdder.Terminate();
                    rollingStreamAdder.Dispose();
                    rollingStreamAdder = null;
                }
            }

            public override string ToString() => $"Instrument Binding: {instrument}";
        }

        private class VocodedVoiceBinding : MidiBinding
        {
            private RollingStreamAdder rollingStreamAdder = null;
            private BufferedWasapiQueuer recordingStream = null;
            private readonly Random randomizer = new Random();

            private int waveformValue = 0;

            public VocodedVoiceBinding()
            {
            }

            public override IBGCStream CreateOutputStream(MidiKeyboardHandler midiKeyboardHandler)
            {
                CleanUp();

                recordingStream = new BufferedWasapiQueuer(midiKeyboardHandler.targetInputDevice, 1000);

                rollingStreamAdder = new RollingStreamAdder(1, recordingStream.SamplingRate);

                return new BGC.Audio.Filters.Vocoder(
                    stream: recordingStream.EnsureMono().ApplyMicrophoneConfigs(midiKeyboardHandler.botConfig.MicConfiguration).ToBufferedStream(1 << 13),
                    carrierStream: rollingStreamAdder.ToBufferedStream(1 << 13),
                    fftSize: 1 << 13)
                    .StreamLevelScaler(10)
                    .LimitStream();
            }

            private IBGCStream GetCarrier(int key)
            {
                double freq = 262 * Math.Pow(2, (key - 60) / 12.0);

                const int SAW_CUTOFF = 128 / 3; 
                const int SQUARE_CUTOFF = 2 * SAW_CUTOFF;

                return waveformValue switch
                {
                    < SAW_CUTOFF => new BGC.Audio.Filters.StreamAdder(
                        //Fundamental
                        new BGC.Audio.Synthesis.TriangleWave(0.1, freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0, samplingRate: recordingStream.SamplingRate),

                        //Upper Harmonics
                        new BGC.Audio.Synthesis.TriangleWave(0.05, 2 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0, samplingRate: recordingStream.SamplingRate),
                        new BGC.Audio.Synthesis.TriangleWave(0.025, 4 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0, samplingRate: recordingStream.SamplingRate),

                        //Lower Harmonics
                        new BGC.Audio.Synthesis.TriangleWave(0.01, 0.5 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0, samplingRate: recordingStream.SamplingRate),
                        new BGC.Audio.Synthesis.TriangleWave(0.001, 0.25 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0, samplingRate: recordingStream.SamplingRate),

                        //Fifth and Harmonics
                        new BGC.Audio.Synthesis.TriangleWave(0.01, 1.5 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0, samplingRate: recordingStream.SamplingRate)),

                    < SQUARE_CUTOFF => new BGC.Audio.Filters.StreamAdder(
                        //Fundamental
                        new BGC.Audio.Synthesis.SquareWave(0.1, freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), samplingRate: recordingStream.SamplingRate),

                        //Upper Harmonics
                        new BGC.Audio.Synthesis.SquareWave(0.04, 2 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), samplingRate: recordingStream.SamplingRate),
                        new BGC.Audio.Synthesis.SquareWave(0.016, 4 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), samplingRate: recordingStream.SamplingRate),

                        //Lower Harmonics
                        new BGC.Audio.Synthesis.SquareWave(0.05, 0.5 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), samplingRate: recordingStream.SamplingRate),
                        new BGC.Audio.Synthesis.SquareWave(0.025, 0.25 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), samplingRate: recordingStream.SamplingRate),

                        //Fifth and Harmonics
                        new BGC.Audio.Synthesis.SquareWave(0.06, 1.5 * freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), samplingRate: recordingStream.SamplingRate)),

                        //Organ
                    _ => new BGC.Audio.Filters.StreamAdder(
                        //Fundamental
                        new BGC.Audio.Synthesis.TriangleWave(0.1, freq, 2.0 * Math.PI * randomizer.NextDouble(), samplingRate: recordingStream.SamplingRate),

                        //Upper Harmonics
                        new BGC.Audio.Synthesis.TriangleWave(0.05, 2 * freq, 2.0 * Math.PI * randomizer.NextDouble(), samplingRate: recordingStream.SamplingRate),
                        new BGC.Audio.Synthesis.TriangleWave(0.025, 4 * freq, 2.0 * Math.PI * randomizer.NextDouble(), samplingRate: recordingStream.SamplingRate),

                        //Lower Harmonics
                        new BGC.Audio.Synthesis.TriangleWave(0.06, 0.5 * freq, 2.0 * Math.PI * randomizer.NextDouble(), samplingRate: recordingStream.SamplingRate),
                        new BGC.Audio.Synthesis.TriangleWave(0.036, 0.25 * freq, 2.0 * Math.PI * randomizer.NextDouble(), samplingRate: recordingStream.SamplingRate))
                };
            }

            public override void HandleNoteOn(int key)
            {
                rollingStreamAdder?.AddStream(
                    stream: GetCarrier(key),
                    key);
            }

            public override void HandleNoteOff(int key)
            {
                rollingStreamAdder?.ReleaseNote(key);
            }

            public override void HandleController(int controller, int value)
            {
                if (controller == 21)
                {
                    waveformValue = value;
                }
            }

            public override void CleanUp()
            {
                if (recordingStream is not null)
                {
                    //Clean up last effect
                    recordingStream.StopRecording();
                    recordingStream.Dispose();
                    recordingStream = null;
                }

                if (rollingStreamAdder is not null)
                {
                    rollingStreamAdder.Terminate();
                    rollingStreamAdder.Dispose();
                    rollingStreamAdder = null;
                }
            }

            protected override void ChildDisposal()
            {
                if (recordingStream is not null)
                {
                    //Clean up last effect
                    recordingStream.StopRecording();
                    recordingStream.Dispose();
                    recordingStream = null;
                }

                if (rollingStreamAdder is not null)
                {
                    rollingStreamAdder.Terminate();
                    rollingStreamAdder.Dispose();
                    rollingStreamAdder = null;
                }
            }

            public override string ToString() => $"Vocoded Voice Binding";
        }

        private class FlexInstrumentBinding : MidiBinding
        {
            private RollingStreamAdder rollingStreamAdder = null;
            private readonly Random randomizer = new Random();

            private readonly int[] controllerValues = new int[8];

            public FlexInstrumentBinding()
            {
                controllerValues[0] = 0;

                for (int i = 1; i < 8; i++)
                {
                    controllerValues[i] = -1;
                }
            }

            public override IBGCStream CreateOutputStream(MidiKeyboardHandler _)
            {
                if (rollingStreamAdder is not null)
                {
                    rollingStreamAdder.Terminate();
                    rollingStreamAdder.Dispose();
                    rollingStreamAdder = null;
                }

                rollingStreamAdder = new RollingStreamAdder(1, 44100f);

                return rollingStreamAdder.LimitStream();
            }

            public override void HandleController(int controller, int value)
            {
                if (controller >= 21 && controller < 29)
                {
                    controllerValues[controller - 21] = value;
                }
            }

            private const int INSTRUMENT_COUNT = 3;
            private const int INSTRUMENT_STRIDE = (128 + INSTRUMENT_COUNT - 1) / INSTRUMENT_COUNT;

            private Instrument CurrentInstrument =>
                (controllerValues[0] / INSTRUMENT_STRIDE) switch
                {
                    0 => Instrument.Organ,
                    1 => Instrument.Sawtooth,
                    2 => Instrument.SquareWaves,
                    _ => throw new Exception($"Bad controllerValue[0]: {controllerValues[0]}")
                };

            private double GetValue(int index, double defaultValue)
            {
                if (controllerValues[index] == -1)
                {
                    return defaultValue;
                }

                return controllerValues[index] / 128.0;
            }

            private IBGCStream GetNote(int key)
            {
                double freq = 262 * Math.Pow(2, (key - 60) / 12.0);

                switch (CurrentInstrument)
                {
                    case Instrument.Organ:
                        {
                            double amplitudeFactor = 0.01 + 0.1 * GetValue(1, 0.6);
                            double upperHarmonicFalloff = GetValue(2, 0.5);
                            double lowerHarmonicFalloff = GetValue(3, 0.6);
                            double fifthRatio = GetValue(4, 0.0);
                            double dutyCycle = GetValue(5, 0.5);
                            double harmonicDutyCycle = GetValue(6, 0.5);

                            //Fundamental
                            BGC.Audio.Filters.StreamAdder streamAdder = new BGC.Audio.Filters.StreamAdder(
                                new BGC.Audio.Synthesis.TriangleWave(amplitudeFactor, freq, 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: dutyCycle));

                            //Upper Harmonics
                            if (upperHarmonicFalloff > 0)
                            {
                                double harmonicFreq = 2 * freq;
                                double harmonicAmplitude = amplitudeFactor * upperHarmonicFalloff;

                                for (int i = 0; i < 5; i++)
                                {
                                    streamAdder.AddStream(
                                        new BGC.Audio.Synthesis.TriangleWave(harmonicAmplitude, harmonicFreq, 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: harmonicDutyCycle));

                                    harmonicFreq *= 2;
                                    harmonicAmplitude *= upperHarmonicFalloff;
                                }
                            }

                            //Lower Harmonics
                            if (lowerHarmonicFalloff > 0)
                            {
                                double harmonicFreq = 0.5 * freq;
                                double harmonicAmplitude = amplitudeFactor * lowerHarmonicFalloff;

                                for (int i = 0; i < 5; i++)
                                {
                                    streamAdder.AddStream(
                                        new BGC.Audio.Synthesis.TriangleWave(harmonicAmplitude, harmonicFreq, 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: harmonicDutyCycle));

                                    harmonicFreq *= 0.5;
                                    harmonicAmplitude *= lowerHarmonicFalloff;
                                }
                            }

                            //Fifths
                            if (fifthRatio > 0)
                            {
                                double harmonicFreq = 1.5 * freq;
                                double harmonicAmplitude = fifthRatio * amplitudeFactor;

                                for (int i = 0; i < 3; i++)
                                {
                                    streamAdder.AddStream(
                                        new BGC.Audio.Synthesis.TriangleWave(harmonicAmplitude, harmonicFreq, 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: harmonicDutyCycle));

                                    if (upperHarmonicFalloff <= 0)
                                    {
                                        break;
                                    }

                                    harmonicFreq *= 2;
                                    harmonicAmplitude *= upperHarmonicFalloff;
                                }
                            }

                            return streamAdder;
                        }

                    case Instrument.SquareWaves:
                        {
                            double amplitudeFactor = 0.01 + 0.2 * GetValue(1, 0.5);
                            double upperHarmonicFalloff = GetValue(2, 0.4);
                            double lowerHarmonicFalloff = GetValue(3, 0.5);
                            double fifthRatio = GetValue(4, 0.6);
                            double dutyCycle = GetValue(5, 0.5);
                            double harmonicDutyCycle = GetValue(6, 0.5);

                            //Fundamental
                            BGC.Audio.Filters.StreamAdder streamAdder = new BGC.Audio.Filters.StreamAdder(
                                new BGC.Audio.Synthesis.SquareWave(amplitudeFactor, freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: dutyCycle));

                            //Upper Harmonics
                            if (upperHarmonicFalloff > 0)
                            {
                                double harmonicFreq = 2 * freq;
                                double harmonicAmplitude = amplitudeFactor * upperHarmonicFalloff;

                                for (int i = 0; i < 5; i++)
                                {
                                    streamAdder.AddStream(
                                        new BGC.Audio.Synthesis.SquareWave(harmonicAmplitude, harmonicFreq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: harmonicDutyCycle));

                                    harmonicFreq *= 2;
                                    harmonicAmplitude *= upperHarmonicFalloff;
                                }
                            }

                            //Lower Harmonics
                            if (lowerHarmonicFalloff > 0)
                            {
                                double harmonicFreq = 0.5 * freq;
                                double harmonicAmplitude = amplitudeFactor * lowerHarmonicFalloff;

                                for (int i = 0; i < 5; i++)
                                {
                                    streamAdder.AddStream(
                                        new BGC.Audio.Synthesis.SquareWave(harmonicAmplitude, harmonicFreq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: harmonicDutyCycle));

                                    harmonicFreq /= 2;
                                    harmonicAmplitude *= lowerHarmonicFalloff;
                                }
                            }

                            //Fifths
                            if (fifthRatio > 0)
                            {
                                double harmonicFreq = 1.5 * freq;
                                double harmonicAmplitude = fifthRatio * amplitudeFactor;

                                for (int i = 0; i < 3; i++)
                                {
                                    streamAdder.AddStream(
                                        new BGC.Audio.Synthesis.SquareWave(harmonicAmplitude, harmonicFreq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: harmonicDutyCycle));

                                    if (upperHarmonicFalloff <= 0)
                                    {
                                        break;
                                    }

                                    harmonicFreq *= 2;
                                    harmonicAmplitude *= upperHarmonicFalloff;
                                }
                            }

                            return streamAdder;
                        }

                    case Instrument.Sawtooth:
                        {
                            double amplitudeFactor = 0.01 + 0.2 * GetValue(1, 0.5);
                            double upperHarmonicFalloff = GetValue(2, 0.5);
                            double lowerHarmonicFalloff = GetValue(3, 0.1);
                            double fifthRatio = GetValue(4, 0.1);

                            //Fundamental
                            BGC.Audio.Filters.StreamAdder streamAdder = new BGC.Audio.Filters.StreamAdder(
                                new BGC.Audio.Synthesis.TriangleWave(amplitudeFactor, freq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0));

                            //Upper Harmonics
                            if (upperHarmonicFalloff > 0)
                            {
                                double harmonicFreq = 2 * freq;
                                double harmonicAmplitude = amplitudeFactor * upperHarmonicFalloff;

                                for (int i = 0; i < 5; i++)
                                {
                                    streamAdder.AddStream(
                                        new BGC.Audio.Synthesis.TriangleWave(harmonicAmplitude, harmonicFreq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0));

                                    harmonicFreq *= 2;
                                    harmonicAmplitude *= upperHarmonicFalloff;
                                }
                            }

                            //Lower Harmonics
                            if (lowerHarmonicFalloff > 0)
                            {
                                double harmonicFreq = 0.5 * freq;
                                double harmonicAmplitude = amplitudeFactor * lowerHarmonicFalloff;

                                for (int i = 0; i < 5; i++)
                                {
                                    streamAdder.AddStream(
                                        new BGC.Audio.Synthesis.TriangleWave(harmonicAmplitude, harmonicFreq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0));

                                    harmonicFreq /= 2;
                                    harmonicAmplitude *= lowerHarmonicFalloff;
                                }
                            }

                            //Fifths
                            if (fifthRatio > 0)
                            {
                                double harmonicFreq = 1.5 * freq;
                                double harmonicAmplitude = fifthRatio * amplitudeFactor;

                                for (int i = 0; i < 3; i++)
                                {
                                    streamAdder.AddStream(
                                        new BGC.Audio.Synthesis.TriangleWave(harmonicAmplitude, harmonicFreq, phase: 2.0 * Math.PI * randomizer.NextDouble(), dutyCycle: 1.0));

                                    if (upperHarmonicFalloff <= 0)
                                    {
                                        break;
                                    }

                                    harmonicFreq *= 2;
                                    harmonicAmplitude *= upperHarmonicFalloff;
                                }
                            }

                            return streamAdder;
                        }

                    default: throw new NotSupportedException($"Flex Instrument Binding does not support {CurrentInstrument}");
                }
            }

            public override void HandleNoteOn(int key)
            {
                rollingStreamAdder?.AddStream(
                    stream: GetNote(key),
                    key);
            }

            public override void HandleNoteOff(int key)
            {
                rollingStreamAdder?.ReleaseNote(key);
            }

            public override void CleanUp()
            {
                if (rollingStreamAdder is not null)
                {
                    rollingStreamAdder.Terminate();
                    rollingStreamAdder.Dispose();
                    rollingStreamAdder = null;
                }
            }

            protected override void ChildDisposal()
            {
                if (rollingStreamAdder is not null)
                {
                    rollingStreamAdder.Terminate();
                    rollingStreamAdder.Dispose();
                    rollingStreamAdder = null;
                }
            }

            public override string ToString() => $"Flex Instrument Binding";
        }

        #endregion Bindings
    }
}
