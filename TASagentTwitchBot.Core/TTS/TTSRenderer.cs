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

        //REGEX Note:
        //  (?<!\\)
        //    A negative-lookbehind group that asserts there is not a single backslash (\) preceeding the text
        //  (?:\\\\)*
        //    An anonymous capture group that greedily matches sets of escaped backslash (\\)
        //  (?<!\\)(?:\\\\)*\!
        //    Combining the above, this only matches non-escaped bangs
        //    Match !
        //    Match \\!
        //    No Match \!
        //  (?<![^\\](?:\\\\)*\\)
        //    Negative lookbehind that starts with a non-backslash character, has any number of escaped backslashes, and ends with a backslash

        //Command Regex:
        //  \/\w+
        //    Matches words preceeded by forward slashes
        //    Match /text
        //  \!\w+(?:\(.*?\))?)
        //    Matches words preceeded by a bang and optionally followed by parentheses enclosing arguments
        //    Match !text
        //    Match !text1(text2)
        //  (\/\w+|\!\w+(?:\(.*?\))?)
        //    Matches and captures either of the above two conditions
        //    Match /text -> Captures "/text"
        //    Match !text -> Captures "!text"
        //    Match !text1(text2) -> Captures "!text1(text2)"
        //  (?<![^\\](?:\\\\)*\\)(\/\w+|\!\w+(?:(?<!\\)(?:\\\\)*\(.*?(?<!\\)(?:\\\\)*\))?)
        //    Matches and captures all unescaped commands and sound effects
        //    Match /text -> Captures "/text"
        //    Match !text -> Captures "!text"
        //    Match !text1(text2) -> Captures "!text1(text2)"
        //    Match \\!text1(text2) -> Captures "!text1(text2)"
        //    No Match \/text
        //    No Match \!text1(text2)
        private static readonly Regex commandRegex = new Regex(@"(?<![^\\](?:\\\\)*\\)(\/\w+|\!\w+(?:(?<!\\)(?:\\\\)*\(.*?(?<!\\)(?:\\\\)*\))?)");

        //Whisper Regex:
        //  \(([^<>]*?)\)
        //    Matches and captures text that doesn't contain a < or > enclosed in parentheses
        //    Match (text) -> Captures "text"
        //  (?<![^\\](?:\\\\)*\\)\(([^<>]*?(?<!\\)(?:\\\\)*)\)
        //    Matches and captures text that doesn't contain a < or > enclosed in unescaped parentheses
        //    Match (text) -> Captures "text"
        //    Match \\(text) -> Captures "text"
        //    Match (text\\) -> Captures "text\"
        //    No Match \(text)
        //    No Match (text\)
        //  Replace with <whisperMarkup>$1</whisperMarkup>
        private static readonly Regex whisperRegex = new Regex(@"(?<![^\\](?:\\\\)*\\)\(([^<>]*?(?<!\\)(?:\\\\)*)\)");

        //Emphasis Regex:
        //  (\*|_)([^<>]*?)\1
        //    Matches all text that doesn't contain a < or > surrounded on either side by * or _
        //    Match *text1 text2*
        //    Match _text_
        //    No Match *text_
        //  (?<![^\\](?:\\\\)*\\)(\*|_)([^<>]*?(?<!\\)(?:\\\\)*)\1
        //    Matches and captures all text that doesn't contain a < or > surrounded on either side by unescaped * or _
        //    Match *text1 text2* -> Captures "text1 text2"
        //    Match _text_ -> Captures "text"
        //    Match \\*text* -> Captures "text"
        //    Match _text\\_ -> Captures "text\\"
        //    No Match *text_
        //    No Match \*text*
        //    No Match \\\*text*
        //    No Match *text\*
        //  Replace with <emphasis level="strong">$2</emphasis>
        private static readonly Regex emphasisRegex = new Regex(@"(?<![^\\](?:\\\\)*\\)(\*|_)([^<>]*?(?<!\\)(?:\\\\)*)\1");

        //Censored Regex:
        //  \~([^<>]*?)\~
        //    Matches all text that doesn't contain a < or > surrounded by a ~
        //    Match ~text1 text2~
        //  (?<![^\\](?:\\\\)*\\)\~([^<>]*?(?<!\\)(?:\\\\)*)\~
        //    Matches and captures all text that doesn't contain a < or > surrounded by unescaped ~
        //    Match ~text1 text2~ -> Captures "text1 text2"
        //    Match \\~text~ -> Captures "text"
        //    Match ~text\\~ -> Captures "text\\"
        //    No Match \~text~
        //    No Match ~text\~
        //    No Match \\\~text~
        //  Replace with <say-as interpret-as="expletive">$1</say-as>
        private static readonly Regex censoredRegex = new Regex(@"(?<![^\\](?:\\\\)*\\)\~([^<>]*?(?<!\\)(?:\\\\)*)\~");

        //Unescape Regex:
        //  \\([\\\(\)\~\*\,_])
        //    Match all instances of a backslash \ followed by any one of: \ ( ) ~ * , _
        //    Matches \\ -> Captures "\"
        //    Matches \( -> Captures "("
        //    Matches \) -> Captures ")"
        //    Matches \~ -> Captures "~"
        //    Matches \* -> Captures "*"
        //    Matches \, -> Captures ","
        //    Matches \_ -> Captures "_"
        //    No Match \
        //  Replace with $1
        private static readonly Regex unescapeRegex = new Regex(@"\\([\\\(\)\~\*\,_])");

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

                                        if (!string.IsNullOrEmpty(filename))
                                        {
                                            audioRequestSegments.Add(new AudioFileRequest(filename, effectsChain));
                                        }
                                        else
                                        {
                                            //Add audio delay for consistency
                                            audioRequestSegments.Add(new AudioDelay(200));
                                        }

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

                                        if (!string.IsNullOrEmpty(filename))
                                        {
                                            audioRequestSegments.Add(new AudioFileRequest(filename, effectsChain));
                                        }
                                        else
                                        {
                                            //Add audio delay for consistency
                                            audioRequestSegments.Add(new AudioDelay(200));
                                        }

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

                    if (!string.IsNullOrEmpty(filename))
                    {
                        audioRequestSegments.Add(new AudioFileRequest(filename, effectsChain));
                    }
                    else
                    {
                        //Add audio delay for consistency
                        audioRequestSegments.Add(new AudioDelay(200));
                    }


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

                if (!string.IsNullOrEmpty(filename))
                {
                    return new AudioFileRequest(filename, effectsChain);
                }
                else
                {
                    //Return audio delay for consistency
                    return new AudioDelay(200);
                }
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
            try
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
            catch (Exception e)
            {
                communication.SendErrorMessage($"Exception caught when rendering Amazon TTS {e}");
                return null;
            }
        }

        protected async Task<string> GetGoogleSynthSpeech(
            string text,
            TTSVoice voicePreference,
            TTSPitch pitchPreference,
            TTSSpeed speedPreference,
            string filename = null)
        {
            try
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
            catch (Exception e)
            {
                communication.SendErrorMessage($"Exception caught when rendering Google TTS {e}");
                return null;
            }
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

            //Handle censoring
            text = censoredRegex.Replace(text, @"<say-as interpret-as=""expletive"">$1</say-as>");

            //Handle escaped characters
            text = unescapeRegex.Replace(text, @"$1");

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

            //Handle censoring
            text = censoredRegex.Replace(text, @"<say-as interpret-as=""expletive"">$1</say-as>");

            //Handle escaped characters
            text = unescapeRegex.Replace(text, @"$1");

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
