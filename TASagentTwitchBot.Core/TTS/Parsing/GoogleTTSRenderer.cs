using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Google.Cloud.TextToSpeech.V1;

using TASagentTwitchBot.Core.Audio.Effects;

using GoogleSynthesizeSpeechResponse = Google.Cloud.TextToSpeech.V1.SynthesizeSpeechResponse;

namespace TASagentTwitchBot.Core.TTS.Parsing
{
    public class GoogleTTSRenderer : StandardTTSSystemRenderer
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

        private readonly TextToSpeechClient googleClient;

        public GoogleTTSRenderer(
            TextToSpeechClient googleClient,
            ICommunication communication,
            TTSVoice voice,
            TTSPitch pitch,
            TTSSpeed speed,
            Effect effectsChain)
            : base(
                  communication: communication,
                  voice: (voice == TTSVoice.Unassigned) ? TTSVoice.en_US_Standard_B : voice,
                  pitch: pitch,
                  speed: speed,
                  effectsChain: effectsChain)
        {
            this.googleClient = googleClient;
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

        protected override async Task<string> SynthesizeSpeech(string interiorSSML)
        {
            try
            {
                VoiceSelectionParams voiceParams = voice.GetGoogleVoiceSelectionParams();

                AudioConfig config = new AudioConfig
                {
                    AudioEncoding = AudioEncoding.Mp3,
                    Pitch = pitch.GetSemitoneShift(),
                    SpeakingRate = speed.GetGoogleSpeed()
                };

                //TTS
                SynthesisInput input = new SynthesisInput
                {
                    Ssml = WrapSSML(interiorSSML)
                };

                // Perform the Text-to-Speech request, passing the text input
                // with the selected voice parameters and audio file type
                GoogleSynthesizeSpeechResponse response = await googleClient.SynthesizeSpeechAsync(input, voiceParams, config);

                // Write the binary AudioContent of the response to file.
                string filepath = Path.Combine(TTSFilesPath, $"{Guid.NewGuid()}.mp3");

                using (Stream file = new FileStream(filepath, FileMode.Create))
                {
                    response.AudioContent.WriteTo(file);
                }

                return filepath;
            }
            catch (Exception e)
            {
                communication.SendErrorMessage($"Exception caught when rendering Google TTS {e}");
                return null;
            }
        }

        protected override string PrepareText(string text) => SanitizeXML(FixTASagent(text));

        private string WrapSSML(string interiorSSML)
        {
            return $"<speak>{interiorSSML}</speak>";
        }

        private static string FixTASagent(string text)
        {
            text = tasRegex.Replace(text, "tass");
            text = tasAgentRegex.Replace(text, "tass agent");
            return text;
        }
    }
}
