using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

using Google.Cloud.TextToSpeech.V1;
using Amazon.Polly;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Audio.Effects;

using GoogleSynthesizeSpeechResponse = Google.Cloud.TextToSpeech.V1.SynthesizeSpeechResponse;
using AmazonSynthesizeSpeechResponse = Amazon.Polly.Model.SynthesizeSpeechResponse;
using AmazonSynthesizeSpeechRequest = Amazon.Polly.Model.SynthesizeSpeechRequest;

namespace TASagentTwitchBot.Core.TTS
{
    public interface ITTSRenderer
    {
        Task<AudioRequest> TTSRequest(
            TTSVoice voicePreference,
            TTSPitch pitchPreference,
            TTSSpeed speedPreference,
            Effect effectsChain,
            string ttsText);

        Task<AudioRequest> TTSRequest(
            TTSVoice voicePreference,
            TTSPitch pitchPreference,
            TTSSpeed speedPreference,
            Effect effectsChain,
            string[] splitTTSText);
    }

    /// <summary>
    /// Handles the creation of TTS Audio files.  This class is extended by VoiceSampleGenerator for testing.
    /// </summary>
    public class TTSRenderer : ITTSRenderer
    {
        protected static string TTSFilesPath => BGC.IO.DataManagement.PathForDataDirectory("TTSFiles");

        protected readonly TextToSpeechClient googleClient;
        protected readonly AmazonPollyClient amazonClient;

        protected readonly ICommunication communication;
        protected readonly ISoundEffectSystem soundEffectSystem;

        private static readonly Regex commandRegex = new Regex(@"(\/\w+|\!\w+\(.*?\))");
        private static readonly Regex whisperRegex = new Regex(@"\((.*?)\)");
        private static readonly Regex emphasisRegex = new Regex(@"(\*|_)(.*?)\1");

        private static readonly Regex tasRegex = new Regex(@"\b[Tt][Aa][Ss]\b");
        private static readonly Regex tasAgentRegex = new Regex(@"\b[Tt][Aa][Ss][Aa][Gg][Ee][Nn][Tt]\b");

        public TTSRenderer(
            ICommunication communication,
            ISoundEffectSystem soundEffectSystem)
        {
            this.communication = communication;
            this.soundEffectSystem = soundEffectSystem;

            //
            // Prepare Google TTS
            // 
            TextToSpeechClientBuilder builder = new TextToSpeechClientBuilder();

            string googleCredentialsPath = BGC.IO.DataManagement.PathForDataFile("Config", "googleCloudCredentials.json");

            if (!File.Exists(googleCredentialsPath))
            {
                throw new FileNotFoundException($"Could not find credentials for Google TTS at {googleCredentialsPath}");
            }

            builder.CredentialsPath = googleCredentialsPath;
            googleClient = builder.Build();

            //
            // Prepare Amazon TTS
            // 
            string awsCredentialsPath = BGC.IO.DataManagement.PathForDataFile("Config", "awsPollyCredentials.json");

            if (!File.Exists(awsCredentialsPath))
            {
                throw new FileNotFoundException($"Could not find credentials for AWS Polly at {awsCredentialsPath}");
            }

            AWSPollyCredentials awsPolyCredentials = JsonSerializer.Deserialize<AWSPollyCredentials>(File.ReadAllText(awsCredentialsPath));

            Amazon.Runtime.BasicAWSCredentials awsCredentials = new Amazon.Runtime.BasicAWSCredentials(
                awsPolyCredentials.AccessKey,
                awsPolyCredentials.SecretKey);

            amazonClient = new AmazonPollyClient(awsCredentials, Amazon.RegionEndpoint.USWest1);
        }

        public Task<AudioRequest> TTSRequest(
            TTSVoice voicePreference,
            TTSPitch pitchPreference,
            TTSSpeed speedPreference,
            Effect effectsChain,
            string ttsText) =>
            TTSRequest(voicePreference, pitchPreference, speedPreference, effectsChain, ttsText.Split(' ', options: StringSplitOptions.RemoveEmptyEntries));

