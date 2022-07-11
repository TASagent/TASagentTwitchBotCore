using Google.Cloud.TextToSpeech.V1;

namespace TASagentTwitchBot.Plugin.TTS.GoogleTTS;

public static class GoogleTTSVoiceExtensions
{
    private static Dictionary<string, GoogleTTSVoice>? ttsVoiceLookup = null;

    public static string Serialize(this GoogleTTSVoice voice)
    {
        switch (voice)
        {
            case GoogleTTSVoice.en_AU_Standard_A: return "en-AU-Standard-A";
            case GoogleTTSVoice.en_AU_Standard_B: return "en-AU-Standard-B";
            case GoogleTTSVoice.en_AU_Standard_C: return "en-AU-Standard-C";
            case GoogleTTSVoice.en_AU_Standard_D: return "en-AU-Standard-D";
            case GoogleTTSVoice.en_IN_Standard_A: return "en-IN-Standard-A";
            case GoogleTTSVoice.en_IN_Standard_B: return "en-IN-Standard-B";
            case GoogleTTSVoice.en_IN_Standard_C: return "en-IN-Standard-C";
            case GoogleTTSVoice.en_IN_Standard_D: return "en-IN-Standard-D";
            case GoogleTTSVoice.en_GB_Standard_A: return "en-GB-Standard-A";
            case GoogleTTSVoice.en_GB_Standard_B: return "en-GB-Standard-B";
            case GoogleTTSVoice.en_GB_Standard_C: return "en-GB-Standard-C";
            case GoogleTTSVoice.en_GB_Standard_D: return "en-GB-Standard-D";
            case GoogleTTSVoice.en_GB_Standard_F: return "en-GB-Standard-F";
            case GoogleTTSVoice.en_US_Standard_A: return "en-US-Standard-A";
            case GoogleTTSVoice.en_US_Standard_B: return "en-US-Standard-B";
            case GoogleTTSVoice.en_US_Standard_C: return "en-US-Standard-C";
            case GoogleTTSVoice.en_US_Standard_D: return "en-US-Standard-D";
            case GoogleTTSVoice.en_US_Standard_E: return "en-US-Standard-E";
            case GoogleTTSVoice.en_US_Standard_F: return "en-US-Standard-F";
            case GoogleTTSVoice.en_US_Standard_G: return "en-US-Standard-G";
            case GoogleTTSVoice.en_US_Standard_H: return "en-US-Standard-H";
            case GoogleTTSVoice.en_US_Standard_I: return "en-US-Standard-I";
            case GoogleTTSVoice.en_US_Standard_J: return "en-US-Standard-J";

            case GoogleTTSVoice.en_AU_Wavenet_A: return "en-AU-Wavenet-A";
            case GoogleTTSVoice.en_AU_Wavenet_B: return "en-AU-Wavenet-B";
            case GoogleTTSVoice.en_AU_Wavenet_C: return "en-AU-Wavenet-C";
            case GoogleTTSVoice.en_AU_Wavenet_D: return "en-AU-Wavenet-D";
            case GoogleTTSVoice.en_IN_Wavenet_A: return "en-IN-Wavenet-A";
            case GoogleTTSVoice.en_IN_Wavenet_B: return "en-IN-Wavenet-B";
            case GoogleTTSVoice.en_IN_Wavenet_C: return "en-IN-Wavenet-C";
            case GoogleTTSVoice.en_IN_Wavenet_D: return "en-IN-Wavenet-D";
            case GoogleTTSVoice.en_GB_Wavenet_A: return "en-GB-Wavenet-A";
            case GoogleTTSVoice.en_GB_Wavenet_B: return "en-GB-Wavenet-B";
            case GoogleTTSVoice.en_GB_Wavenet_C: return "en-GB-Wavenet-C";
            case GoogleTTSVoice.en_GB_Wavenet_D: return "en-GB-Wavenet-D";
            case GoogleTTSVoice.en_GB_Wavenet_F: return "en-GB-Wavenet-F";
            case GoogleTTSVoice.en_US_Wavenet_A: return "en-US-Wavenet-A";
            case GoogleTTSVoice.en_US_Wavenet_B: return "en-US-Wavenet-B";
            case GoogleTTSVoice.en_US_Wavenet_C: return "en-US-Wavenet-C";
            case GoogleTTSVoice.en_US_Wavenet_D: return "en-US-Wavenet-D";
            case GoogleTTSVoice.en_US_Wavenet_E: return "en-US-Wavenet-E";
            case GoogleTTSVoice.en_US_Wavenet_F: return "en-US-Wavenet-F";
            case GoogleTTSVoice.en_US_Wavenet_G: return "en-US-Wavenet-G";
            case GoogleTTSVoice.en_US_Wavenet_H: return "en-US-Wavenet-H";
            case GoogleTTSVoice.en_US_Wavenet_I: return "en-US-Wavenet-I";
            case GoogleTTSVoice.en_US_Wavenet_J: return "en-US-Wavenet-J";

            default:
                BGC.Debug.LogError($"Unsupported GoogleTTSVoice {voice}");
                goto case GoogleTTSVoice.en_AU_Standard_B;
        }
    }


