using System.Text.RegularExpressions;
using Google.Cloud.TextToSpeech.V1;

using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.TTS;
using TASagentTwitchBot.Core.TTS.Parsing;
using TASagentTwitchBot.Core.Audio.Effects;

namespace TASagentTwitchBot.Plugin.TTS.GoogleTTS;

public abstract class GoogleTTSRenderer : StandardTTSSystemRenderer
{
    //TAS Regex:
    //  \bTAS\b
    //    Matches the word TAS when wrapped in word boundaries
    //    Match TAS
    //    Match TAS,
    //    No Match aTAS
    //    No Match TASa
    private static readonly Regex tasRegex = new Regex(@"\bTAS\b", RegexOptions.IgnoreCase);

    //TASagent Regex:
    //  \bTASagent\b
    //    Matches the word TASagent when wrapped in word boundaries
    //    Match TASagent
    //    Match TASagent,
    //    No Match aTASagent
    //    No Match TASagenta
    private static readonly Regex tasAgentRegex = new Regex(@"\bTASagent\b", RegexOptions.IgnoreCase);

    protected readonly GoogleTTSVoice googleVoice;

    public GoogleTTSRenderer(
        ICommunication? communication,
        ILogger? logger,
        GoogleTTSVoice voice,
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
        googleVoice = voice;
    }

    protected override string GetModeMarkup(TTSRenderMode mode, bool start)
    {
        switch (mode)
        {
            case TTSRenderMode.Whisper:
                return start ? "<emphasis level=\"reduced\">" : "</emphasis>";

            case TTSRenderMode.Emphasis:
                return start ? "<emphasis level=\"strong\">" : "</emphasis>";

            case TTSRenderMode.Censor:
                return start ? "<say-as interpret-as=\"expletive\">" : "</say-as>";

            case TTSRenderMode.Normal:
            default:
                throw new Exception($"Unsupported RenderMode for Markup: {mode}");
        }
    }

    protected override string PrepareText(string text) => SanitizeXML(FixTASagent(text));

    private static string FixTASagent(string text)
    {
        text = tasRegex.Replace(text, "tass");
        text = tasAgentRegex.Replace(text, "tass agent");
        return text;
    }

    protected override string FinalizeSSML(string interiorSSML)
    {
        return $"<speak>{interiorSSML}</speak>";
    }
}

public class GoogleTTSLocalRenderer : GoogleTTSRenderer
{
    private readonly TextToSpeechClient googleClient;

    public GoogleTTSLocalRenderer(
        TextToSpeechClient googleClient,
        ICommunication communication,
        GoogleTTSVoice voice,
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
        this.googleClient = googleClient;
    }

    public GoogleTTSLocalRenderer(
        TextToSpeechClient googleClient,
        ILogger logger,
        GoogleTTSVoice voice,
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
        this.googleClient = googleClient;
    }

    public override async Task<string?> SynthesizeSpeech(string finalSSML)
    {
        VoiceSelectionParams voiceParams = googleVoice.GetGoogleVoiceSelectionParams();

        AudioConfig config = new AudioConfig
        {
            AudioEncoding = AudioEncoding.Mp3,
            Pitch = pitch.GetSemitoneShift(),
            SpeakingRate = speed.GetGoogleSpeed()
        };

        //TTS
        SynthesisInput input = new SynthesisInput
        {
            Ssml = finalSSML
        };

        // Perform the Text-to-Speech request, passing the text input
        // with the selected voice parameters and audio file type
        SynthesizeSpeechResponse response = await googleClient.SynthesizeSpeechAsync(input, voiceParams, config);

        // Write the binary AudioContent of the response to file.
        string filepath = Path.Combine(TTSFilesPath, $"{Guid.NewGuid()}.mp3");

        using (Stream file = new FileStream(filepath, FileMode.Create))
        {
            response.AudioContent.WriteTo(file);
        }

        return filepath;
    }
}

public class GoogleTTSWebRenderer : GoogleTTSRenderer
{
    private readonly TTSWebRequestHandler ttsWebRequestHandler;

    public GoogleTTSWebRenderer(
        TTSWebRequestHandler ttsWebRequestHandler,
        ICommunication communication,
        GoogleTTSVoice voice,
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
