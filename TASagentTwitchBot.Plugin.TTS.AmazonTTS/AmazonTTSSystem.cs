using Amazon.Polly;
using System.Text.Json;

using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.TTS;
using TASagentTwitchBot.Core.TTS.Parsing;

namespace TASagentTwitchBot.Plugin.TTS.AmazonTTS;

public abstract class AmazonTTSSystem : ITTSSystem
{
    private IReadOnlyList<string>? voices = null;

    public abstract string SystemName { get; }

    protected void FinalizeInitialization(bool enabled)
    {
        if (voices is not null)
        {
            throw new Exception($"Amazon TTS System already initialized");
        }

        if (enabled)
        {
            List<string> voicesList = new List<string>((int)AmazonTTSVoice.MAX);
            for (AmazonTTSVoice voice = 0; voice < AmazonTTSVoice.MAX; voice++)
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

    public string GetDefaultVoice() => AmazonTTSVoice.en_US_Joanna.Serialize();

    public TTSVoiceInfo GetTTSVoiceInfo(string voiceString)
    {
        AmazonTTSVoice voice = voiceString.SafeTranslateAmazonTTSVoice();
        return new TTSVoiceInfo(voice.Serialize(), voice.IsNeuralVoice());
    }

    public abstract TTSSystemRenderer CreateRenderer(string voice, TTSPitch pitch, TTSSpeed speed, Effect effectsChain);
}

public class AmazonTTSLocalSystem : AmazonTTSSystem
{
    public override string SystemName => "AWS Polly (Local)";

    private readonly Core.ICommunication communication;
    private readonly AmazonPollyClient? amazonClient;

    public AmazonTTSLocalSystem(
        Core.ICommunication communication)
    {
        this.communication = communication;

        try
        {
            string awsCredentialsPath = BGC.IO.DataManagement.PathForDataFile("Config", "awsPollyCredentials.json");

            if (!File.Exists(awsCredentialsPath))
            {
                throw new FileNotFoundException($"Could not find credentials for AWS Polly at {awsCredentialsPath}");
            }

            AWSPollyCredentials awsPolyCredentials = JsonSerializer.Deserialize<AWSPollyCredentials>(File.ReadAllText(awsCredentialsPath))!;

            Amazon.Runtime.BasicAWSCredentials awsCredentials = new Amazon.Runtime.BasicAWSCredentials(
                awsPolyCredentials.AccessKey,
                awsPolyCredentials.SecretKey);

            amazonClient = new AmazonPollyClient(awsCredentials, Amazon.RegionEndpoint.USWest2);

            FinalizeInitialization(true);
        }
        catch (Exception ex)
        {
            communication.SendErrorMessage($"Error initializing Local Amazon TTS, Disabling system: {ex.Message}");
            amazonClient = null;

            FinalizeInitialization(false);
        }
    }

    public override TTSSystemRenderer CreateRenderer(
        string voice,
        TTSPitch pitch,
        TTSSpeed speed,
        Effect effectsChain)
    {
        return new AmazonTTSLocalRenderer(
            amazonClient: amazonClient!,
            communication: communication,
            voice: voice.SafeTranslateAmazonTTSVoice(),
            pitch: pitch,
            speed: speed,
            effectsChain: effectsChain);

    }
}

public class AmazonTTSWebSystem : AmazonTTSSystem
{
    private readonly TTSWebRequestHandler ttsWebRequestHandler;
    private readonly Core.ICommunication communication;

    public override string SystemName => "AWS Polly (Web)";

    public AmazonTTSWebSystem(
        TTSWebRequestHandler ttsWebRequestHandler,
        Core.ICommunication communication)
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
        return new AmazonTTSWebRenderer(
            ttsWebRequestHandler: ttsWebRequestHandler,
            communication: communication,
            voice: voice.SafeTranslateAmazonTTSVoice(),
            pitch: pitch,
            speed: speed,
            effectsChain: effectsChain);
    }
}