    public static GoogleTTSVoice SafeTranslateGoogleTTSVoice(this string voiceString)
    {
        GoogleTTSVoice voice = voiceString.TranslateGoogleTTSVoice();

        if (voice == GoogleTTSVoice.MAX)
        {
            return GoogleTTSVoice.en_US_Standard_B;
        }

        return voice;
    }

    public static GoogleTTSVoice TranslateGoogleTTSVoice(this string voiceString)
    {
        if (ttsVoiceLookup is null)
        {
            ttsVoiceLookup = new Dictionary<string, GoogleTTSVoice>();

            for (GoogleTTSVoice voice = 0; voice < GoogleTTSVoice.MAX; voice++)
            {
                ttsVoiceLookup.Add(Serialize(voice).ToLowerInvariant(), voice);
            }
        }

        if (string.IsNullOrEmpty(voiceString))
        {
            return GoogleTTSVoice.en_US_Standard_B;
        }

        string cleanedString = voiceString.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(cleanedString))
        {
            return GoogleTTSVoice.en_US_Standard_B;
        }

        if (ttsVoiceLookup.TryGetValue(cleanedString, out GoogleTTSVoice ttsVoice))
        {
            return ttsVoice;
        }

        if (cleanedString == "default" || cleanedString == "unassigned")
        {
            return GoogleTTSVoice.en_US_Standard_B;
        }

