using System;
using System.Collections.Generic;
using Amazon.Polly;
using Google.Cloud.TextToSpeech.V1;

using TASagentTwitchBot.Core.Audio.Effects;

namespace TASagentTwitchBot.Core.TTS
{
    public enum TTSVoice
    {
        Unassigned = 0,

        //Google TTS Voices
        en_AU_Standard_A,
        en_AU_Standard_B,
        en_AU_Standard_C,
        en_AU_Standard_D,

        en_IN_Standard_A,
        en_IN_Standard_B,
        en_IN_Standard_C,
        en_IN_Standard_D,

        en_GB_Standard_A,
        en_GB_Standard_B,
        en_GB_Standard_C,
        en_GB_Standard_D,
        en_GB_Standard_F,

        en_US_Standard_B,
        en_US_Standard_C,
        en_US_Standard_D,
        en_US_Standard_E,
        en_US_Standard_G,
        en_US_Standard_H,
        en_US_Standard_I,
        en_US_Standard_J,

        //AWS TTS Voices
        en_AU_Nicole,
        en_AU_Olivia,
        en_AU_Russell,

        en_GB_Amy,
        en_GB_Emma,
        en_GB_Brian,

        en_IN_Aditi,
        en_IN_Raveena,

        en_US_Ivy,
        en_US_Joanna,
        en_US_Kendra,
        en_US_Kimberly,
        en_US_Salli,
        en_US_Joey,
        en_US_Justin,
        en_US_Matthew,

        en_GB_WLS_Geraint,

        MAX
    }

    public enum TTSPitch
    {
        Unassigned = 0,

        X_Low,
        Low,
        Medium,
        High,
        X_High,

        MAX
    }

    public enum TTSService
    {
        Amazon,
        Google
    }

    public static class TTSVoiceExtensions
    {
        public static string Serialize(this TTSVoice voice)
        {
            switch (voice)
            {
                case TTSVoice.en_AU_Standard_A: return "en-AU-Standard-A";
                case TTSVoice.en_AU_Standard_B: return "en-AU-Standard-B";
                case TTSVoice.en_AU_Standard_C: return "en-AU-Standard-C";
                case TTSVoice.en_AU_Standard_D: return "en-AU-Standard-D";

                case TTSVoice.en_IN_Standard_A: return "en-IN-Standard-A";
                case TTSVoice.en_IN_Standard_B: return "en-IN-Standard-B";
                case TTSVoice.en_IN_Standard_C: return "en-IN-Standard-C";
                case TTSVoice.en_IN_Standard_D: return "en-IN-Standard-D";

                case TTSVoice.en_GB_Standard_A: return "en-GB-Standard-A";
                case TTSVoice.en_GB_Standard_B: return "en-GB-Standard-B";
                case TTSVoice.en_GB_Standard_C: return "en-GB-Standard-C";
                case TTSVoice.en_GB_Standard_D: return "en-GB-Standard-D";
                case TTSVoice.en_GB_Standard_F: return "en-GB-Standard-F";

                case TTSVoice.en_US_Standard_B: return "en-US-Standard-B";
                case TTSVoice.en_US_Standard_C: return "en-US-Standard-C";
                case TTSVoice.en_US_Standard_D: return "en-US-Standard-D";
                case TTSVoice.en_US_Standard_E: return "en-US-Standard-E";
                case TTSVoice.en_US_Standard_G: return "en-US-Standard-G";
                case TTSVoice.en_US_Standard_H: return "en-US-Standard-H";
                case TTSVoice.en_US_Standard_I: return "en-US-Standard-I";
                case TTSVoice.en_US_Standard_J: return "en-US-Standard-J";

                case TTSVoice.en_AU_Nicole: return "Nicole";
                case TTSVoice.en_AU_Olivia: return "Olivia";
                case TTSVoice.en_AU_Russell: return "Russell";
                case TTSVoice.en_GB_Amy: return "Amy";
                case TTSVoice.en_GB_Emma: return "Emma";
                case TTSVoice.en_GB_Brian: return "Brian";
                case TTSVoice.en_IN_Aditi: return "Aditi";
                case TTSVoice.en_IN_Raveena: return "Raveena";
                case TTSVoice.en_US_Ivy: return "Ivy";
                case TTSVoice.en_US_Joanna: return "Joanna";
                case TTSVoice.en_US_Kendra: return "Kendra";
                case TTSVoice.en_US_Kimberly: return "Kimberly";
                case TTSVoice.en_US_Salli: return "Salli";
                case TTSVoice.en_US_Joey: return "Joey";
                case TTSVoice.en_US_Justin: return "Justin";
                case TTSVoice.en_US_Matthew: return "Matthew";
                case TTSVoice.en_GB_WLS_Geraint: return "Geraint";

                case TTSVoice.Unassigned: return "Unassigned";

                default:
                    BGC.Debug.LogError($"Unsupported TTSVoice {voice}");
                    goto case TTSVoice.en_US_Standard_B;
            }
        }

