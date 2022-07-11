namespace TASagentTwitchBot.Plugin.TTS.AzureTTS;

public static class AzureTTSVoiceExtensions
{
    private static Dictionary<string, AzureTTSVoice>? ttsVoiceLookup = null;

    public static string Serialize(this AzureTTSVoice voice)
    {
        switch (voice)
        {
            case AzureTTSVoice.en_AU_Catherine: return "Catherine";
            case AzureTTSVoice.en_AU_HayleyRUS: return "Hayley";
            case AzureTTSVoice.en_CA_HeatherRUS: return "Heather";
            case AzureTTSVoice.en_CA_Linda: return "Linda";
            case AzureTTSVoice.en_IN_Heera: return "Heera";
            case AzureTTSVoice.en_IN_PriyaRUS: return "Priya";
            case AzureTTSVoice.en_IN_Ravi: return "Ravi";
            case AzureTTSVoice.en_IE_Sean: return "Sean";
            case AzureTTSVoice.en_GB_George: return "George";
            case AzureTTSVoice.en_GB_HazelRUS: return "Hazel";
            case AzureTTSVoice.en_GB_Susan: return "Susan";
            case AzureTTSVoice.en_US_BenjaminRUS: return "Benjamin";
            case AzureTTSVoice.en_US_GuyRUS: return "Guy";
            case AzureTTSVoice.en_US_AriaRUS: return "Aria";
            case AzureTTSVoice.en_US_ZiraRUS: return "Zira";

            case AzureTTSVoice.en_AU_NatashaNeural: return "NatashaNeural";
            case AzureTTSVoice.en_AU_WilliamNeural: return "WilliamNeural";
            case AzureTTSVoice.en_CA_ClaraNeural: return "ClaraNeural";
            case AzureTTSVoice.en_CA_LiamNeural: return "LiamNeural";
            case AzureTTSVoice.en_HK_YanNeural: return "YanNeural";
            case AzureTTSVoice.en_HK_SamNeural: return "SamNeural";
            case AzureTTSVoice.en_IN_NeerjaNeural: return "NeerjaNeural";
            case AzureTTSVoice.en_IN_PrabhatNeural: return "PrabhatNeural";
            case AzureTTSVoice.en_IE_EmilyNeural: return "EmilyNeural";
            case AzureTTSVoice.en_IE_ConnorNeural: return "ConnorNeural";
            case AzureTTSVoice.en_NZ_MollyNeural: return "MollyNeural";
            case AzureTTSVoice.en_NZ_MitchellNeural: return "MitchellNeural";
            case AzureTTSVoice.en_PH_RosaNeural: return "RosaNeural";
            case AzureTTSVoice.en_PH_JamesNeural: return "JamesNeural";
            case AzureTTSVoice.en_SG_LunaNeural: return "LunaNeural";
            case AzureTTSVoice.en_SG_WayneNeural: return "WayneNeural";
            case AzureTTSVoice.en_ZA_LeahNeural: return "LeahNeural";
            case AzureTTSVoice.en_ZA_LukeNeural: return "LukeNeural";
            case AzureTTSVoice.en_GB_LibbyNeural: return "LibbyNeural";
            case AzureTTSVoice.en_GB_MiaNeural: return "MiaNeural";
            case AzureTTSVoice.en_GB_RyanNeural: return "RyanNeural";
            case AzureTTSVoice.en_US_AriaNeural: return "AriaNeural";
            case AzureTTSVoice.en_US_JennyNeural: return "JennyNeural";
            case AzureTTSVoice.en_US_GuyNeural: return "GuyNeural";

            default:
                BGC.Debug.LogError($"Unsupported AzureTTSVoice {voice}");
                goto case AzureTTSVoice.en_US_GuyRUS;
        }
    }