        return GoogleTTSVoice.MAX;
    }

    public static string GetTTSVoiceString(this GoogleTTSVoice voice)
    {
        switch (voice)
        {
            case GoogleTTSVoice.en_AU_Standard_A: return "en-AU-Standard-A";
            case GoogleTTSVoice.en_AU_Standard_B: return "en-AU-Standard-B";
            case GoogleTTSVoice.en_AU_Standard_C: return "en-AU-Standard-C";
            case GoogleTTSVoice.en_AU_Standard_D: return "en-AU-Standard-D";

            case GoogleTTSVoice.en_IN_Standard_A: return "en-IN-Standard-A";
            case GoogleTTSVoice.en_IN_Standard_B: return "en-IN-Standard-B";
            case GoogleTTSVoice.en_IN_Standard_C: return "en-IN-Standard-C";
            case GoogleTTSVoice.en_IN_Standard_D: return "en-IN-Standard-D";

            case GoogleTTSVoice.en_GB_Standard_A: return "en-GB-Standard-A";
            case GoogleTTSVoice.en_GB_Standard_B: return "en-GB-Standard-B";
            case GoogleTTSVoice.en_GB_Standard_C: return "en-GB-Standard-C";
            case GoogleTTSVoice.en_GB_Standard_D: return "en-GB-Standard-D";
            case GoogleTTSVoice.en_GB_Standard_F: return "en-GB-Standard-F";

            case GoogleTTSVoice.en_US_Standard_A: return "en-US-Standard-A";
            case GoogleTTSVoice.en_US_Standard_B: return "en-US-Standard-B";
            case GoogleTTSVoice.en_US_Standard_C: return "en-US-Standard-C";
            case GoogleTTSVoice.en_US_Standard_D: return "en-US-Standard-D";
            case GoogleTTSVoice.en_US_Standard_E: return "en-US-Standard-E";
            case GoogleTTSVoice.en_US_Standard_F: return "en-US-Standard-F";
            case GoogleTTSVoice.en_US_Standard_G: return "en-US-Standard-G";
            case GoogleTTSVoice.en_US_Standard_H: return "en-US-Standard-H";
            case GoogleTTSVoice.en_US_Standard_I: return "en-US-Standard-I";
            case GoogleTTSVoice.en_US_Standard_J: return "en-US-Standard-J";

            case GoogleTTSVoice.en_AU_Wavenet_A: return "en-AU-Wavenet-A";
            case GoogleTTSVoice.en_AU_Wavenet_B: return "en-AU-Wavenet-B";
            case GoogleTTSVoice.en_AU_Wavenet_C: return "en-AU-Wavenet-C";
            case GoogleTTSVoice.en_AU_Wavenet_D: return "en-AU-Wavenet-D";

            case GoogleTTSVoice.en_IN_Wavenet_A: return "en-IN-Wavenet-A";
            case GoogleTTSVoice.en_IN_Wavenet_B: return "en-IN-Wavenet-B";
            case GoogleTTSVoice.en_IN_Wavenet_C: return "en-IN-Wavenet-C";
            case GoogleTTSVoice.en_IN_Wavenet_D: return "en-IN-Wavenet-D";

            case GoogleTTSVoice.en_GB_Wavenet_A: return "en-GB-Wavenet-A";
            case GoogleTTSVoice.en_GB_Wavenet_B: return "en-GB-Wavenet-B";
            case GoogleTTSVoice.en_GB_Wavenet_C: return "en-GB-Wavenet-C";
            case GoogleTTSVoice.en_GB_Wavenet_D: return "en-GB-Wavenet-D";
            case GoogleTTSVoice.en_GB_Wavenet_F: return "en-GB-Wavenet-F";

            case GoogleTTSVoice.en_US_Wavenet_A: return "en-US-Wavenet-A";
            case GoogleTTSVoice.en_US_Wavenet_B: return "en-US-Wavenet-B";
            case GoogleTTSVoice.en_US_Wavenet_C: return "en-US-Wavenet-C";
            case GoogleTTSVoice.en_US_Wavenet_D: return "en-US-Wavenet-D";
            case GoogleTTSVoice.en_US_Wavenet_E: return "en-US-Wavenet-E";
            case GoogleTTSVoice.en_US_Wavenet_F: return "en-US-Wavenet-F";
            case GoogleTTSVoice.en_US_Wavenet_G: return "en-US-Wavenet-G";
            case GoogleTTSVoice.en_US_Wavenet_H: return "en-US-Wavenet-H";
            case GoogleTTSVoice.en_US_Wavenet_I: return "en-US-Wavenet-I";
            case GoogleTTSVoice.en_US_Wavenet_J: return "en-US-Wavenet-J";

            default:
                BGC.Debug.LogError($"GoogleTTSVoice not supported {voice}");
                goto case GoogleTTSVoice.en_US_Standard_B;
        }
    }

    public static bool IsNeuralVoice(this GoogleTTSVoice voice)
    {
        switch (voice)
        {
            //Google Standard Voices
            case GoogleTTSVoice.en_AU_Standard_A:
            case GoogleTTSVoice.en_AU_Standard_B:
            case GoogleTTSVoice.en_AU_Standard_C:
            case GoogleTTSVoice.en_AU_Standard_D:
            case GoogleTTSVoice.en_IN_Standard_A:
            case GoogleTTSVoice.en_IN_Standard_B:
            case GoogleTTSVoice.en_IN_Standard_C:
            case GoogleTTSVoice.en_IN_Standard_D:
            case GoogleTTSVoice.en_GB_Standard_A:
            case GoogleTTSVoice.en_GB_Standard_B:
            case GoogleTTSVoice.en_GB_Standard_C:
            case GoogleTTSVoice.en_GB_Standard_D:
            case GoogleTTSVoice.en_GB_Standard_F:
            case GoogleTTSVoice.en_US_Standard_A:
            case GoogleTTSVoice.en_US_Standard_B:
            case GoogleTTSVoice.en_US_Standard_C:
            case GoogleTTSVoice.en_US_Standard_D:
            case GoogleTTSVoice.en_US_Standard_E:
            case GoogleTTSVoice.en_US_Standard_F:
            case GoogleTTSVoice.en_US_Standard_G:
            case GoogleTTSVoice.en_US_Standard_H:
            case GoogleTTSVoice.en_US_Standard_I:
            case GoogleTTSVoice.en_US_Standard_J:
                return false;

            //Google Neural Voices
            case GoogleTTSVoice.en_AU_Wavenet_A:
            case GoogleTTSVoice.en_AU_Wavenet_B:
            case GoogleTTSVoice.en_AU_Wavenet_C:
            case GoogleTTSVoice.en_AU_Wavenet_D:
            case GoogleTTSVoice.en_IN_Wavenet_A:
            case GoogleTTSVoice.en_IN_Wavenet_B:
            case GoogleTTSVoice.en_IN_Wavenet_C:
            case GoogleTTSVoice.en_IN_Wavenet_D:
            case GoogleTTSVoice.en_GB_Wavenet_A:
            case GoogleTTSVoice.en_GB_Wavenet_B:
            case GoogleTTSVoice.en_GB_Wavenet_C:
            case GoogleTTSVoice.en_GB_Wavenet_D:
            case GoogleTTSVoice.en_GB_Wavenet_F:
            case GoogleTTSVoice.en_US_Wavenet_A:
            case GoogleTTSVoice.en_US_Wavenet_B:
            case GoogleTTSVoice.en_US_Wavenet_C:
            case GoogleTTSVoice.en_US_Wavenet_D:
            case GoogleTTSVoice.en_US_Wavenet_E:
            case GoogleTTSVoice.en_US_Wavenet_F:
            case GoogleTTSVoice.en_US_Wavenet_G:
            case GoogleTTSVoice.en_US_Wavenet_H:
            case GoogleTTSVoice.en_US_Wavenet_I:
            case GoogleTTSVoice.en_US_Wavenet_J:
                return true;

            default:
                BGC.Debug.LogError($"Unsupported GoogleTTSVoice {voice}");
                goto case GoogleTTSVoice.en_US_Standard_B;
        }
    }


    public static VoiceSelectionParams GetGoogleVoiceSelectionParams(this GoogleTTSVoice voice)
    {
        switch (voice)
        {
            case GoogleTTSVoice.en_AU_Standard_A:
            case GoogleTTSVoice.en_AU_Standard_B:
            case GoogleTTSVoice.en_AU_Standard_C:
            case GoogleTTSVoice.en_AU_Standard_D:
            case GoogleTTSVoice.en_AU_Wavenet_A:
            case GoogleTTSVoice.en_AU_Wavenet_B:
            case GoogleTTSVoice.en_AU_Wavenet_C:
            case GoogleTTSVoice.en_AU_Wavenet_D:
                return new VoiceSelectionParams
                {
                    Name = voice.GetTTSVoiceString(),
                    LanguageCode = "en-AU",
                    SsmlGender = SsmlVoiceGender.Neutral
                };

            case GoogleTTSVoice.en_IN_Standard_A:
            case GoogleTTSVoice.en_IN_Standard_B:
            case GoogleTTSVoice.en_IN_Standard_C:
            case GoogleTTSVoice.en_IN_Standard_D:
            case GoogleTTSVoice.en_IN_Wavenet_A:
            case GoogleTTSVoice.en_IN_Wavenet_B:
            case GoogleTTSVoice.en_IN_Wavenet_C:
            case GoogleTTSVoice.en_IN_Wavenet_D:
                return new VoiceSelectionParams
                {
                    Name = voice.GetTTSVoiceString(),
                    LanguageCode = "en-IN",
                    SsmlGender = SsmlVoiceGender.Neutral
                };

            case GoogleTTSVoice.en_GB_Standard_A:
            case GoogleTTSVoice.en_GB_Standard_B:
            case GoogleTTSVoice.en_GB_Standard_C:
            case GoogleTTSVoice.en_GB_Standard_D:
            case GoogleTTSVoice.en_GB_Standard_F:
            case GoogleTTSVoice.en_GB_Wavenet_A:
            case GoogleTTSVoice.en_GB_Wavenet_B:
            case GoogleTTSVoice.en_GB_Wavenet_C:
            case GoogleTTSVoice.en_GB_Wavenet_D:
            case GoogleTTSVoice.en_GB_Wavenet_F:
                return new VoiceSelectionParams
                {
                    Name = voice.GetTTSVoiceString(),
                    LanguageCode = "en-GB",
                    SsmlGender = SsmlVoiceGender.Neutral
                };

            case GoogleTTSVoice.en_US_Standard_A:
            case GoogleTTSVoice.en_US_Standard_B:
            case GoogleTTSVoice.en_US_Standard_C:
            case GoogleTTSVoice.en_US_Standard_D:
            case GoogleTTSVoice.en_US_Standard_E:
            case GoogleTTSVoice.en_US_Standard_F:
            case GoogleTTSVoice.en_US_Standard_G:
            case GoogleTTSVoice.en_US_Standard_H:
            case GoogleTTSVoice.en_US_Standard_I:
            case GoogleTTSVoice.en_US_Standard_J:
            case GoogleTTSVoice.en_US_Wavenet_A:
            case GoogleTTSVoice.en_US_Wavenet_B:
            case GoogleTTSVoice.en_US_Wavenet_C:
            case GoogleTTSVoice.en_US_Wavenet_D:
            case GoogleTTSVoice.en_US_Wavenet_E:
            case GoogleTTSVoice.en_US_Wavenet_F:
            case GoogleTTSVoice.en_US_Wavenet_G:
            case GoogleTTSVoice.en_US_Wavenet_H:
            case GoogleTTSVoice.en_US_Wavenet_I:
            case GoogleTTSVoice.en_US_Wavenet_J:
                return new VoiceSelectionParams
                {
                    Name = voice.GetTTSVoiceString(),
                    LanguageCode = "en-US",
                    SsmlGender = SsmlVoiceGender.Neutral
                };

            default:
                BGC.Debug.LogError($"GoogleTTSVoice not supported {voice}");
                goto case GoogleTTSVoice.en_US_Standard_B;
        }
    }

}