        private static Dictionary<string, TTSVoice> ttsVoiceLookup = null;

        public static TTSVoice TranslateTTSVoice(this string voiceString)
        {
            if (ttsVoiceLookup == null)
            {
                ttsVoiceLookup = new Dictionary<string, TTSVoice>();

                for (TTSVoice voice = 0; voice < TTSVoice.MAX; voice++)
                {
                    ttsVoiceLookup.Add(voice.Serialize().ToLowerInvariant(), voice);
                }
            }

            string cleanedString = voiceString.Trim().ToLowerInvariant();

            if (ttsVoiceLookup.ContainsKey(cleanedString))
            {
                return ttsVoiceLookup[cleanedString];
            }

            if (cleanedString == "default")
            {
                return TTSVoice.Unassigned;
            }

            return TTSVoice.MAX;
        }

        public static TTSPitch TranslateTTSPitch(this string pitchString)
        {
            pitchString = pitchString.ToLowerInvariant();

            switch (pitchString)
            {
                case "x-low": return TTSPitch.X_Low;
                case "low": return TTSPitch.Low;
                case "medium": return TTSPitch.Medium;
                case "high": return TTSPitch.High;
                case "x-high": return TTSPitch.X_High;

                case "default": return TTSPitch.Unassigned;
                case "normal": return TTSPitch.Unassigned;

                default: return TTSPitch.MAX;
            }
        }

        public static Effect TranslateVoiceEffect(this string effectString)
        {
            effectString = effectString.ToLowerInvariant();

            switch (effectString)
            {
                case "default":
                case "normal":
                case "none":
                    return new NoiseVocodeEffect(22, prior: null);

                default:
                    return effectString.TranslateTTSEffect();
            }
        }

        public static Effect TranslateTTSEffect(this string effectString)
        {
            effectString = effectString.ToLowerInvariant();

            switch (effectString)
            {
                case "default":
                case "normal":
                case "none":
                    return new NoEffect();

                case "vocode":
                case "vocoded":
                case "noise":
                    return new NoiseVocodeEffect(22, prior: null);

                case "modulated":
                case "modulate":
                    return new FrequencyModulationEffect(4.0, 250.0, null);

                case "shifted":
                case "shift":
                case "shiftup":
                    return new FrequencyShiftEffect(100.0, null);

                case "shiftdown":
                    return new FrequencyShiftEffect(-100.0, null);

                case "chibi":
                    return new PitchShiftEffect(1.5, null);

                case "deep":
                    return new PitchShiftEffect(0.75, null);

                case "chibidemon":
                case "chibi_demon":
                case "chibispecial":
                case "chibi_special":
                    return new PitchShiftEffect(1.5, new NoiseVocodeEffect(20, prior: null));

                default:
                    return null;
            }
        }

        public static TTSService GetTTSService(this TTSVoice voice)
        {
            switch (voice)
            {
                case TTSVoice.en_AU_Standard_A:
                case TTSVoice.en_AU_Standard_B:
                case TTSVoice.en_AU_Standard_C:
                case TTSVoice.en_AU_Standard_D:
                case TTSVoice.en_IN_Standard_A:
                case TTSVoice.en_IN_Standard_B:
                case TTSVoice.en_IN_Standard_C:
                case TTSVoice.en_IN_Standard_D:
                case TTSVoice.en_GB_Standard_A:
                case TTSVoice.en_GB_Standard_B:
                case TTSVoice.en_GB_Standard_C:
                case TTSVoice.en_GB_Standard_D:
                case TTSVoice.en_GB_Standard_F:
                case TTSVoice.en_US_Standard_B:
                case TTSVoice.en_US_Standard_C:
                case TTSVoice.en_US_Standard_D:
                case TTSVoice.en_US_Standard_E:
                case TTSVoice.en_US_Standard_G:
                case TTSVoice.en_US_Standard_H:
                case TTSVoice.en_US_Standard_I:
                case TTSVoice.en_US_Standard_J:
                    return TTSService.Google;

                case TTSVoice.en_AU_Nicole:
                case TTSVoice.en_AU_Olivia:
                case TTSVoice.en_AU_Russell:
                case TTSVoice.en_GB_Amy:
                case TTSVoice.en_GB_Emma:
                case TTSVoice.en_GB_Brian:
                case TTSVoice.en_IN_Aditi:
                case TTSVoice.en_IN_Raveena:
                case TTSVoice.en_US_Ivy:
                case TTSVoice.en_US_Joanna:
                case TTSVoice.en_US_Kendra:
                case TTSVoice.en_US_Kimberly:
                case TTSVoice.en_US_Salli:
                case TTSVoice.en_US_Joey:
                case TTSVoice.en_US_Justin:
                case TTSVoice.en_US_Matthew:
                case TTSVoice.en_GB_WLS_Geraint:
                    return TTSService.Amazon;

                case TTSVoice.Unassigned: goto case TTSVoice.en_US_Joanna;

                default:
                    BGC.Debug.LogError($"Unsupported TTSVoice {voice}");
                    goto case TTSVoice.en_US_Standard_B;
            }
        }

