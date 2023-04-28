using Microsoft.CognitiveServices.Speech;

using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.TTS;
using TASagentTwitchBot.Core.TTS.Parsing;

namespace TASagentTwitchBot.Plugin.TTS.AzureTTS;


public abstract class AzureTTSRenderer : StandardTTSSystemRenderer
{
    protected readonly AzureTTSVoice azureVoice;

    public AzureTTSRenderer(
        ICommunication? communication,
        ILogger? logger,
        AzureTTSVoice voice,
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
        azureVoice = voice;
    }

    protected override string GetModeMarkup(TTSRenderMode mode, bool start)
    {
        switch (mode)
        {
            case TTSRenderMode.Whisper:
                return start ? "<mstts:express-as style=\"whispering\">" : "</mstts:express-as>";

            case TTSRenderMode.Emphasis:
                return start ? "<emphasis level=\"strong\">" : "</emphasis>";

            case TTSRenderMode.Censor:
                return start ? "<say-as interpret-as=\"expletive\">" : "</say-as>";

            case TTSRenderMode.Normal:
            default:
                throw new Exception($"Unsupported RenderMode for Markup: {mode}");
        }
    }

    protected override string PrepareText(string text) => SanitizeXML(text);

    protected override string FinalizeSSML(string interiorSSML)
    {
        if (pitch != TTSPitch.Medium || speed != TTSSpeed.Medium)
        {
            interiorSSML = $"<prosody pitch=\"{pitch.GetPitchShift()}\" rate=\"{speed.GetSpeedValue()}\">{interiorSSML}</prosody>";
        }

        return $"<speak version=\"1.0\" xml:lang=\"en-US\" xmlns:mstts=\"http://www.w3.org/2001/mstts\">" +
            $"<voice name=\"{azureVoice.GetTTSVoiceString()}\">" +
            $"<mstts:silence type=\"Sentenceboundary\" value=\"250ms\"/>" +
            $"<mstts:silence type=\"Tailing\" value=\"0ms\"/>" +
            $"{interiorSSML}" +
            $"</voice></speak>";
    }

}

public class AzureTTSLocalRenderer : AzureTTSRenderer
{
    private readonly SpeechConfig azureClient;

    public AzureTTSLocalRenderer(
        SpeechConfig azureClient,
        ICommunication? communication,
        AzureTTSVoice voice,
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
        this.azureClient = azureClient;
    }

    public AzureTTSLocalRenderer(
        SpeechConfig azureClient,
        ILogger? logger,
        AzureTTSVoice voice,
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
        this.azureClient = azureClient;
    }

    public override async Task<string?> SynthesizeSpeech(string finalSSML)
    {
        // Write the binary AudioContent of the response to file.
        string filepath = Path.Combine(TTSFilesPath, $"{Guid.NewGuid()}.mp3");

        using SpeechSynthesizer synthesizer = new SpeechSynthesizer(azureClient, null);
        using SpeechSynthesisResult result = await synthesizer.SpeakSsmlAsync(finalSSML);

        if (result.Reason == ResultReason.Canceled)
        {
            SpeechSynthesisCancellationDetails details = SpeechSynthesisCancellationDetails.FromResult(result);

            if (details.ErrorCode == CancellationErrorCode.TooManyRequests)
            {
                //Retry logic
                int delay = 1000;

                while (details.ErrorCode == CancellationErrorCode.TooManyRequests && delay < 64000)
                {
                    communication?.SendWarningMessage($"Azure TTS returned TooManyRequests. Waiting {delay}: {details.ErrorDetails}");
                    logger?.LogWarning("Azure TTS returned TooManyRequests. Waiting {Delay}: {ErrorDetails}", delay, details.ErrorDetails);

                    await Task.Delay(delay);
                    using SpeechSynthesisResult retryResult = await synthesizer.SpeakSsmlAsync(finalSSML);
                    details = SpeechSynthesisCancellationDetails.FromResult(retryResult);

                    if (details.ErrorCode == CancellationErrorCode.NoError)
                    {
                        //Success
                        using AudioDataStream retryStream = AudioDataStream.FromResult(retryResult);
                        await retryStream.SaveToWaveFileAsync(filepath);
                        return filepath;
                    }

                    if (details.ErrorCode != CancellationErrorCode.TooManyRequests)
                    {
                        //Some other error
                        throw new Exception($"Error caught when rendering Azure TTS: {details.ErrorDetails}");
                    }

                    delay *= 2;
                }

                //Timeout failure
                throw new Exception($"Unable to out-wait TooManyRequests: {details.ErrorDetails}");
            }
            else
            {
                //Failed
                throw new Exception($"Error caught when rendering Azure TTS: {details.ErrorDetails}");
            }
        }

        using AudioDataStream stream = AudioDataStream.FromResult(result);
        await stream.SaveToWaveFileAsync(filepath);
        return filepath;
    }
}

public class AzureTTSWebRenderer : AzureTTSRenderer
{
    private readonly TTSWebRequestHandler ttsWebRequestHandler;

    public AzureTTSWebRenderer(
        TTSWebRequestHandler ttsWebRequestHandler,
        ICommunication? communication,
        AzureTTSVoice voice,
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
