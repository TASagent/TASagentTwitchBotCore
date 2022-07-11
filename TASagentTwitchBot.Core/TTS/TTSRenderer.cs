using System.Text.Json;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.TTS.Parsing;

namespace TASagentTwitchBot.Core.TTS;

public class TTSRenderer : ITTSRenderer
{
    private readonly ICommunication communication;
    private readonly ISoundEffectSystem soundEffectSystem;
    private readonly ErrorHandler errorHandler;

    private readonly TTSConfiguration ttsConfig;
    private readonly ITTSSystem[] ttsSystems;
    private readonly Dictionary<string, ITTSSystem> voiceLookup = new Dictionary<string, ITTSSystem>();

    public TTSRenderer(
        TTSConfiguration ttsConfig,
        ICommunication communication,
        ErrorHandler errorHandler,
        ISoundEffectSystem soundEffectSystem,
        IEnumerable<ITTSSystem> ttsSystems)
    {
        this.ttsConfig = ttsConfig;
        this.communication = communication;
        this.errorHandler = errorHandler;
        this.soundEffectSystem = soundEffectSystem;

        this.ttsSystems = ttsSystems.ToArray();

        foreach (ITTSSystem system in this.ttsSystems)
        {
            foreach (string voice in system.GetVoices())
            {
                voiceLookup.Add(voice.ToLowerInvariant(), system);
            }
        }
    }

    public Task<bool> SetTTSEnabled(bool enabled)
    {
        if (enabled == ttsConfig.Enabled)
        {
            //Already set
            return Task.FromResult(true);
        }

        ttsConfig.Enabled = enabled;
        return Task.FromResult(true);
    }

    public bool IsTTSVoiceValid(string voice)
    {
        if (voiceLookup.Count == 0)
        {
            //No voices are supported
            return false;
        }

        voice = voice.ToLowerInvariant();

        if (voice == "" || voice == "default" || voice == "unassigned")
        {
            return true;
        }

        return voiceLookup.ContainsKey(voice);
    }

    public TTSVoiceInfo? GetTTSVoiceInfo(string voice)
    {
        if (voiceLookup.Count == 0)
        {
            //No voices are supported
            return null;
        }

        if (!voiceLookup.TryGetValue(voice.ToLowerInvariant(), out ITTSSystem? ttsSystem))
        {
            ttsSystem = ttsSystems[0];
        }

        return ttsSystem.GetTTSVoiceInfo(voice);
    }

    public async Task<AudioRequest?> TTSRequest(
        Commands.AuthorizationLevel authorizationLevel,
        string voice,
        TTSPitch pitch,
        TTSSpeed speed,
        Effect effectsChain,
        string ttsText)
    {
        if (!ttsConfig.Enabled)
        {
            communication.SendDebugMessage($"TTS currently disabled - Rejecting request.");
            return null;
        }

        if (!voiceLookup.TryGetValue(voice.ToLowerInvariant(), out ITTSSystem? ttsSystem))
        {
            ttsSystem = ttsSystems[0];
        }

        TTSVoiceInfo ttsVoiceInfo = ttsSystem.GetTTSVoiceInfo(voice);

        //Make sure Neural Voices are allowed
        if (ttsVoiceInfo.IsNeural && !ttsConfig.CanUseNeuralVoice(authorizationLevel))
        {
            communication.SendWarningMessage($"Neural voice {voice} disallowed.  Changing voice to service default.");
            voice = ttsSystem.GetDefaultVoice();
        }

        TTSSystemRenderer ttsSystemRenderer = ttsSystem.CreateRenderer(voice, pitch, speed, effectsChain);

        try
        {
            return await TTSParser.ParseTTS(ttsText, ttsSystemRenderer, soundEffectSystem);
        }
        catch (Exception ex)
        {
            errorHandler.LogCommandException(ex, $"!tts {ttsText}");
            return null;
        }
    }
}