        private static string GetGoogleTTSVoiceString(this TTSVoice voice)
        {
            switch (voice)
            {
                case TTSVoice.en_AU_Standard_A: return "en-AU-Standard-A";
                case TTSVoice.en_AU_Standard_B: return "en-AU-Standard-B";
                case TTSVoice.en_AU_Standard_C: return "en-AU-Standard-C";
                case TTSVoice.en_AU_Standard_D: return "en-AU-Standard-D";

                case TTSVoice.en_IN_Standard_A: return "en-IN-Standard-A";
                case TTSVoice.en_IN_Standard_B: return "en-IN-Standard-B";
                case TTSVoice.en_IN_Standard_C: return "en-IN-Standard-C";
                case TTSVoice.en_IN_Standard_D: return "en-IN-Standard-D";

                case TTSVoice.en_GB_Standard_A: return "en-GB-Standard-A";
                case TTSVoice.en_GB_Standard_B: return "en-GB-Standard-B";
                case TTSVoice.en_GB_Standard_C: return "en-GB-Standard-C";
                case TTSVoice.en_GB_Standard_D: return "en-GB-Standard-D";
                case TTSVoice.en_GB_Standard_F: return "en-GB-Standard-F";

                case TTSVoice.en_US_Standard_B: return "en-US-Standard-B";
                case TTSVoice.en_US_Standard_C: return "en-US-Standard-C";
                case TTSVoice.en_US_Standard_D: return "en-US-Standard-D";
                case TTSVoice.en_US_Standard_E: return "en-US-Standard-E";
                case TTSVoice.en_US_Standard_G: return "en-US-Standard-G";
                case TTSVoice.en_US_Standard_H: return "en-US-Standard-H";
                case TTSVoice.en_US_Standard_I: return "en-US-Standard-I";
                case TTSVoice.en_US_Standard_J: return "en-US-Standard-J";

                case TTSVoice.en_AU_Nicole:
                case TTSVoice.en_AU_Olivia:
                case TTSVoice.en_AU_Russell:
                case TTSVoice.en_GB_Amy:
                case TTSVoice.en_GB_Emma:
                case TTSVoice.en_GB_Brian:
                case TTSVoice.en_IN_Aditi:
                case TTSVoice.en_IN_Raveena:
                case TTSVoice.en_US_Ivy:
                case TTSVoice.en_US_Joanna:
                case TTSVoice.en_US_Kendra:
                case TTSVoice.en_US_Kimberly:
                case TTSVoice.en_US_Salli:
                case TTSVoice.en_US_Joey:
                case TTSVoice.en_US_Justin:
                case TTSVoice.en_US_Matthew:
                case TTSVoice.en_GB_WLS_Geraint:
                    BGC.Debug.LogError($"Tried to get Google Voice string from AWS TTS Voice {voice}");
                    goto case TTSVoice.en_US_Standard_B;

                case TTSVoice.Unassigned: goto case TTSVoice.en_US_Standard_B;

                default:
                    BGC.Debug.LogError($"TTS Voice not supported {voice}");
                    goto case TTSVoice.en_US_Standard_B;
            }
        }

