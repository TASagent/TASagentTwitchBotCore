using Google.Cloud.TextToSpeech.V1;

using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.TTS;
using TASagentTwitchBot.Core.TTS.Parsing;

namespace TASagentTwitchBot.Plugin.TTS.GoogleTTS;

public abstract class GoogleTTSSystem : ITTSSystem
{
    private IReadOnlyList<string>? voices = null;

    public abstract string SystemName { get; }

    protected void FinalizeInitialization(bool enabled)
    {
        if (voices is not null)
        {
            throw new Exception($"Google TTS System already initialized");
        }

        if (enabled)
        {
            List<string> voicesList = new List<string>((int)GoogleTTSVoice.MAX);
            for (GoogleTTSVoice voice = 0; voice < GoogleTTSVoice.MAX; voice++)
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

    public string GetDefaultVoice() => GoogleTTSVoice.en_US_Standard_B.Serialize();

    public TTSVoiceInfo GetTTSVoiceInfo(string voiceString)
    {
        GoogleTTSVoice voice = voiceString.SafeTranslateGoogleTTSVoice();
        return new TTSVoiceInfo(voice.Serialize(), voice.IsNeuralVoice());
    }

    public abstract TTSSystemRenderer CreateRenderer(string voice, TTSPitch pitch, TTSSpeed speed, Effect effectsChain);
}

public class GoogleTTSLocalSystem : GoogleTTSSystem
{
    private readonly ICommunication communication;

    private readonly TextToSpeechClient? googleClient;

    public override string SystemName => "Google TTS (Local)";

    public GoogleTTSLocalSystem(
        ICommunication communication)
    {
        this.communication = communication;

        try
        {
            TextToSpeechClientBuilder builder = new TextToSpeechClientBuilder();

            string googleCredentialsPath = BGC.IO.DataManagement.PathForDataFile("Config", "googleCloudCredentials.json");

            if (!File.Exists(googleCredentialsPath))
            {
                throw new FileNotFoundException($"Could not find credentials for Google TTS at {googleCredentialsPath}");
            }

            builder.CredentialsPath = googleCredentialsPath;
            googleClient = builder.Build();

            FinalizeInitialization(true);
        }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"Error initializing Local Google TTS, Disabling system: {ex.Message}");
            googleClient = null;

            FinalizeInitialization(false);
        }
    }

    public override TTSSystemRenderer CreateRenderer(
        string voice,
        TTSPitch pitch,
        TTSSpeed speed,
        Effect effectsChain)
    {
        return new GoogleTTSLocalRenderer(
            googleClient: googleClient!,
            communication: communication,
            voice: voice.SafeTranslateGoogleTTSVoice(),
            pitch: pitch,
            speed: speed,
            effectsChain: effectsChain);
    }
}

public class GoogleTTSWebSystem : GoogleTTSSystem
{
    private readonly TTSWebRequestHandler ttsWebRequestHandler;
    private readonly ICommunication communication;

    public override string SystemName => "Google TTS (Web)";

    public GoogleTTSWebSystem(
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
        return new GoogleTTSWebRenderer(
            ttsWebRequestHandler: ttsWebRequestHandler,
            communication: communication,
            voice: voice.SafeTranslateGoogleTTSVoice(),
            pitch: pitch,
            speed: speed,
            effectsChain: effectsChain);
    }
}
