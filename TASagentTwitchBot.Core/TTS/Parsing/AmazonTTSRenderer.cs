using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Polly;

using TASagentTwitchBot.Core.Audio.Effects;
using AmazonSynthesizeSpeechResponse = Amazon.Polly.Model.SynthesizeSpeechResponse;
using AmazonSynthesizeSpeechRequest = Amazon.Polly.Model.SynthesizeSpeechRequest;

namespace TASagentTwitchBot.Core.TTS.Parsing
{
    public class AmazonTTSRenderer : StandardTTSSystemRenderer
    {
        private readonly AmazonPollyClient amazonClient;

        public AmazonTTSRenderer(
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

        protected override string GetModeMarkup(TTSRenderMode mode, bool start)
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

        protected override async Task<string> SynthesizeSpeech(
            string interiorSSML,
            string filename = null)
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
                string filepath;
                if (string.IsNullOrWhiteSpace(filename))
                {
                    filepath = Path.Combine(TTSFilesPath, $"{Guid.NewGuid()}.mp3");
                }
                else
                {
                    filepath = Path.Combine(TTSFilesPath, $"{filename}.mp3");
                }

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
            if (pitch != TTSPitch.Medium || speed != TTSSpeed.Medium)
            {
                interiorSSML = $"<prosody pitch=\"{pitch.GetPitchShift()}\" rate=\"{speed.GetSpeedValue()}\">{interiorSSML}</prosody>";
            }

            if (voice.GetRequiresLangTag())
            {
                interiorSSML = $"<lang xml:lang=\"en-US\">{interiorSSML}</lang>";
            }

            return $"<speak>{interiorSSML}</speak>";
        }

        protected override string PrepareText(string text) => SanitizeXML(text);
    }
}