        public static VoiceSelectionParams GetGoogleVoiceSelectionParams(this TTSVoice voice)
        {
            switch (voice)
            {
                case TTSVoice.Unassigned:
                    return new VoiceSelectionParams
                    {
                        LanguageCode = "en-US",
                        SsmlGender = SsmlVoiceGender.Neutral
                    };

                case TTSVoice.en_AU_Standard_A:
                case TTSVoice.en_AU_Standard_B:
                case TTSVoice.en_AU_Standard_C:
                case TTSVoice.en_AU_Standard_D:
                    return new VoiceSelectionParams
                    {
                        Name = voice.GetGoogleTTSVoiceString(),
                        LanguageCode = "en-AU",
                        SsmlGender = SsmlVoiceGender.Neutral
                    };

                case TTSVoice.en_IN_Standard_A:
                case TTSVoice.en_IN_Standard_B:
                case TTSVoice.en_IN_Standard_C:
                case TTSVoice.en_IN_Standard_D:
                    return new VoiceSelectionParams
                    {
                        Name = voice.GetGoogleTTSVoiceString(),
                        LanguageCode = "en-IN",
                        SsmlGender = SsmlVoiceGender.Neutral
                    };

                case TTSVoice.en_GB_Standard_A:
                case TTSVoice.en_GB_Standard_B:
                case TTSVoice.en_GB_Standard_C:
                case TTSVoice.en_GB_Standard_D:
                case TTSVoice.en_GB_Standard_F:
                    return new VoiceSelectionParams
                    {
                        Name = voice.GetGoogleTTSVoiceString(),
                        LanguageCode = "en-GB",
                        SsmlGender = SsmlVoiceGender.Neutral
                    };

                case TTSVoice.en_US_Standard_B:
                case TTSVoice.en_US_Standard_C:
                case TTSVoice.en_US_Standard_D:
                case TTSVoice.en_US_Standard_E:
                case TTSVoice.en_US_Standard_G:
                case TTSVoice.en_US_Standard_H:
                case TTSVoice.en_US_Standard_I:
                case TTSVoice.en_US_Standard_J:
                    return new VoiceSelectionParams
                    {
                        Name = voice.GetGoogleTTSVoiceString(),
                        LanguageCode = "en-US",
                        SsmlGender = SsmlVoiceGender.Neutral
                    };

                case TTSVoice.en_AU_Nicole:
                case TTSVoice.en_AU_Olivia:
                case TTSVoice.en_AU_Russell:
                case TTSVoice.en_GB_Amy:
                case TTSVoice.en_GB_Emma:
                case TTSVoice.en_GB_Brian:
                case TTSVoice.en_IN_Aditi:
                case TTSVoice.en_IN_Raveena:
                case TTSVoice.en_US_Ivy:
                case TTSVoice.en_US_Joanna:
                case TTSVoice.en_US_Kendra:
                case TTSVoice.en_US_Kimberly:
                case TTSVoice.en_US_Salli:
                case TTSVoice.en_US_Joey:
                case TTSVoice.en_US_Justin:
                case TTSVoice.en_US_Matthew:
                case TTSVoice.en_GB_WLS_Geraint:
                    BGC.Debug.LogError($"Tried to get Google VoiceSelectionParams from AWS TTS Voice {voice}");
                    goto case TTSVoice.Unassigned;

                default:
                    BGC.Debug.LogError($"TTS Voice not supported {voice}");
                    goto case TTSVoice.Unassigned;
            }

        }

        private static readonly List<string> awsLexicons = new List<string>() { "TASagentLex" };

