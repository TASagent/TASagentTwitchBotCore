using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using BGC.Audio;
using BGC.Mathematics;

namespace TASagentTwitchBot.Core.Audio
{
    public class BufferedWasapiQueuer : IBGCStream, IDisposable
    {
        public int Channels { get; }

        public int TotalSamples => int.MaxValue;
        public int ChannelSamples => int.MaxValue;

        public float SamplingRate { get; }

        private readonly WasapiCapture capture;
        private readonly int maxQueuedSamples;
        private readonly WaveFormatEncoding encoding;
        private readonly int bytesPerSample;

        private readonly ConcurrentQueue<AudioFrame> preparedFrames = new ConcurrentQueue<AudioFrame>();
        private readonly ConcurrentQueue<AudioFrame> pooledFrames = new ConcurrentQueue<AudioFrame>();

        private readonly SemaphoreSlim readSemaphore = new SemaphoreSlim(0);

        private AudioFrame currentFrame = null;
        private bool finished = false;
        private int queuedSamples = 0;

        private bool disposedValue;

        private int poppedFrames = 0;
        private int popPrintThreshold = 1;

        public BufferedWasapiQueuer(
            MMDevice device,
            double maxDelayMs)
        {
            capture = new WasapiCapture(device, true, 10);

            capture.DataAvailable += CaptureOnDataAvailable;
            capture.RecordingStopped += CaptureOnRecordingStopped;
            
            SamplingRate = capture.WaveFormat.SampleRate;
            Channels = capture.WaveFormat.Channels;

            encoding = capture.WaveFormat.Encoding;
            bytesPerSample = capture.WaveFormat.BitsPerSample / 8;

            maxQueuedSamples = Channels * (int)Math.Round(maxDelayMs * SamplingRate / 1000.0);

            capture.StartRecording();
        }

        private void CaptureOnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded > 0)
            {
                //Handle new audio frames
                AudioFrame newFrame = null;

                if (queuedSamples > maxQueuedSamples && preparedFrames.Count > 3)
                {
                    if (poppedFrames++ > popPrintThreshold)
                    {
                        BGC.Debug.LogWarning($"Discarded {popPrintThreshold} total audio frames.");
                        popPrintThreshold *= 2;
                    }
                    //Pop queued frame because we're behind

                    //Keep readSemaphore synced with count
                    readSemaphore.Wait();

                    if (preparedFrames.TryDequeue(out newFrame))
                    {
                        queuedSamples -= newFrame.SampleCount;
                    }
                }
                
                if (newFrame is null)
                {
                    //Not too many queued frames, or failed to get queued frame
                    if (!pooledFrames.TryDequeue(out newFrame))
                    {
                        //No pooled frames
                        //Create frame
                        newFrame = new AudioFrame(e.BytesRecorded, encoding, bytesPerSample);
                    }
                }

                newFrame.SetData(e.Buffer, e.BytesRecorded);

                preparedFrames.Enqueue(newFrame);
                queuedSamples += newFrame.SampleCount;

                //Signal a frame is ready
                readSemaphore.Release(1);
            }
        }

        public void StopRecording() => capture.StopRecording();

        private void CaptureOnRecordingStopped(object sender, StoppedEventArgs e)
        {
            //Simply marking an extra release is enough to handle the exit condition
            readSemaphore.Release(1);
        }

        public void Initialize() { }

        public int Read(float[] data, int offset, int count)
        {
            int samplesRead = 0;
            if (currentFrame is not null)
            {
                samplesRead = currentFrame.Read(data, offset, count);

                if (currentFrame.IsComplete)
                {
                    pooledFrames.Enqueue(currentFrame);
                    currentFrame = null;
                }
            }

            if (finished)
            {
                return samplesRead;
            }

            while (samplesRead < count)
            {
                //Wait for frame
                readSemaphore.Wait();

                if (!preparedFrames.TryDequeue(out currentFrame))
                {
                    //when semaphore trips and there are no more frames, we are done
                    finished = true;
                    return samplesRead;
                }

                queuedSamples -= currentFrame.SampleCount;
                samplesRead += currentFrame.Read(data, offset + samplesRead, count - samplesRead);

                if (currentFrame.IsComplete)
                {
                    pooledFrames.Enqueue(currentFrame);
                    currentFrame = null;
                }
            }

            return samplesRead;
        }

        public void Reset() => throw new NotSupportedException();

        public void Seek(int position) => throw new NotSupportedException();

        public IEnumerable<double> GetChannelRMS() => throw new NotSupportedException();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    capture.Dispose();
                    readSemaphore.Dispose();
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

        private class AudioFrame
        {
            private const int MIN_BUFFER_SIZE = 512;
            private const float TO_FLOAT_FACTOR = 1f / 32767f;

            private readonly WaveFormatEncoding encoding;
            private readonly int bytesPerSample;

            private float[] data;
            private int sampleCount = 0;
            private int currentSample = 0;

            public int SampleCount => sampleCount;
            public bool IsComplete => sampleCount == currentSample;

            public AudioFrame(int size, WaveFormatEncoding encoding, int bytesPerSample)
            {
                size = Math.Max(size.CeilingToPowerOfTwo(), MIN_BUFFER_SIZE);

                data = new float[size];

                this.encoding = encoding;
                this.bytesPerSample = bytesPerSample;
            }

            public void SetData(byte[] buffer, int bytesRecorded)
            {
                currentSample = 0;
                sampleCount = bytesRecorded / bytesPerSample;
                if (sampleCount > data.Length)
                {
                    //Resize
                    data = new float[sampleCount.CeilingToPowerOfTwo()];
                }

                switch (encoding)
                {
                    case WaveFormatEncoding.Pcm:
                        for (int sample = 0; sample < sampleCount; sample++)
                        {
                            data[sample] = TO_FLOAT_FACTOR * BitConverter.ToInt16(buffer, bytesPerSample * sample);
                        }
                        break;

                    case WaveFormatEncoding.IeeeFloat:
                        for (int sample = 0; sample < sampleCount; sample++)
                        {
                            data[sample] = BitConverter.ToSingle(buffer, bytesPerSample * sample);
                        }
                        break;

                    default:
                        throw new NotSupportedException();
                }

            }


            public int Read(float[] data, int offset, int count)
            {
                int readSamples = Math.Min(count, sampleCount - currentSample);

                Array.Copy(
                    sourceArray: this.data,
                    sourceIndex: currentSample,
                    destinationArray: data,
                    destinationIndex: offset,
                    length: readSamples);

                currentSample += readSamples;

                return readSamples;
            }
        }

    }
}
