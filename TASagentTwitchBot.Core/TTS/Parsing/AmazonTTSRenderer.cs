using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Polly;

using TASagentTwitchBot.Core.Audio.Effects;
using AmazonSynthesizeSpeechResponse = Amazon.Polly.Model.SynthesizeSpeechResponse;
using AmazonSynthesizeSpeechRequest = Amazon.Polly.Model.SynthesizeSpeechRequest;

namespace TASagentTwitchBot.Core.TTS.Parsing
{
    public abstract class AmazonTTSRenderer : StandardTTSSystemRenderer
    {
        public AmazonTTSRenderer(
            ICommunication communication,
            TTSVoice voice,
            TTSPitch pitch,
            TTSSpeed speed,
            Effect effectsChain)
            : base(
                  communication: communication,
                  voice: (voice == TTSVoice.Unassigned) ? TTSVoice.en_US_Joanna : voice,
                  pitch: pitch,
                  speed: speed,
                  effectsChain: effectsChain)
        {
        }

        protected override string GetModeMarkup(TTSRenderMode mode, bool start)
        {
            if (voice.IsNeuralVoice())
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

        protected override string PrepareText(string text) => SanitizeXML(text);
    }

    public class AmazonTTSLocalRenderer : AmazonTTSRenderer
    {
        private readonly AmazonPollyClient amazonClient;

        public AmazonTTSLocalRenderer(
            AmazonPollyClient amazonClient,
            ICommunication communication,
            TTSVoice voice,
            TTSPitch pitch,
            TTSSpeed speed,
            Effect effectsChain)
            : base(
                  communication: communication,
                  voice: (voice == TTSVoice.Unassigned) ? TTSVoice.en_US_Joanna : voice,
                  pitch: pitch,
                  speed: speed,
                  effectsChain: effectsChain)
        {
            this.amazonClient = amazonClient;
        }

        protected override async Task<string> SynthesizeSpeech(string interiorSSML)
        {
            try
            {
                AmazonSynthesizeSpeechRequest synthesisRequest = voice.GetAmazonTTSSpeechRequest();
                synthesisRequest.TextType = TextType.Ssml;
                synthesisRequest.Text = WrapSSML(interiorSSML);

                //communication.SendDebugMessage(synthesisRequest.Text);

                // Perform the Text-to-Speech request, passing the text input
                // with the selected voice parameters and audio file type
                AmazonSynthesizeSpeechResponse synthesisResponse = await amazonClient.SynthesizeSpeechAsync(synthesisRequest);

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
            catch (Exception e)
            {
                communication.SendErrorMessage($"Exception caught when rendering Amazon TTS {e}");
                return null;
            }
        }

        private string WrapSSML(string interiorSSML)
        {
            if (voice.IsNeuralVoice())
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

            if (voice.GetRequiresLangTag())
            {
                interiorSSML = $"<lang xml:lang=\"en-US\">{interiorSSML}</lang>";
            }

            return $"<speak>{interiorSSML}</speak>";
        }
    }

    public class AmazonTTSWebRenderer : AmazonTTSRenderer
    {
        private readonly TTSWebRenderer ttsWebRenderer;

        public AmazonTTSWebRenderer(
            TTSWebRenderer ttsWebRenderer,
            ICommunication communication,
            TTSVoice voice,
            TTSPitch pitch,
            TTSSpeed speed,
            Effect effectsChain)
            : base(
                  communication: communication,
                  voice: (voice == TTSVoice.Unassigned) ? TTSVoice.en_US_Joanna : voice,
                  pitch: pitch,
                  speed: speed,
                  effectsChain: effectsChain)
        {
            this.ttsWebRenderer = ttsWebRenderer;
        }

        protected override Task<string> SynthesizeSpeech(string interiorSSML)
        {
            return ttsWebRenderer.SubmitTTSWebRequest(new ServerTTSRequest(
                RequestIdentifier: Guid.NewGuid().ToString(),
                Ssml: interiorSSML,
                Voice: voice,
                Pitch: pitch,
                Speed: speed));
        }
    }
}