        public async Task<AudioRequest> TTSRequest(
            TTSVoice voicePreference,
            TTSPitch pitchPreference,
            TTSSpeed speedPreference,
            Effect effectsChain,
            string[] splitTTSText)
        {
            if (splitTTSText.Any(x => x.Contains('/') || x.Contains('!')))
            {
                List<AudioRequest> audioRequestSegments = new List<AudioRequest>();
                //Complex parsing

                StringBuilder stringbuilder = new StringBuilder();

                foreach (string ttsWord in splitTTSText)
                {
                    if (ttsWord.Contains('/') || ttsWord.Contains('!'))
                    {
                        foreach (string ttsWordSegment in SplitStringByCommandRegex(ttsWord))
                        {
                            if (ttsWordSegment.StartsWith('/'))
                            {
                                //Sound Effect
                                SoundEffect soundEffect = soundEffectSystem.GetSoundEffectByAlias(ttsWordSegment);

                                if (soundEffect is null)
                                {
                                    //Unrecognized, append as is
                                    stringbuilder.Append(ttsWordSegment);
                                }
                                else
                                {
                                    //Output current
                                    if (stringbuilder.Length > 0)
                                    {
                                        string filename = await GetSynthSpeech(
                                            stringbuilder.ToString(),
                                            voicePreference,
                                            pitchPreference,
                                            speedPreference);

                                        audioRequestSegments.Add(new AudioFileRequest(filename, effectsChain));
                                        stringbuilder.Clear();
                                    }

                                    audioRequestSegments.Add(new SoundEffectRequest(soundEffect));
                                }
                            }
                            else if (ttsWordSegment.StartsWith('!'))
                            {
                                //Command
                                AudioRequest request = AudioRequest.ParseCommand(ttsWordSegment.ToLower());

                                if (request is null)
                                {
                                    //Unrecognized, append as is
                                    stringbuilder.Append(ttsWordSegment);
                                }
                                else
                                {
                                    //Output current
                                    if (stringbuilder.Length > 0)
                                    {
                                        string filename = await GetSynthSpeech(
                                            stringbuilder.ToString(),
                                            voicePreference,
                                            pitchPreference,
                                            speedPreference);
                                        audioRequestSegments.Add(new AudioFileRequest(filename, effectsChain));
                                        stringbuilder.Clear();
                                    }

                                    audioRequestSegments.Add(request);
                                }
                            }
                            else
                            {
                                stringbuilder.Append(ttsWordSegment);
                            }
                        }

                        if (stringbuilder.Length > 0)
                        {
                            stringbuilder.Append(' ');
                        }
                    }
                    else
                    {
                        stringbuilder.Append(ttsWord);
                        stringbuilder.Append(' ');
                    }
                }

                if (stringbuilder.Length > 0)
                {
                    string filename = await GetSynthSpeech(
                        stringbuilder.ToString(),
                        voicePreference,
                        pitchPreference,
                        speedPreference);
                    audioRequestSegments.Add(new AudioFileRequest(filename, effectsChain));

                    stringbuilder.Clear();
                }

                return new ConcatenatedAudioRequest(audioRequestSegments);
            }
            else
            {
                //Simple parsing

                string ttsSpeech = string.Join(' ', splitTTSText);
                string filename = await GetSynthSpeech(
                    ttsSpeech,
                    voicePreference,
                    pitchPreference,
                    speedPreference);

                return new AudioFileRequest(filename, effectsChain);
            }
        }

        protected async Task<string> GetSynthSpeech(
            string text,
            TTSVoice voicePreference,
            TTSPitch pitchPreference,
            TTSSpeed speedPreference)
        {
            switch (voicePreference.GetTTSService())
            {
                case TTSService.Amazon:
                    return await GetAmazonSynthSpeech(text, voicePreference, pitchPreference, speedPreference);

                case TTSService.Google:
                    return await GetGoogleSynthSpeech(text, voicePreference, pitchPreference, speedPreference);

                default:
                    communication.SendErrorMessage($"Unsupported TTSVoice for TTSService {voicePreference}");
                    goto case TTSService.Google;
            }
        }

