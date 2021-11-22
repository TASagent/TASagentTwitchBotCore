using BGC.Audio.Filters;

namespace BGC.Audio.Midi;

/// <summary>
/// Additive Synthesis - Output samples are equal to the linear sum of input samples.
/// </summary>
public class RollingStreamAdder : BGCFilter
{
    private readonly List<DecayableNote> streams = new List<DecayableNote>();
    public override IEnumerable<IBGCStream> InternalStreams => streams;

    private readonly List<DecayableNote> pendingNewStreams = new List<DecayableNote>();

    public override int Channels { get; }

    public override int TotalSamples => int.MaxValue;
    public override int ChannelSamples => int.MaxValue;

    public override float SamplingRate { get; }

    private const int BUFFER_SIZE = 512;
    private readonly float[] buffer = new float[BUFFER_SIZE];

    private bool terminated = false;

    public RollingStreamAdder(
        int channels = 1,
        float samplingRate = 44100f)
    {
        Channels = channels;
        SamplingRate = samplingRate;
    }

    public void AddStream(IBGCStream stream, int key)
    {
        if (stream.Channels != Channels)
        {
            if (stream.Channels > Channels)
            {
                stream = stream.IsolateChannel(0);
            }
            else
            {
                stream = stream.UpChannel(2);
            }
        }

        pendingNewStreams.Add(new DecayableNote(stream, key));
    }

    public void ReleaseNote(int key)
    {
        foreach (DecayableNote stream in streams.Where(x => !x.Decaying && x.Key == key).ToArray())
        {
            stream.TriggerDecay();
        }

        foreach (DecayableNote stream in pendingNewStreams.Where(x => !x.Decaying && x.Key == key).ToArray())
        {
            stream.TriggerDecay();
        }
    }

    public void Terminate() => terminated = true;

    public override void Reset()
    {
        terminated = false;
        pendingNewStreams.Clear();
        streams.Clear();
    }

    public override int Read(float[] data, int offset, int count)
    {
        if (terminated)
        {
            return 0;
        }

        streams.AddRange(pendingNewStreams);
        pendingNewStreams.Clear();

        Array.Clear(data, offset, count);

        //foreach (IBGCStream stream in streams)
        for (int i = streams.Count - 1; i >= 0; i--)
        {
            int streamRemainingSamples = count;
            int streamOffset = offset;

            while (streamRemainingSamples > 0)
            {
                int maxRead = Math.Min(BUFFER_SIZE, streamRemainingSamples);
                int streamReadSamples = streams[i].Read(buffer, 0, maxRead);

                if (streamReadSamples == 0)
                {
                    //Done with this stream
                    //Remove it
                    streams[i].Dispose();
                    streams.RemoveAt(i);
                    break;
                }

                for (int j = 0; j < streamReadSamples; j++)
                {
                    data[streamOffset + j] += buffer[j];
                }

                streamOffset += streamReadSamples;
                streamRemainingSamples -= streamReadSamples;
            }
        }

        return count;
    }

    public override void Seek(int position) => Reset();

    public override IEnumerable<double> GetChannelRMS() => throw new NotSupportedException();

    public class DecayableNote : SimpleBGCFilter
    {
        public int Key { get; init; }

        public override int Channels => stream.Channels;
        public override int TotalSamples => stream.TotalSamples;
        public override int ChannelSamples => stream.ChannelSamples;

        private readonly float attackRate;
        private readonly float releaseRate;
        private readonly float minAttenuation;
        private float attenuation = 0f;
        private bool pendingDecay = false;

        private Phase phase = Phase.Attack;
        public bool Decaying => phase == Phase.Decay;

        private enum Phase
        {
            Attack = 0,
            Steady,
            Decay
        }

        public DecayableNote(
            IBGCStream stream,
            int key,
            double attackDuration = 0.025,
            double releaseDuration = 0.150)
            : base(stream)
        {
            Key = key;

            attackRate = (float)(1.0 / (attackDuration * SamplingRate));
            releaseRate = (float)(1.0 / (releaseDuration * SamplingRate));
            minAttenuation = (float)Math.Pow(10.0, -80.0 / 20.0);

            phase = Phase.Attack;
            pendingDecay = false;
            attenuation = 0f;
        }

        public void TriggerDecay() => pendingDecay = true;

        public override void Reset()
        {
            base.Reset();
            attenuation = 0f;
            phase = Phase.Attack;
            pendingDecay = false;
        }

        public override int Read(float[] data, int offset, int count)
        {
            if (pendingDecay)
            {
                phase = Phase.Decay;
            }
            else if (attenuation == 1f && phase == Phase.Attack)
            {
                phase = Phase.Steady;
            }

            switch (phase)
            {
                case Phase.Attack:
                    {
                        int samplesRead = stream.Read(data, offset, count);

                        for (int i = 0; i < samplesRead; i++)
                        {
                            for (int c = 0; c < Channels; c++)
                            {
                                data[offset + i + c] *= attenuation;
                            }

                            attenuation = Math.Min(1f, attenuation + attackRate);

                            i += Channels - 1;
                        }

                        return samplesRead;
                    }

                case Phase.Steady:
                    return stream.Read(data, offset, count);

                case Phase.Decay:
                    {
                        if (attenuation < minAttenuation)
                        {
                            return 0;
                        }


                        int samplesRead = stream.Read(data, offset, count);

                        for (int i = 0; i < samplesRead; i++)
                        {
                            for (int c = 0; c < Channels; c++)
                            {
                                data[offset + i + c] *= attenuation;
                            }

                            attenuation = Math.Max(0, attenuation - releaseRate);

                            i += Channels - 1;
                        }

                        return samplesRead;
                    }

                default:
                    throw new NotSupportedException();
            }
        }

        public override IEnumerable<double> GetChannelRMS() => throw new NotSupportedException();
    }
}
