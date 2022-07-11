using Amazon.Polly;
using Amazon.Polly.Model;

using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.TTS;
using TASagentTwitchBot.Core.TTS.Parsing;

namespace TASagentTwitchBot.Plugin.TTS.AmazonTTS;


public abstract class AmazonTTSRenderer : StandardTTSSystemRenderer
{
    protected readonly AmazonTTSVoice amazonVoice;

    public AmazonTTSRenderer(
        ICommunication? communication,
        ILogger? logger,
        AmazonTTSVoice voice,
        TTSPitch pitch,
        TTSSpeed speed,
        Effect effectsChain)
        : base(
              communication: communication,
              logger: logger,
              voice: voice.Serialize(),
              pitch: pitch,
              speed: speed,
              effectsChain: effectsChain)
    {
        amazonVoice = voice;
    }


    protected override string GetModeMarkup(TTSRenderMode mode, bool start)
    {
        if (amazonVoice.IsNeuralVoice())
        {
            switch (mode)
            {
                case TTSRenderMode.Whisper:
                case TTSRenderMode.Emphasis:
                    return "";

                case TTSRenderMode.Censor:
                    return start ? "<say-as interpret-as=\"expletive\">" : "</say-as>";

                case TTSRenderMode.Normal:
                default:
                    throw new Exception($"Unsupported RenderMode for Markup: {mode}");
            }
        }
        else
        {
            switch (mode)
            {
                case TTSRenderMode.Whisper:
                    return start ? "<amazon:effect name=\"whispered\">" : "</amazon:effect>";

                case TTSRenderMode.Emphasis:
                    return start ? "<emphasis level=\"strong\">" : "</emphasis>";

                case TTSRenderMode.Censor:
                    return start ? "<say-as interpret-as=\"expletive\">" : "</say-as>";

                case TTSRenderMode.Normal:
                default:
                    throw new Exception($"Unsupported RenderMode for Markup: {mode}");
            }
        }

    }

    protected override string FinalizeSSML(string interiorSSML)
    {
        if (amazonVoice.IsNeuralVoice())
        {
            if (speed != TTSSpeed.Medium)
            {
                interiorSSML = $"<prosody rate=\"{speed.GetSpeedValue()}\">{interiorSSML}</prosody>";
            }
        }
        else
        {
            if (pitch != TTSPitch.Medium || speed != TTSSpeed.Medium)
            {
                interiorSSML = $"<prosody pitch=\"{pitch.GetPitchShift()}\" rate=\"{speed.GetSpeedValue()}\">{interiorSSML}</prosody>";
            }
        }

        if (amazonVoice.GetRequiresLangTag())
        {
            interiorSSML = $"<lang xml:lang=\"en-US\">{interiorSSML}</lang>";
        }

        return $"<speak>{interiorSSML}</speak>";
    }

    protected override string PrepareText(string text) => SanitizeXML(text);
}

public class AmazonTTSLocalRenderer : AmazonTTSRenderer
{
    private readonly AmazonPollyClient amazonClient;

    public AmazonTTSLocalRenderer(
        AmazonPollyClient amazonClient,
        ICommunication? communication,
        AmazonTTSVoice voice,
        TTSPitch pitch,
        TTSSpeed speed,
        Effect effectsChain)
        : base(
              communication: communication,
              logger: null,
              voice: voice,
              pitch: pitch,
              speed: speed,
              effectsChain: effectsChain)
    {
        this.amazonClient = amazonClient;
    }

    public AmazonTTSLocalRenderer(
        AmazonPollyClient amazonClient,
        ILogger? logger,
        AmazonTTSVoice voice,
        TTSPitch pitch,
        TTSSpeed speed,
        Effect effectsChain)
        : base(
              communication: null,
              logger: logger,
              voice: voice,
              pitch: pitch,
              speed: speed,
              effectsChain: effectsChain)
    {
        this.amazonClient = amazonClient;
    }

    public override async Task<string?> SynthesizeSpeech(string finalSSML)
    {
        SynthesizeSpeechRequest synthesisRequest = amazonVoice.GetAmazonTTSSpeechRequest();
        synthesisRequest.TextType = TextType.Ssml;
        synthesisRequest.Text = finalSSML;

        //communication.SendDebugMessage(synthesisRequest.Text);

        // Perform the Text-to-Speech request, passing the text input
        // with the selected voice parameters and audio file type
        SynthesizeSpeechResponse synthesisResponse = await amazonClient.SynthesizeSpeechAsync(synthesisRequest);

        // Write the binary AudioContent of the response to file.
        string filepath = Path.Combine(TTSFilesPath, $"{Guid.NewGuid()}.mp3");

        using (Stream file = new FileStream(filepath, FileMode.Create))
        {
            await synthesisResponse.AudioStream.CopyToAsync(file);
            await file.FlushAsync();
            file.Close();
        }

        return filepath;
    }
}

public class AmazonTTSWebRenderer : AmazonTTSRenderer
{
    private readonly TTSWebRequestHandler ttsWebRequestHandler;

    public AmazonTTSWebRenderer(
        TTSWebRequestHandler ttsWebRequestHandler,
        ICommunication? communication,
        AmazonTTSVoice voice,
        TTSPitch pitch,
        TTSSpeed speed,
        Effect effectsChain)
        : base(
              communication: communication,
              logger: null,
              voice: voice,
              pitch: pitch,
              speed: speed,
              effectsChain: effectsChain)
    {
        this.ttsWebRequestHandler = ttsWebRequestHandler;
    }

    public override Task<string?> SynthesizeSpeech(string finalSSML)
    {
        return ttsWebRequestHandler.SubmitTTSWebRequest(new ServerTTSRequest(
            RequestIdentifier: Guid.NewGuid().ToString(),
            Ssml: finalSSML,
            Voice: voice,
            Pitch: pitch,
            Speed: speed));
    }
}