    public static string GetTTSVoiceString(this AzureTTSVoice voice)
    {
        switch (voice)
        {
            case AzureTTSVoice.en_AU_Catherine: return "en-AU-Catherine";
            case AzureTTSVoice.en_AU_HayleyRUS: return "en-AU-HayleyRUS";
            case AzureTTSVoice.en_CA_HeatherRUS: return "en-CA-HeatherRUS";
            case AzureTTSVoice.en_CA_Linda: return "en-CA-Linda";
            case AzureTTSVoice.en_IN_Heera: return "en-IN-Heera";
            case AzureTTSVoice.en_IN_PriyaRUS: return "en-IN-PriyaRUS";
            case AzureTTSVoice.en_IN_Ravi: return "en-IN-Ravi";
            case AzureTTSVoice.en_IE_Sean: return "en-IE-Sean";
            case AzureTTSVoice.en_GB_George: return "en-GB-George";
            case AzureTTSVoice.en_GB_HazelRUS: return "en-GB-HazelRUS";
            case AzureTTSVoice.en_GB_Susan: return "en-GB-Susan";
            case AzureTTSVoice.en_US_BenjaminRUS: return "en-US-BenjaminRUS";
            case AzureTTSVoice.en_US_GuyRUS: return "en-US-GuyRUS";
            case AzureTTSVoice.en_US_AriaRUS: return "en-US-AriaRUS";
            case AzureTTSVoice.en_US_ZiraRUS: return "en-US-ZiraRUS";

            case AzureTTSVoice.en_AU_NatashaNeural: return "en-AU-NatashaNeural";
            case AzureTTSVoice.en_AU_WilliamNeural: return "en-AU-WilliamNeural";
            case AzureTTSVoice.en_CA_ClaraNeural: return "en-CA-ClaraNeural";
            case AzureTTSVoice.en_CA_LiamNeural: return "en-CA-LiamNeural";
            case AzureTTSVoice.en_HK_YanNeural: return "en-HK-YanNeural";
            case AzureTTSVoice.en_HK_SamNeural: return "en-HK-SamNeural";
            case AzureTTSVoice.en_IN_NeerjaNeural: return "en-IN-NeerjaNeural";
            case AzureTTSVoice.en_IN_PrabhatNeural: return "en-IN-PrabhatNeural";
            case AzureTTSVoice.en_IE_EmilyNeural: return "en-IE-EmilyNeural";
            case AzureTTSVoice.en_IE_ConnorNeural: return "en-IE-ConnorNeural";
            case AzureTTSVoice.en_NZ_MollyNeural: return "en-NZ-MollyNeural";
            case AzureTTSVoice.en_NZ_MitchellNeural: return "en-NZ-MitchellNeural";
            case AzureTTSVoice.en_PH_RosaNeural: return "en-PH-RosaNeural";
            case AzureTTSVoice.en_PH_JamesNeural: return "en-PH-JamesNeural";
            case AzureTTSVoice.en_SG_LunaNeural: return "en-SG-LunaNeural";
            case AzureTTSVoice.en_SG_WayneNeural: return "en-SG-WayneNeural";
            case AzureTTSVoice.en_ZA_LeahNeural: return "en-ZA-LeahNeural";
            case AzureTTSVoice.en_ZA_LukeNeural: return "en-ZA-LukeNeural";
            case AzureTTSVoice.en_GB_LibbyNeural: return "en-GB-LibbyNeural";
            case AzureTTSVoice.en_GB_MiaNeural: return "en-GB-MiaNeural";
            case AzureTTSVoice.en_GB_RyanNeural: return "en-GB-RyanNeural";
            case AzureTTSVoice.en_US_AriaNeural: return "en-US-AriaNeural";
            case AzureTTSVoice.en_US_JennyNeural: return "en-US-JennyNeural";
            case AzureTTSVoice.en_US_GuyNeural: return "en-US-GuyNeural";

            default:
                BGC.Debug.LogError($"AzureTTSVoice not supported {voice}");
                goto case AzureTTSVoice.en_US_GuyRUS;
        }
    }