        public static Amazon.Polly.Model.SynthesizeSpeechRequest GetAmazonTTSSpeechRequest(this TTSVoice voice)
        {
            Amazon.Polly.Model.SynthesizeSpeechRequest synthesisRequest = new Amazon.Polly.Model.SynthesizeSpeechRequest
            {
                OutputFormat = OutputFormat.Mp3,
                Engine = Engine.Standard,
                LexiconNames = awsLexicons
            };

            switch (voice)
            {
                case TTSVoice.en_AU_Standard_A:
                case TTSVoice.en_AU_Standard_B:
                case TTSVoice.en_AU_Standard_C:
                case TTSVoice.en_AU_Standard_D:
                case TTSVoice.en_IN_Standard_A:
                case TTSVoice.en_IN_Standard_B:
                case TTSVoice.en_IN_Standard_C:
                case TTSVoice.en_IN_Standard_D:
                case TTSVoice.en_GB_Standard_A:
                case TTSVoice.en_GB_Standard_B:
                case TTSVoice.en_GB_Standard_C:
                case TTSVoice.en_GB_Standard_D:
                case TTSVoice.en_GB_Standard_F:
                case TTSVoice.en_US_Standard_B:
                case TTSVoice.en_US_Standard_C:
                case TTSVoice.en_US_Standard_D:
                case TTSVoice.en_US_Standard_E:
                case TTSVoice.en_US_Standard_G:
                case TTSVoice.en_US_Standard_H:
                case TTSVoice.en_US_Standard_I:
                case TTSVoice.en_US_Standard_J:
                    BGC.Debug.LogError($"Tried to get Amazon VoiceId from Google TTS Voice {voice}");
                    goto case TTSVoice.en_GB_Brian;

                case TTSVoice.en_AU_Nicole:
                    synthesisRequest.VoiceId = VoiceId.Nicole;
                    break;

                //Olivia is unsupported
                case TTSVoice.en_AU_Olivia:
                    synthesisRequest.VoiceId = VoiceId.Nicole;
                    break;

                case TTSVoice.en_AU_Russell:
                    synthesisRequest.VoiceId = VoiceId.Russell;
                    break;

                case TTSVoice.en_GB_Amy:
                    synthesisRequest.VoiceId = VoiceId.Amy;
                    break;

                case TTSVoice.en_GB_Emma:
                    synthesisRequest.VoiceId = VoiceId.Emma;
                    break;

                case TTSVoice.en_GB_Brian:
                    synthesisRequest.VoiceId = VoiceId.Brian;
                    break;

                case TTSVoice.en_IN_Aditi:
                    synthesisRequest.VoiceId = VoiceId.Aditi;
                    synthesisRequest.LanguageCode = LanguageCode.EnIN;
                    break;

                case TTSVoice.en_IN_Raveena:
                    synthesisRequest.VoiceId = VoiceId.Raveena;
                    synthesisRequest.LanguageCode = LanguageCode.EnIN;
                    break;

                case TTSVoice.en_US_Ivy:
                    synthesisRequest.VoiceId = VoiceId.Ivy;
                    break;

                case TTSVoice.en_US_Joanna:
                    synthesisRequest.VoiceId = VoiceId.Joanna;
                    break;

                case TTSVoice.en_US_Kendra:
                    synthesisRequest.VoiceId = VoiceId.Kendra;
                    break;

                case TTSVoice.en_US_Kimberly:
                    synthesisRequest.VoiceId = VoiceId.Kimberly;
                    break;

                case TTSVoice.en_US_Salli:
                    synthesisRequest.VoiceId = VoiceId.Salli;
                    break;

                case TTSVoice.en_US_Joey:
                    synthesisRequest.VoiceId = VoiceId.Joey;
                    break;

                case TTSVoice.en_US_Justin:
                    synthesisRequest.VoiceId = VoiceId.Justin;
                    break;

                case TTSVoice.en_US_Matthew:
                    synthesisRequest.VoiceId = VoiceId.Matthew;
                    break;

                case TTSVoice.en_GB_WLS_Geraint:
                    synthesisRequest.VoiceId = VoiceId.Geraint;
                    break;

                case TTSVoice.Unassigned: goto case TTSVoice.en_US_Joanna;

                default:
                    BGC.Debug.LogError($"TTS Voice not supported {voice}");
                    goto case TTSVoice.Unassigned;
            }

            return synthesisRequest;
        }

        public static double GetSemitoneShift(this TTSPitch pitch)
        {
            switch (pitch)
            {
                case TTSPitch.X_Low: return -10;
                case TTSPitch.Low: return -5;
                case TTSPitch.Medium: return 0;
                case TTSPitch.High: return 5;
                case TTSPitch.X_High: return 10;

                case TTSPitch.Unassigned:
                    goto case TTSPitch.Medium;

                default:
                    BGC.Debug.LogError($"TTS Pitch not supported {pitch}");
                    goto case TTSPitch.Unassigned;
            }
        }

        public static string WrapAmazonPitchParam(this TTSPitch pitch, string text)
        {
            int pitchShift;
            switch (pitch)
            {
                case TTSPitch.X_Low:
                case TTSPitch.Low:
                case TTSPitch.High:
                case TTSPitch.X_High:
                    //proceed
                    break;

                case TTSPitch.Medium:
                    return text;

                case TTSPitch.Unassigned:
                    goto case TTSPitch.Medium;

                default:
                    BGC.Debug.LogError($"TTS Pitch not supported {pitch}");
                    goto case TTSPitch.Unassigned;
            }

            //pitchShift = (int)Math.Round(100 * Math.Pow(2, pitch.GetSemitoneShift() / 12.0)) - 100;
            pitchShift = (int)Math.Round(100 * (pitch.GetSemitoneShift() / 12.0));
            return $"<prosody pitch=\"{pitchShift:+0;-#}%\">{text}</prosody>";
        }
    }
}
