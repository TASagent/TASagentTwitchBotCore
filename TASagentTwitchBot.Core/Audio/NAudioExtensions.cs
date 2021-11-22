using NAudio.Wave;
using BGC.Audio;
using BGC.Audio.NAudio;

namespace TASagentTwitchBot.Core.Audio;

public static class NAudioExtensions
{

    public static ISampleProvider ApplyEffects(this IWaveProvider waveProvider, Effects.Effect effectsChain)
    {
        if (effectsChain is null || effectsChain is Effects.NoEffect)
        {
            //Bypass conversion
            return waveProvider.ToSampleProvider();
        }

        return effectsChain.ApplyEffects(waveProvider.ToBGCStream().EnsureMono()).LimitStream().ToSampleProvider();
    }

    public static ISampleProvider ApplyMicrophoneEffects(
        this IBGCStream inputStream,
        Config.MicConfiguration micConfig,
        Effects.Effect? effectsChain)
    {
        IBGCStream incomingStream = inputStream.EnsureMono();

        if (micConfig.NoiseGateConfiguration.Enabled)
        {
            incomingStream = incomingStream.NoiseGateV2(
                openThreshold: micConfig.NoiseGateConfiguration.OpenThreshold,
                closeThreshold: micConfig.NoiseGateConfiguration.CloseThreshold,
                attackDuration: micConfig.NoiseGateConfiguration.AttackDuration,
                holdDuration: micConfig.NoiseGateConfiguration.HoldDuration,
                releaseDuration: micConfig.NoiseGateConfiguration.ReleaseDuration);
        }

        if (micConfig.ExpanderConfiguration.Enabled)
        {
            incomingStream = incomingStream.Expander(
                ratio: micConfig.ExpanderConfiguration.Ratio,
                threshold: micConfig.ExpanderConfiguration.Threshold,
                attackDuration: micConfig.ExpanderConfiguration.AttackDuration,
                releaseDuration: micConfig.ExpanderConfiguration.ReleaseDuration,
                outputGain: micConfig.ExpanderConfiguration.OutputGain);
        }

        if (micConfig.CompressorConfiguration.Enabled)
        {
            incomingStream = incomingStream.Compressor(
                ratio: micConfig.CompressorConfiguration.Ratio,
                threshold: micConfig.CompressorConfiguration.Threshold,
                attackDuration: micConfig.CompressorConfiguration.AttackDuration,
                releaseDuration: micConfig.CompressorConfiguration.ReleaseDuration,
                outputGain: micConfig.CompressorConfiguration.OutputGain);

        }

        if (effectsChain is null || effectsChain is Effects.NoEffect)
        {
            //Do Nothing
        }
        else
        {
            incomingStream = effectsChain.ApplyEffects(incomingStream);
        }

        return incomingStream.LimitStream().ToBufferedSampleProvider(256);
    }

    public static IBGCStream ApplyMicrophoneConfigs(
        this IBGCStream inputStream,
        Config.MicConfiguration micConfig)
    {
        IBGCStream incomingStream = inputStream.EnsureMono();

        if (micConfig.NoiseGateConfiguration.Enabled)
        {
            incomingStream = incomingStream.NoiseGateV2(
                openThreshold: micConfig.NoiseGateConfiguration.OpenThreshold,
                closeThreshold: micConfig.NoiseGateConfiguration.CloseThreshold,
                attackDuration: micConfig.NoiseGateConfiguration.AttackDuration,
                holdDuration: micConfig.NoiseGateConfiguration.HoldDuration,
                releaseDuration: micConfig.NoiseGateConfiguration.ReleaseDuration);
        }

        if (micConfig.ExpanderConfiguration.Enabled)
        {
            incomingStream = incomingStream.Expander(
                ratio: micConfig.ExpanderConfiguration.Ratio,
                threshold: micConfig.ExpanderConfiguration.Threshold,
                attackDuration: micConfig.ExpanderConfiguration.AttackDuration,
                releaseDuration: micConfig.ExpanderConfiguration.ReleaseDuration,
                outputGain: micConfig.ExpanderConfiguration.OutputGain);
        }

        if (micConfig.CompressorConfiguration.Enabled)
        {
            incomingStream = incomingStream.Compressor(
                ratio: micConfig.CompressorConfiguration.Ratio,
                threshold: micConfig.CompressorConfiguration.Threshold,
                attackDuration: micConfig.CompressorConfiguration.AttackDuration,
                releaseDuration: micConfig.CompressorConfiguration.ReleaseDuration,
                outputGain: micConfig.CompressorConfiguration.OutputGain);

        }

        return incomingStream;
    }

    public static ISampleProvider ApplyMicrophoneEffects(
        this IWaveProvider waveProvider,
        Config.MicConfiguration micConfig,
        Effects.Effect effectsChain)
    {
        IBGCStream incomingStream = waveProvider
            .ToBGCStream()
            .EnsureMono();

        if (micConfig.NoiseGateConfiguration.Enabled)
        {
            incomingStream = incomingStream.NoiseGateV2(
                openThreshold: micConfig.NoiseGateConfiguration.OpenThreshold,
                closeThreshold: micConfig.NoiseGateConfiguration.CloseThreshold,
                attackDuration: micConfig.NoiseGateConfiguration.AttackDuration,
                holdDuration: micConfig.NoiseGateConfiguration.HoldDuration,
                releaseDuration: micConfig.NoiseGateConfiguration.ReleaseDuration);
        }

        if (micConfig.ExpanderConfiguration.Enabled)
        {
            incomingStream = incomingStream.Expander(
                ratio: micConfig.ExpanderConfiguration.Ratio,
                threshold: micConfig.ExpanderConfiguration.Threshold,
                attackDuration: micConfig.ExpanderConfiguration.AttackDuration,
                releaseDuration: micConfig.ExpanderConfiguration.ReleaseDuration,
                outputGain: micConfig.ExpanderConfiguration.OutputGain);
        }

        if (micConfig.CompressorConfiguration.Enabled)
        {
            incomingStream = incomingStream.Compressor(
                ratio: micConfig.CompressorConfiguration.Ratio,
                threshold: micConfig.CompressorConfiguration.Threshold,
                attackDuration: micConfig.CompressorConfiguration.AttackDuration,
                releaseDuration: micConfig.CompressorConfiguration.ReleaseDuration,
                outputGain: micConfig.CompressorConfiguration.OutputGain);

        }

        incomingStream = incomingStream.LimitStream();

        if (effectsChain is null || effectsChain is Effects.NoEffect)
        {
            //Do Nothing
        }
        else
        {
            incomingStream = effectsChain.ApplyEffects(incomingStream).LimitStream();
        }

        return incomingStream.ToSampleProvider();
    }

    public static IBGCStream Spatialize(this IBGCStream stream, double angle) => SoundEffectSystem.Spatialize(stream, angle);
}