    public static bool IsNeuralVoice(this AzureTTSVoice voice)
    {
        switch (voice)
        {
            case AzureTTSVoice.en_AU_Catherine:
            case AzureTTSVoice.en_AU_HayleyRUS:
            case AzureTTSVoice.en_CA_HeatherRUS:
            case AzureTTSVoice.en_CA_Linda:
            case AzureTTSVoice.en_IN_Heera:
            case AzureTTSVoice.en_IN_PriyaRUS:
            case AzureTTSVoice.en_IN_Ravi:
            case AzureTTSVoice.en_IE_Sean:
            case AzureTTSVoice.en_GB_George:
            case AzureTTSVoice.en_GB_HazelRUS:
            case AzureTTSVoice.en_GB_Susan:
            case AzureTTSVoice.en_US_BenjaminRUS:
            case AzureTTSVoice.en_US_GuyRUS:
            case AzureTTSVoice.en_US_AriaRUS:
            case AzureTTSVoice.en_US_ZiraRUS:
                return false;

            case AzureTTSVoice.en_AU_NatashaNeural:
            case AzureTTSVoice.en_AU_WilliamNeural:
            case AzureTTSVoice.en_CA_ClaraNeural:
            case AzureTTSVoice.en_CA_LiamNeural:
            case AzureTTSVoice.en_HK_SamNeural:
            case AzureTTSVoice.en_HK_YanNeural:
            case AzureTTSVoice.en_IN_NeerjaNeural:
            case AzureTTSVoice.en_IN_PrabhatNeural:
            case AzureTTSVoice.en_IE_ConnorNeural:
            case AzureTTSVoice.en_IE_EmilyNeural:
            case AzureTTSVoice.en_NZ_MitchellNeural:
            case AzureTTSVoice.en_NZ_MollyNeural:
            case AzureTTSVoice.en_PH_RosaNeural:
            case AzureTTSVoice.en_PH_JamesNeural:
            case AzureTTSVoice.en_SG_LunaNeural:
            case AzureTTSVoice.en_SG_WayneNeural:
            case AzureTTSVoice.en_ZA_LeahNeural:
            case AzureTTSVoice.en_ZA_LukeNeural:
            case AzureTTSVoice.en_GB_LibbyNeural:
            case AzureTTSVoice.en_GB_MiaNeural:
            case AzureTTSVoice.en_GB_RyanNeural:
            case AzureTTSVoice.en_US_AriaNeural:
            case AzureTTSVoice.en_US_GuyNeural:
            case AzureTTSVoice.en_US_JennyNeural:
                return true;

            default:
                BGC.Debug.LogError($"Unsupported GoogleTTSVoice {voice}");
                goto case AzureTTSVoice.en_US_GuyRUS;
        }
    }

    public static AzureTTSVoice SafeTranslateAzureTTSVoice(this string voiceString)
    {
        AzureTTSVoice voice = voiceString.TranslateAzureTTSVoice();

        if (voice == AzureTTSVoice.MAX)
        {
            return AzureTTSVoice.en_US_GuyRUS;
        }

        return voice;
    }


    public static AzureTTSVoice TranslateAzureTTSVoice(this string voiceString)
    {
        if (ttsVoiceLookup is null)
        {
            ttsVoiceLookup = new Dictionary<string, AzureTTSVoice>();

            for (AzureTTSVoice voice = 0; voice < AzureTTSVoice.MAX; voice++)
            {
                ttsVoiceLookup.Add(Serialize(voice).ToLowerInvariant(), voice);
            }
        }

        if (string.IsNullOrEmpty(voiceString))
        {
            return AzureTTSVoice.en_US_GuyRUS;
        }

        string cleanedString = voiceString.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(cleanedString))
        {
            return AzureTTSVoice.en_US_GuyRUS;
        }

        if (ttsVoiceLookup.TryGetValue(cleanedString, out AzureTTSVoice ttsVoice))
        {
            return ttsVoice;
        }

        if (cleanedString == "default" || cleanedString == "unassigned")
        {
            return AzureTTSVoice.en_US_GuyRUS;
        }

        return AzureTTSVoice.MAX;
    }

}