        protected async Task<string> GetAmazonSynthSpeech(
            string text,
            TTSVoice voicePreference,
            TTSPitch pitchPreference,
            TTSSpeed speedPreference,
            string filename = null)
        {
            AmazonSynthesizeSpeechRequest synthesisRequest = voicePreference.GetAmazonTTSSpeechRequest();
            synthesisRequest.TextType = TextType.Ssml;
            synthesisRequest.Text = PrepareAmazonSSML(text, pitchPreference, speedPreference, voicePreference.GetRequiresLangTag());

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

        protected async Task<string> GetGoogleSynthSpeech(
            string text,
            TTSVoice voicePreference,
            TTSPitch pitchPreference,
            TTSSpeed speedPreference,
            string filename = null)
        {
            VoiceSelectionParams voice = voicePreference.GetGoogleVoiceSelectionParams();

            AudioConfig config = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Mp3,
                Pitch = pitchPreference.GetSemitoneShift(),
                SpeakingRate = speedPreference.GetGoogleSpeed()
            };

            //TTS
            SynthesisInput input = new SynthesisInput
            {
                Ssml = PrepareGoogleSSML(text)
            };

            // Perform the Text-to-Speech request, passing the text input
            // with the selected voice parameters and audio file type
            GoogleSynthesizeSpeechResponse response = await googleClient.SynthesizeSpeechAsync(input, voice, config);

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
                response.AudioContent.WriteTo(file);
            }

            return filepath;
        }

        private static IEnumerable<string> SplitStringByCommandRegex(string inputText)
        {
            inputText = inputText.Trim();

            int lastIndex = 0;

            MatchCollection result = commandRegex.Matches(inputText);

            foreach (Match match in result)
            {
                if (match.Index > lastIndex)
                {
                    //Output preceeding text
                    string prospectiveText = inputText[lastIndex..match.Index].Trim();
                    if (prospectiveText.Length > 0)
                    {
                        yield return prospectiveText;
                    }
                    lastIndex = match.Index;
                }

                //Output prospective command
                yield return match.Value;

                lastIndex = match.Index + match.Length;
            }

            //Output end
            if (lastIndex < inputText.Length)
            {
                string prospectiveText = inputText[lastIndex..inputText.Length].Trim();
                if (prospectiveText.Length > 0)
                {
                    yield return prospectiveText;
                }
            }
        }

        private static string PrepareGoogleSSML(string text)
        {
            //Sanitize inputs
            text = SanitizeInputText(text);

            //Handle whispers in the best way we can manage
            text = whisperRegex.Replace(text, @"<emphasis level=""reduced"">$1</emphasis>");

            //Handle emphasis
            text = emphasisRegex.Replace(text, @"<emphasis level=""strong"">$2</emphasis>");

            //Fix my name
            text = tasRegex.Replace(text, "tass");
            text = tasAgentRegex.Replace(text, "tass agent");

            return $"<speak>{text}</speak>";
        }

        private static string PrepareAmazonSSML(string text, TTSPitch pitch, TTSSpeed speed, bool useLangTag)
        {
            //Sanitize inputs
            text = SanitizeInputText(text);

            //Handle whispers
            text = whisperRegex.Replace(text, @"<amazon:effect name=""whispered"">$1</amazon:effect>");

            //Handle emphasis
            text = emphasisRegex.Replace(text, @"<emphasis level=""strong"">$2</emphasis>");

            //Handle pitch
            text = text.WrapAmazonProsody(pitch, speed);

            if (useLangTag)
            {
                text = $"<lang xml:lang=\"en-US\">{text}</lang>";
            }

            return $"<speak>{text}</speak>";
        }

        private static string SanitizeInputText(string input) =>
            input.Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
    }
}
