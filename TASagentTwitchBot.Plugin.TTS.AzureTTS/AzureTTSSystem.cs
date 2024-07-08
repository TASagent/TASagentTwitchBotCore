using System.Text.Json;
using Microsoft.CognitiveServices.Speech;

using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.TTS;
using TASagentTwitchBot.Core.TTS.Parsing;

namespace TASagentTwitchBot.Plugin.TTS.AzureTTS;

public abstract class AzureTTSSystem : ITTSSystem
{
    private IReadOnlyList<string>? voices = null;

    public abstract string SystemName { get; }

    protected void FinalizeInitialization(bool enabled)
    {
        if (voices is not null)
        {
            throw new Exception($"Azure TTS System already initialized");
        }

        if (enabled)
        {
            List<string> voicesList = new List<string>((int)AzureTTSVoice.MAX);
            for (AzureTTSVoice voice = 0; voice < AzureTTSVoice.MAX; voice++)
            {
                voicesList.Add(voice.Serialize());
            }

            voices = voicesList;
        }
        else
        {
            voices = Array.Empty<string>();
        }
    }

    public IEnumerable<string> GetVoices() => voices ?? throw new Exception($"GetVoices called before FinalizeInitialization");

    public string GetDefaultVoice() => AzureTTSVoice.en_US_GuyNeural.Serialize();

    public TTSVoiceInfo GetTTSVoiceInfo(string voiceString)
    {
        AzureTTSVoice voice = voiceString.SafeTranslateAzureTTSVoice();
        return new TTSVoiceInfo(voice.Serialize(), voice.IsNeuralVoice());
    }

    public abstract TTSSystemRenderer CreateRenderer(string voice, TTSPitch pitch, TTSSpeed speed, Effect effectsChain);
}

public class AzureTTSLocalSystem : AzureTTSSystem
{
    private readonly ICommunication communication;
    private readonly SpeechConfig? azureClient;

    public override string SystemName => "Azure Speech Synthesis (Local)";

    public AzureTTSLocalSystem(
        ICommunication communication)
    {
        this.communication = communication;

        try
        {
            string azureCredentialsPath = BGC.IO.DataManagement.PathForDataFile("Config", "azureSpeechSynthesisCredentials.json");

            if (!File.Exists(azureCredentialsPath))
            {
                throw new FileNotFoundException($"Could not find credentials for Azure SpeechSynthesis at {azureCredentialsPath}");
            }

            AzureSpeechSynthesisCredentials azureCredentials = JsonSerializer.Deserialize<AzureSpeechSynthesisCredentials>(File.ReadAllText(azureCredentialsPath))!;

            azureClient = SpeechConfig.FromSubscription(azureCredentials.AccessKey, azureCredentials.Region);
            azureClient.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio24Khz48KBitRateMonoMp3);

            FinalizeInitialization(true);
        }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"Error initializing Local Azure TTS, Disabling system: {ex.Message}");
            azureClient = null;

            FinalizeInitialization(false);
        }
    }

    public override TTSSystemRenderer CreateRenderer(
        string voice,
        TTSPitch pitch,
        TTSSpeed speed,
        Effect effectsChain)
    {
        return new AzureTTSLocalRenderer(
            azureClient: azureClient!,
            communication: communication,
            voice: voice.SafeTranslateAzureTTSVoice(),
            pitch: pitch,
            speed: speed,
            effectsChain: effectsChain);
    }
}

public class AzureTTSWebSystem : AzureTTSSystem
{
    private readonly TTSWebRequestHandler ttsWebRequestHandler;
    private readonly ICommunication communication;

    public override string SystemName => "Azure Speech Synthesis (Web)";

    public AzureTTSWebSystem(
        TTSWebRequestHandler ttsWebRequestHandler,
        ICommunication communication)
    {
        this.ttsWebRequestHandler = ttsWebRequestHandler;
        this.communication = communication;

        FinalizeInitialization(true);
    }


    public override TTSSystemRenderer CreateRenderer(
        string voice,
        TTSPitch pitch,
        TTSSpeed speed,
        Effect effectsChain)
    {
        return new AzureTTSWebRenderer(
            ttsWebRequestHandler: ttsWebRequestHandler,
            communication: communication,
            voice: voice.SafeTranslateAzureTTSVoice(),
            pitch: pitch,
            speed: speed,
            effectsChain: effectsChain);
    }
}
