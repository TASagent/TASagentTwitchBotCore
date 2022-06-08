
using Amazon.Polly;
using Google.Cloud.TextToSpeech.V1;

namespace TASagentTwitchBot.Core.TTS;

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
    en_AU_OliviaNeural,
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

    //Expanded AWS TTS Voices
    fr_FR_Celine,
    fr_FR_Lea,
    fr_FR_Mathieu,

    fr_CA_Chantal,

    de_DE_Marlene,
    de_DE_Vicki,
    de_DE_Hans,

    it_IT_Bianca,
    it_IT_Carla,
    it_IT_Giorgio,

    pl_PL_Ewa,
    pl_PL_Maja,
    pl_PL_Jacek,
    pl_PL_Jan,

    pt_BR_Vitoria,
    pt_BR_Camila,
    pt_BR_Ricardo,

    ru_RU_Tatyana,
    ru_RU_Maxim,

    es_ES_Lucia,
    es_ES_Conchita,
    es_ES_Enrique,

    es_MX_Mia,

    es_US_Penelope,
    es_US_Lupe,
    es_US_Miguel,

    tr_TR_Filiz,

    cy_GB_Gwyneth,

    //Azure Standard Voices
    en_AU_Catherine,
    en_AU_HayleyRUS,

    en_CA_HeatherRUS,
    en_CA_Linda,

    en_IN_Heera,
    en_IN_PriyaRUS,
    en_IN_Ravi,

    en_IE_Sean,

    en_GB_George,
    en_GB_HazelRUS,
    en_GB_Susan,

    en_US_BenjaminRUS,
    en_US_GuyRUS,
    en_US_AriaRUS,
    en_US_ZiraRUS,

    //Azure Neural Voices
    en_AU_NatashaNeural,
    en_AU_WilliamNeural,

    en_CA_ClaraNeural,
    en_CA_LiamNeural,

    en_HK_SamNeural,
    en_HK_YanNeural,

    en_IN_NeerjaNeural,
    en_IN_PrabhatNeural,

    en_IE_ConnorNeural,
    en_IE_EmilyNeural,

    en_NZ_MitchellNeural,
    en_NZ_MollyNeural,

    en_PH_RosaNeural,
    en_PH_JamesNeural,

    en_SG_LunaNeural,
    en_SG_WayneNeural,

    en_ZA_LeahNeural,
    en_ZA_LukeNeural,

    en_GB_LibbyNeural,
    en_GB_MiaNeural,
    en_GB_RyanNeural,

    en_US_AriaNeural,
    en_US_GuyNeural,
    en_US_JennyNeural,

    //New Google TTS Voices
    en_US_Standard_A,
    en_US_Standard_F,

    //Google Neural Voices
    en_AU_Wavenet_A,
    en_AU_Wavenet_B,
    en_AU_Wavenet_C,
    en_AU_Wavenet_D,

    en_IN_Wavenet_A,
    en_IN_Wavenet_B,
    en_IN_Wavenet_C,
    en_IN_Wavenet_D,

    en_GB_Wavenet_A,
    en_GB_Wavenet_B,
    en_GB_Wavenet_C,
    en_GB_Wavenet_D,
    en_GB_Wavenet_F,

    en_US_Wavenet_A,
    en_US_Wavenet_B,
    en_US_Wavenet_C,
    en_US_Wavenet_D,
    en_US_Wavenet_E,
    en_US_Wavenet_F,
    en_US_Wavenet_G,
    en_US_Wavenet_H,
    en_US_Wavenet_I,
    en_US_Wavenet_J,

    //AWS Neural
    en_GB_AmyNeural,
    en_GB_BrianNeural,
    en_GB_EmmaNeural,

    en_US_IvyNeural,
    en_US_JoannaNeural,
    en_US_JoeyNeural,
    en_US_JustinNeural,
    en_US_KendraNeural,
    en_US_KevinNeural,
    en_US_KimberlyNeural,
    en_US_MatthewNeural,
    en_US_SalliNeural,

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

public enum TTSSpeed
{
    Unassigned = 0,

    X_Slow,
    Slow,
    Medium,
    Fast,
    X_Fast,

    MAX
}

public enum TTSService
{
    Amazon,
    Google,
    Azure,
    MAX
}

public static class TTSVoiceExtensions
{
    public static string Serialize(this TTSVoice voice)
    {
        switch (voice)
        {
            case TTSVoice.Unassigned: return "Unassigned";

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
            case TTSVoice.en_US_Standard_A: return "en-US-Standard-A";
            case TTSVoice.en_US_Standard_B: return "en-US-Standard-B";
            case TTSVoice.en_US_Standard_C: return "en-US-Standard-C";
            case TTSVoice.en_US_Standard_D: return "en-US-Standard-D";
            case TTSVoice.en_US_Standard_E: return "en-US-Standard-E";
            case TTSVoice.en_US_Standard_F: return "en-US-Standard-F";
            case TTSVoice.en_US_Standard_G: return "en-US-Standard-G";
            case TTSVoice.en_US_Standard_H: return "en-US-Standard-H";
            case TTSVoice.en_US_Standard_I: return "en-US-Standard-I";
            case TTSVoice.en_US_Standard_J: return "en-US-Standard-J";

            case TTSVoice.en_AU_Wavenet_A: return "en-AU-Wavenet-A";
            case TTSVoice.en_AU_Wavenet_B: return "en-AU-Wavenet-B";
            case TTSVoice.en_AU_Wavenet_C: return "en-AU-Wavenet-C";
            case TTSVoice.en_AU_Wavenet_D: return "en-AU-Wavenet-D";
            case TTSVoice.en_IN_Wavenet_A: return "en-IN-Wavenet-A";
            case TTSVoice.en_IN_Wavenet_B: return "en-IN-Wavenet-B";
            case TTSVoice.en_IN_Wavenet_C: return "en-IN-Wavenet-C";
            case TTSVoice.en_IN_Wavenet_D: return "en-IN-Wavenet-D";
            case TTSVoice.en_GB_Wavenet_A: return "en-GB-Wavenet-A";
            case TTSVoice.en_GB_Wavenet_B: return "en-GB-Wavenet-B";
            case TTSVoice.en_GB_Wavenet_C: return "en-GB-Wavenet-C";
            case TTSVoice.en_GB_Wavenet_D: return "en-GB-Wavenet-D";
            case TTSVoice.en_GB_Wavenet_F: return "en-GB-Wavenet-F";
            case TTSVoice.en_US_Wavenet_A: return "en-US-Wavenet-A";
            case TTSVoice.en_US_Wavenet_B: return "en-US-Wavenet-B";
            case TTSVoice.en_US_Wavenet_C: return "en-US-Wavenet-C";
            case TTSVoice.en_US_Wavenet_D: return "en-US-Wavenet-D";
            case TTSVoice.en_US_Wavenet_E: return "en-US-Wavenet-E";
            case TTSVoice.en_US_Wavenet_F: return "en-US-Wavenet-F";
            case TTSVoice.en_US_Wavenet_G: return "en-US-Wavenet-G";
            case TTSVoice.en_US_Wavenet_H: return "en-US-Wavenet-H";
            case TTSVoice.en_US_Wavenet_I: return "en-US-Wavenet-I";
            case TTSVoice.en_US_Wavenet_J: return "en-US-Wavenet-J";

            case TTSVoice.en_AU_Nicole: return "Nicole";
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

            case TTSVoice.fr_FR_Celine: return "Celine";
            case TTSVoice.fr_FR_Lea: return "Lea";
            case TTSVoice.fr_FR_Mathieu: return "Mathieu";
            case TTSVoice.fr_CA_Chantal: return "Chantal";
            case TTSVoice.de_DE_Marlene: return "Marlene";
            case TTSVoice.de_DE_Vicki: return "Vicki";
            case TTSVoice.de_DE_Hans: return "Hans";
            case TTSVoice.it_IT_Bianca: return "Bianca";
            case TTSVoice.it_IT_Carla: return "Carla";
            case TTSVoice.it_IT_Giorgio: return "Giorgio";
            case TTSVoice.pl_PL_Ewa: return "Ewa";
            case TTSVoice.pl_PL_Maja: return "Maja";
            case TTSVoice.pl_PL_Jacek: return "Jacek";
            case TTSVoice.pl_PL_Jan: return "Jan";
            case TTSVoice.pt_BR_Vitoria: return "Vitoria";
            case TTSVoice.pt_BR_Camila: return "Camila";
            case TTSVoice.pt_BR_Ricardo: return "Ricardo";
            case TTSVoice.ru_RU_Tatyana: return "Tatyana";
            case TTSVoice.ru_RU_Maxim: return "Maxim";
            case TTSVoice.es_ES_Lucia: return "Lucia";
            case TTSVoice.es_ES_Conchita: return "Conchita";
            case TTSVoice.es_ES_Enrique: return "Enrique";
            case TTSVoice.es_MX_Mia: return "Mia";
            case TTSVoice.es_US_Penelope: return "Penelope";
            case TTSVoice.es_US_Lupe: return "Lupe";
            case TTSVoice.es_US_Miguel: return "Miguel";
            case TTSVoice.tr_TR_Filiz: return "Filiz";
            case TTSVoice.cy_GB_Gwyneth: return "Gwyneth";

            case TTSVoice.en_AU_Catherine: return "Catherine";
            case TTSVoice.en_AU_HayleyRUS: return "Hayley";
            case TTSVoice.en_CA_HeatherRUS: return "Heather";
            case TTSVoice.en_CA_Linda: return "Linda";
            case TTSVoice.en_IN_Heera: return "Heera";
            case TTSVoice.en_IN_PriyaRUS: return "Priya";
            case TTSVoice.en_IN_Ravi: return "Ravi";
            case TTSVoice.en_IE_Sean: return "Sean";
            case TTSVoice.en_GB_George: return "George";
            case TTSVoice.en_GB_HazelRUS: return "Hazel";
            case TTSVoice.en_GB_Susan: return "Susan";
            case TTSVoice.en_US_BenjaminRUS: return "Benjamin";
            case TTSVoice.en_US_GuyRUS: return "Guy";
            case TTSVoice.en_US_AriaRUS: return "Aria";
            case TTSVoice.en_US_ZiraRUS: return "Zira";

            case TTSVoice.en_AU_NatashaNeural: return "NatashaNeural";
            case TTSVoice.en_AU_WilliamNeural: return "WilliamNeural";
            case TTSVoice.en_CA_ClaraNeural: return "ClaraNeural";
            case TTSVoice.en_CA_LiamNeural: return "LiamNeural";
            case TTSVoice.en_HK_YanNeural: return "YanNeural";
            case TTSVoice.en_HK_SamNeural: return "SamNeural";
            case TTSVoice.en_IN_NeerjaNeural: return "NeerjaNeural";
            case TTSVoice.en_IN_PrabhatNeural: return "PrabhatNeural";
            case TTSVoice.en_IE_EmilyNeural: return "EmilyNeural";
            case TTSVoice.en_IE_ConnorNeural: return "ConnorNeural";
            case TTSVoice.en_NZ_MollyNeural: return "MollyNeural";
            case TTSVoice.en_NZ_MitchellNeural: return "MitchellNeural";
            case TTSVoice.en_PH_RosaNeural: return "RosaNeural";
            case TTSVoice.en_PH_JamesNeural: return "JamesNeural";
            case TTSVoice.en_SG_LunaNeural: return "LunaNeural";
            case TTSVoice.en_SG_WayneNeural: return "WayneNeural";
            case TTSVoice.en_ZA_LeahNeural: return "LeahNeural";
            case TTSVoice.en_ZA_LukeNeural: return "LukeNeural";
            case TTSVoice.en_GB_LibbyNeural: return "LibbyNeural";
            case TTSVoice.en_GB_MiaNeural: return "MiaNeural";
            case TTSVoice.en_GB_RyanNeural: return "RyanNeural";
            case TTSVoice.en_US_AriaNeural: return "AriaNeural";
            case TTSVoice.en_US_JennyNeural: return "JennyNeural";
            case TTSVoice.en_US_GuyNeural: return "GuyNeural";

            case TTSVoice.en_AU_OliviaNeural: return "OliviaNeural";
            case TTSVoice.en_GB_AmyNeural: return "AmyNeural";
            case TTSVoice.en_GB_EmmaNeural: return "EmmaNeural";
            case TTSVoice.en_GB_BrianNeural: return "BrianNeural";
            case TTSVoice.en_US_IvyNeural: return "IvyNeural";
            case TTSVoice.en_US_JoannaNeural: return "JoannaNeural";
            case TTSVoice.en_US_KendraNeural: return "KendraNeural";
            case TTSVoice.en_US_KimberlyNeural: return "KimberlyNeural";
            case TTSVoice.en_US_SalliNeural: return "SalliNeural";
            case TTSVoice.en_US_JoeyNeural: return "JoeyNeural";
            case TTSVoice.en_US_JustinNeural: return "JustinNeural";
            case TTSVoice.en_US_KevinNeural: return "KevinNeural";
            case TTSVoice.en_US_MatthewNeural: return "MatthewNeural";

            default:
                BGC.Debug.LogError($"Unsupported TTSVoice {voice}");
                goto case TTSVoice.en_US_Standard_B;
        }
    }

    private static Dictionary<string, TTSVoice>? ttsVoiceLookup = null;

    public static TTSVoice TranslateTTSVoice(this string voiceString)
    {
        if (ttsVoiceLookup is null)
        {
            ttsVoiceLookup = new Dictionary<string, TTSVoice>();

            for (TTSVoice voice = 0; voice < TTSVoice.MAX; voice++)
            {
                ttsVoiceLookup.Add(voice.Serialize().ToLowerInvariant(), voice);
            }
        }

        string cleanedString = voiceString.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(cleanedString))
        {
            return TTSVoice.Unassigned;
        }

        if (ttsVoiceLookup.TryGetValue(cleanedString, out TTSVoice ttsVoice))
        {
            return ttsVoice;
        }

        if (cleanedString == "default")
        {
            return TTSVoice.Unassigned;
        }

        return TTSVoice.MAX;
    }

    public static TTSPitch TranslateTTSPitch(this string pitchString) =>
        pitchString.ToLowerInvariant() switch
        {
            "x-low" => TTSPitch.X_Low,
            "low" => TTSPitch.Low,
            "medium" => TTSPitch.Medium,
            "high" => TTSPitch.High,
            "x-high" => TTSPitch.X_High,
            "default" => TTSPitch.Unassigned,
            "normal" => TTSPitch.Unassigned,
            "unassigned" => TTSPitch.Unassigned,
            _ => TTSPitch.MAX,
        };

    public static TTSSpeed TranslateTTSSpeed(this string speedString) =>
        speedString.ToLowerInvariant() switch
        {
            "x-slow" => TTSSpeed.X_Slow,
            "slow" => TTSSpeed.Slow,
            "medium" => TTSSpeed.Medium,
            "fast" => TTSSpeed.Fast,
            "x-fast" => TTSSpeed.X_Fast,
            "default" => TTSSpeed.Unassigned,
            "normal" => TTSSpeed.Unassigned,
            "unassigned" => TTSSpeed.Unassigned,
            _ => TTSSpeed.MAX,
        };

    public static bool IsNeuralVoice(this TTSVoice voice)
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
            case TTSVoice.en_US_Standard_A:
            case TTSVoice.en_US_Standard_B:
            case TTSVoice.en_US_Standard_C:
            case TTSVoice.en_US_Standard_D:
            case TTSVoice.en_US_Standard_E:
            case TTSVoice.en_US_Standard_F:
            case TTSVoice.en_US_Standard_G:
            case TTSVoice.en_US_Standard_H:
            case TTSVoice.en_US_Standard_I:
            case TTSVoice.en_US_Standard_J:
                return false;

            case TTSVoice.en_AU_Nicole:
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
                return false;

            case TTSVoice.fr_FR_Celine:
            case TTSVoice.fr_FR_Lea:
            case TTSVoice.fr_FR_Mathieu:
            case TTSVoice.fr_CA_Chantal:
            case TTSVoice.de_DE_Marlene:
            case TTSVoice.de_DE_Vicki:
            case TTSVoice.de_DE_Hans:
            case TTSVoice.it_IT_Bianca:
            case TTSVoice.it_IT_Carla:
            case TTSVoice.it_IT_Giorgio:
            case TTSVoice.pl_PL_Ewa:
            case TTSVoice.pl_PL_Maja:
            case TTSVoice.pl_PL_Jacek:
            case TTSVoice.pl_PL_Jan:
            case TTSVoice.pt_BR_Vitoria:
            case TTSVoice.pt_BR_Camila:
            case TTSVoice.pt_BR_Ricardo:
            case TTSVoice.ru_RU_Tatyana:
            case TTSVoice.ru_RU_Maxim:
            case TTSVoice.es_ES_Lucia:
            case TTSVoice.es_ES_Conchita:
            case TTSVoice.es_ES_Enrique:
            case TTSVoice.es_MX_Mia:
            case TTSVoice.es_US_Penelope:
            case TTSVoice.es_US_Lupe:
            case TTSVoice.es_US_Miguel:
            case TTSVoice.tr_TR_Filiz:
            case TTSVoice.cy_GB_Gwyneth:
                return false;

            case TTSVoice.en_AU_Catherine:
            case TTSVoice.en_AU_HayleyRUS:
            case TTSVoice.en_CA_HeatherRUS:
            case TTSVoice.en_CA_Linda:
            case TTSVoice.en_IN_Heera:
            case TTSVoice.en_IN_PriyaRUS:
            case TTSVoice.en_IN_Ravi:
            case TTSVoice.en_IE_Sean:
            case TTSVoice.en_GB_George:
            case TTSVoice.en_GB_HazelRUS:
            case TTSVoice.en_GB_Susan:
            case TTSVoice.en_US_BenjaminRUS:
            case TTSVoice.en_US_GuyRUS:
            case TTSVoice.en_US_AriaRUS:
            case TTSVoice.en_US_ZiraRUS:
                return false;

            //AWS Neural Voices
            case TTSVoice.en_AU_OliviaNeural:
            case TTSVoice.en_GB_AmyNeural:
            case TTSVoice.en_GB_EmmaNeural:
            case TTSVoice.en_GB_BrianNeural:
            case TTSVoice.en_US_IvyNeural:
            case TTSVoice.en_US_JoannaNeural:
            case TTSVoice.en_US_KendraNeural:
            case TTSVoice.en_US_KimberlyNeural:
            case TTSVoice.en_US_SalliNeural:
            case TTSVoice.en_US_JoeyNeural:
            case TTSVoice.en_US_JustinNeural:
            case TTSVoice.en_US_KevinNeural:
            case TTSVoice.en_US_MatthewNeural:
                return true;

            //Google Neural Voices
            case TTSVoice.en_AU_Wavenet_A:
            case TTSVoice.en_AU_Wavenet_B:
            case TTSVoice.en_AU_Wavenet_C:
            case TTSVoice.en_AU_Wavenet_D:
            case TTSVoice.en_IN_Wavenet_A:
            case TTSVoice.en_IN_Wavenet_B:
            case TTSVoice.en_IN_Wavenet_C:
            case TTSVoice.en_IN_Wavenet_D:
            case TTSVoice.en_GB_Wavenet_A:
            case TTSVoice.en_GB_Wavenet_B:
            case TTSVoice.en_GB_Wavenet_C:
            case TTSVoice.en_GB_Wavenet_D:
            case TTSVoice.en_GB_Wavenet_F:
            case TTSVoice.en_US_Wavenet_A:
            case TTSVoice.en_US_Wavenet_B:
            case TTSVoice.en_US_Wavenet_C:
            case TTSVoice.en_US_Wavenet_D:
            case TTSVoice.en_US_Wavenet_E:
            case TTSVoice.en_US_Wavenet_F:
            case TTSVoice.en_US_Wavenet_G:
            case TTSVoice.en_US_Wavenet_H:
            case TTSVoice.en_US_Wavenet_I:
            case TTSVoice.en_US_Wavenet_J:
                return true;

            //Azure Neural Voices
            case TTSVoice.en_AU_NatashaNeural:
            case TTSVoice.en_AU_WilliamNeural:
            case TTSVoice.en_CA_ClaraNeural:
            case TTSVoice.en_CA_LiamNeural:
            case TTSVoice.en_HK_YanNeural:
            case TTSVoice.en_HK_SamNeural:
            case TTSVoice.en_IN_NeerjaNeural:
            case TTSVoice.en_IN_PrabhatNeural:
            case TTSVoice.en_IE_EmilyNeural:
            case TTSVoice.en_IE_ConnorNeural:
            case TTSVoice.en_NZ_MollyNeural:
            case TTSVoice.en_NZ_MitchellNeural:
            case TTSVoice.en_PH_RosaNeural:
            case TTSVoice.en_PH_JamesNeural:
            case TTSVoice.en_SG_LunaNeural:
            case TTSVoice.en_SG_WayneNeural:
            case TTSVoice.en_ZA_LeahNeural:
            case TTSVoice.en_ZA_LukeNeural:
            case TTSVoice.en_GB_LibbyNeural:
            case TTSVoice.en_GB_MiaNeural:
            case TTSVoice.en_GB_RyanNeural:
            case TTSVoice.en_US_AriaNeural:
            case TTSVoice.en_US_JennyNeural:
            case TTSVoice.en_US_GuyNeural:
                return true;

            case TTSVoice.Unassigned: goto case TTSVoice.en_US_Joanna;

            default:
                BGC.Debug.LogError($"Unsupported TTSVoice {voice}");
                goto case TTSVoice.en_US_Standard_B;
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
            case TTSVoice.en_US_Standard_A:
            case TTSVoice.en_US_Standard_B:
            case TTSVoice.en_US_Standard_C:
            case TTSVoice.en_US_Standard_D:
            case TTSVoice.en_US_Standard_E:
            case TTSVoice.en_US_Standard_F:
            case TTSVoice.en_US_Standard_G:
            case TTSVoice.en_US_Standard_H:
            case TTSVoice.en_US_Standard_I:
            case TTSVoice.en_US_Standard_J:
                return TTSService.Google;

            case TTSVoice.en_AU_Wavenet_A:
            case TTSVoice.en_AU_Wavenet_B:
            case TTSVoice.en_AU_Wavenet_C:
            case TTSVoice.en_AU_Wavenet_D:
            case TTSVoice.en_IN_Wavenet_A:
            case TTSVoice.en_IN_Wavenet_B:
            case TTSVoice.en_IN_Wavenet_C:
            case TTSVoice.en_IN_Wavenet_D:
            case TTSVoice.en_GB_Wavenet_A:
            case TTSVoice.en_GB_Wavenet_B:
            case TTSVoice.en_GB_Wavenet_C:
            case TTSVoice.en_GB_Wavenet_D:
            case TTSVoice.en_GB_Wavenet_F:
            case TTSVoice.en_US_Wavenet_A:
            case TTSVoice.en_US_Wavenet_B:
            case TTSVoice.en_US_Wavenet_C:
            case TTSVoice.en_US_Wavenet_D:
            case TTSVoice.en_US_Wavenet_E:
            case TTSVoice.en_US_Wavenet_F:
            case TTSVoice.en_US_Wavenet_G:
            case TTSVoice.en_US_Wavenet_H:
            case TTSVoice.en_US_Wavenet_I:
            case TTSVoice.en_US_Wavenet_J:
                return TTSService.Google;

            case TTSVoice.en_AU_Nicole:
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

            case TTSVoice.fr_FR_Celine:
            case TTSVoice.fr_FR_Lea:
            case TTSVoice.fr_FR_Mathieu:
            case TTSVoice.fr_CA_Chantal:
            case TTSVoice.de_DE_Marlene:
            case TTSVoice.de_DE_Vicki:
            case TTSVoice.de_DE_Hans:
            case TTSVoice.it_IT_Bianca:
            case TTSVoice.it_IT_Carla:
            case TTSVoice.it_IT_Giorgio:
            case TTSVoice.pl_PL_Ewa:
            case TTSVoice.pl_PL_Maja:
            case TTSVoice.pl_PL_Jacek:
            case TTSVoice.pl_PL_Jan:
            case TTSVoice.pt_BR_Vitoria:
            case TTSVoice.pt_BR_Camila:
            case TTSVoice.pt_BR_Ricardo:
            case TTSVoice.ru_RU_Tatyana:
            case TTSVoice.ru_RU_Maxim:
            case TTSVoice.es_ES_Lucia:
            case TTSVoice.es_ES_Conchita:
            case TTSVoice.es_ES_Enrique:
            case TTSVoice.es_MX_Mia:
            case TTSVoice.es_US_Penelope:
            case TTSVoice.es_US_Lupe:
            case TTSVoice.es_US_Miguel:
            case TTSVoice.tr_TR_Filiz:
            case TTSVoice.cy_GB_Gwyneth:
                return TTSService.Amazon;

            case TTSVoice.en_AU_OliviaNeural:
            case TTSVoice.en_GB_AmyNeural:
            case TTSVoice.en_GB_EmmaNeural:
            case TTSVoice.en_GB_BrianNeural:
            case TTSVoice.en_US_IvyNeural:
            case TTSVoice.en_US_JoannaNeural:
            case TTSVoice.en_US_KendraNeural:
            case TTSVoice.en_US_KimberlyNeural:
            case TTSVoice.en_US_SalliNeural:
            case TTSVoice.en_US_JoeyNeural:
            case TTSVoice.en_US_JustinNeural:
            case TTSVoice.en_US_KevinNeural:
            case TTSVoice.en_US_MatthewNeural:
                return TTSService.Amazon;

            case TTSVoice.en_AU_Catherine:
            case TTSVoice.en_AU_HayleyRUS:
            case TTSVoice.en_CA_HeatherRUS:
            case TTSVoice.en_CA_Linda:
            case TTSVoice.en_IN_Heera:
            case TTSVoice.en_IN_PriyaRUS:
            case TTSVoice.en_IN_Ravi:
            case TTSVoice.en_IE_Sean:
            case TTSVoice.en_GB_George:
            case TTSVoice.en_GB_HazelRUS:
            case TTSVoice.en_GB_Susan:
            case TTSVoice.en_US_BenjaminRUS:
            case TTSVoice.en_US_GuyRUS:
            case TTSVoice.en_US_AriaRUS:
            case TTSVoice.en_US_ZiraRUS:
                return TTSService.Azure;

            case TTSVoice.en_AU_NatashaNeural:
            case TTSVoice.en_AU_WilliamNeural:
            case TTSVoice.en_CA_ClaraNeural:
            case TTSVoice.en_CA_LiamNeural:
            case TTSVoice.en_HK_YanNeural:
            case TTSVoice.en_HK_SamNeural:
            case TTSVoice.en_IN_NeerjaNeural:
            case TTSVoice.en_IN_PrabhatNeural:
            case TTSVoice.en_IE_EmilyNeural:
            case TTSVoice.en_IE_ConnorNeural:
            case TTSVoice.en_NZ_MollyNeural:
            case TTSVoice.en_NZ_MitchellNeural:
            case TTSVoice.en_PH_RosaNeural:
            case TTSVoice.en_PH_JamesNeural:
            case TTSVoice.en_SG_LunaNeural:
            case TTSVoice.en_SG_WayneNeural:
            case TTSVoice.en_ZA_LeahNeural:
            case TTSVoice.en_ZA_LukeNeural:
            case TTSVoice.en_GB_LibbyNeural:
            case TTSVoice.en_GB_MiaNeural:
            case TTSVoice.en_GB_RyanNeural:
            case TTSVoice.en_US_AriaNeural:
            case TTSVoice.en_US_JennyNeural:
            case TTSVoice.en_US_GuyNeural:
                return TTSService.Azure;

            case TTSVoice.Unassigned: goto case TTSVoice.en_US_Joanna;

            default:
                BGC.Debug.LogError($"Unsupported TTSVoice {voice}");
                goto case TTSVoice.en_US_Standard_B;
        }
    }

    public static string GetTTSVoiceString(this TTSVoice voice)
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

            case TTSVoice.en_US_Standard_A: return "en-US-Standard-A";
            case TTSVoice.en_US_Standard_B: return "en-US-Standard-B";
            case TTSVoice.en_US_Standard_C: return "en-US-Standard-C";
            case TTSVoice.en_US_Standard_D: return "en-US-Standard-D";
            case TTSVoice.en_US_Standard_E: return "en-US-Standard-E";
            case TTSVoice.en_US_Standard_F: return "en-US-Standard-F";
            case TTSVoice.en_US_Standard_G: return "en-US-Standard-G";
            case TTSVoice.en_US_Standard_H: return "en-US-Standard-H";
            case TTSVoice.en_US_Standard_I: return "en-US-Standard-I";
            case TTSVoice.en_US_Standard_J: return "en-US-Standard-J";

            case TTSVoice.en_AU_Wavenet_A: return "en-AU-Wavenet-A";
            case TTSVoice.en_AU_Wavenet_B: return "en-AU-Wavenet-B";
            case TTSVoice.en_AU_Wavenet_C: return "en-AU-Wavenet-C";
            case TTSVoice.en_AU_Wavenet_D: return "en-AU-Wavenet-D";

            case TTSVoice.en_IN_Wavenet_A: return "en-IN-Wavenet-A";
            case TTSVoice.en_IN_Wavenet_B: return "en-IN-Wavenet-B";
            case TTSVoice.en_IN_Wavenet_C: return "en-IN-Wavenet-C";
            case TTSVoice.en_IN_Wavenet_D: return "en-IN-Wavenet-D";

            case TTSVoice.en_GB_Wavenet_A: return "en-GB-Wavenet-A";
            case TTSVoice.en_GB_Wavenet_B: return "en-GB-Wavenet-B";
            case TTSVoice.en_GB_Wavenet_C: return "en-GB-Wavenet-C";
            case TTSVoice.en_GB_Wavenet_D: return "en-GB-Wavenet-D";
            case TTSVoice.en_GB_Wavenet_F: return "en-GB-Wavenet-F";

            case TTSVoice.en_US_Wavenet_A: return "en-US-Wavenet-A";
            case TTSVoice.en_US_Wavenet_B: return "en-US-Wavenet-B";
            case TTSVoice.en_US_Wavenet_C: return "en-US-Wavenet-C";
            case TTSVoice.en_US_Wavenet_D: return "en-US-Wavenet-D";
            case TTSVoice.en_US_Wavenet_E: return "en-US-Wavenet-E";
            case TTSVoice.en_US_Wavenet_F: return "en-US-Wavenet-F";
            case TTSVoice.en_US_Wavenet_G: return "en-US-Wavenet-G";
            case TTSVoice.en_US_Wavenet_H: return "en-US-Wavenet-H";
            case TTSVoice.en_US_Wavenet_I: return "en-US-Wavenet-I";
            case TTSVoice.en_US_Wavenet_J: return "en-US-Wavenet-J";

            case TTSVoice.en_AU_Nicole:
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
            case TTSVoice.fr_FR_Celine:
            case TTSVoice.fr_FR_Lea:
            case TTSVoice.fr_FR_Mathieu:
            case TTSVoice.fr_CA_Chantal:
            case TTSVoice.de_DE_Marlene:
            case TTSVoice.de_DE_Vicki:
            case TTSVoice.de_DE_Hans:
            case TTSVoice.it_IT_Bianca:
            case TTSVoice.it_IT_Carla:
            case TTSVoice.it_IT_Giorgio:
            case TTSVoice.pl_PL_Ewa:
            case TTSVoice.pl_PL_Maja:
            case TTSVoice.pl_PL_Jacek:
            case TTSVoice.pl_PL_Jan:
            case TTSVoice.pt_BR_Vitoria:
            case TTSVoice.pt_BR_Camila:
            case TTSVoice.pt_BR_Ricardo:
            case TTSVoice.ru_RU_Tatyana:
            case TTSVoice.ru_RU_Maxim:
            case TTSVoice.es_ES_Lucia:
            case TTSVoice.es_ES_Conchita:
            case TTSVoice.es_ES_Enrique:
            case TTSVoice.es_MX_Mia:
            case TTSVoice.es_US_Penelope:
            case TTSVoice.es_US_Lupe:
            case TTSVoice.es_US_Miguel:
            case TTSVoice.tr_TR_Filiz:
            case TTSVoice.cy_GB_Gwyneth:
                BGC.Debug.LogError($"Tried to get Google Voice string from AWS TTS Voice {voice}");
                goto case TTSVoice.en_US_Standard_B;

            case TTSVoice.en_AU_OliviaNeural:
            case TTSVoice.en_GB_AmyNeural:
            case TTSVoice.en_GB_EmmaNeural:
            case TTSVoice.en_GB_BrianNeural:
            case TTSVoice.en_US_IvyNeural:
            case TTSVoice.en_US_JoannaNeural:
            case TTSVoice.en_US_KendraNeural:
            case TTSVoice.en_US_KimberlyNeural:
            case TTSVoice.en_US_SalliNeural:
            case TTSVoice.en_US_JoeyNeural:
            case TTSVoice.en_US_JustinNeural:
            case TTSVoice.en_US_KevinNeural:
            case TTSVoice.en_US_MatthewNeural:
                BGC.Debug.LogError($"Tried to get Google Voice string from AWS TTS Voice {voice}");
                goto case TTSVoice.en_US_Standard_B;

            case TTSVoice.en_AU_Catherine: return "en-AU-Catherine";
            case TTSVoice.en_AU_HayleyRUS: return "en-AU-HayleyRUS";
            case TTSVoice.en_CA_HeatherRUS: return "en-CA-HeatherRUS";
            case TTSVoice.en_CA_Linda: return "en-CA-Linda";
            case TTSVoice.en_IN_Heera: return "en-IN-Heera";
            case TTSVoice.en_IN_PriyaRUS: return "en-IN-PriyaRUS";
            case TTSVoice.en_IN_Ravi: return "en-IN-Ravi";
            case TTSVoice.en_IE_Sean: return "en-IE-Sean";
            case TTSVoice.en_GB_George: return "en-GB-George";
            case TTSVoice.en_GB_HazelRUS: return "en-GB-HazelRUS";
            case TTSVoice.en_GB_Susan: return "en-GB-Susan";
            case TTSVoice.en_US_BenjaminRUS: return "en-US-BenjaminRUS";
            case TTSVoice.en_US_GuyRUS: return "en-US-GuyRUS";
            case TTSVoice.en_US_AriaRUS: return "en-US-AriaRUS";
            case TTSVoice.en_US_ZiraRUS: return "en-US-ZiraRUS";

            case TTSVoice.en_AU_NatashaNeural: return "en-AU-NatashaNeural";
            case TTSVoice.en_AU_WilliamNeural: return "en-AU-WilliamNeural";
            case TTSVoice.en_CA_ClaraNeural: return "en-CA-ClaraNeural";
            case TTSVoice.en_CA_LiamNeural: return "en-CA-LiamNeural";
            case TTSVoice.en_HK_YanNeural: return "en-HK-YanNeural";
            case TTSVoice.en_HK_SamNeural: return "en-HK-SamNeural";
            case TTSVoice.en_IN_NeerjaNeural: return "en-IN-NeerjaNeural";
            case TTSVoice.en_IN_PrabhatNeural: return "en-IN-PrabhatNeural";
            case TTSVoice.en_IE_EmilyNeural: return "en-IE-EmilyNeural";
            case TTSVoice.en_IE_ConnorNeural: return "en-IE-ConnorNeural";
            case TTSVoice.en_NZ_MollyNeural: return "en-NZ-MollyNeural";
            case TTSVoice.en_NZ_MitchellNeural: return "en-NZ-MitchellNeural";
            case TTSVoice.en_PH_RosaNeural: return "en-PH-RosaNeural";
            case TTSVoice.en_PH_JamesNeural: return "en-PH-JamesNeural";
            case TTSVoice.en_SG_LunaNeural: return "en-SG-LunaNeural";
            case TTSVoice.en_SG_WayneNeural: return "en-SG-WayneNeural";
            case TTSVoice.en_ZA_LeahNeural: return "en-ZA-LeahNeural";
            case TTSVoice.en_ZA_LukeNeural: return "en-ZA-LukeNeural";
            case TTSVoice.en_GB_LibbyNeural: return "en-GB-LibbyNeural";
            case TTSVoice.en_GB_MiaNeural: return "en-GB-MiaNeural";
            case TTSVoice.en_GB_RyanNeural: return "en-GB-RyanNeural";
            case TTSVoice.en_US_AriaNeural: return "en-US-AriaNeural";
            case TTSVoice.en_US_JennyNeural: return "en-US-JennyNeural";
            case TTSVoice.en_US_GuyNeural: return "en-US-GuyNeural";

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
            case TTSVoice.en_AU_Wavenet_A:
            case TTSVoice.en_AU_Wavenet_B:
            case TTSVoice.en_AU_Wavenet_C:
            case TTSVoice.en_AU_Wavenet_D:
                return new VoiceSelectionParams
                {
                    Name = voice.GetTTSVoiceString(),
                    LanguageCode = "en-AU",
                    SsmlGender = SsmlVoiceGender.Neutral
                };

            case TTSVoice.en_IN_Standard_A:
            case TTSVoice.en_IN_Standard_B:
            case TTSVoice.en_IN_Standard_C:
            case TTSVoice.en_IN_Standard_D:
            case TTSVoice.en_IN_Wavenet_A:
            case TTSVoice.en_IN_Wavenet_B:
            case TTSVoice.en_IN_Wavenet_C:
            case TTSVoice.en_IN_Wavenet_D:
                return new VoiceSelectionParams
                {
                    Name = voice.GetTTSVoiceString(),
                    LanguageCode = "en-IN",
                    SsmlGender = SsmlVoiceGender.Neutral
                };

            case TTSVoice.en_GB_Standard_A:
            case TTSVoice.en_GB_Standard_B:
            case TTSVoice.en_GB_Standard_C:
            case TTSVoice.en_GB_Standard_D:
            case TTSVoice.en_GB_Standard_F:
            case TTSVoice.en_GB_Wavenet_A:
            case TTSVoice.en_GB_Wavenet_B:
            case TTSVoice.en_GB_Wavenet_C:
            case TTSVoice.en_GB_Wavenet_D:
            case TTSVoice.en_GB_Wavenet_F:
                return new VoiceSelectionParams
                {
                    Name = voice.GetTTSVoiceString(),
                    LanguageCode = "en-GB",
                    SsmlGender = SsmlVoiceGender.Neutral
                };

            case TTSVoice.en_US_Standard_A:
            case TTSVoice.en_US_Standard_B:
            case TTSVoice.en_US_Standard_C:
            case TTSVoice.en_US_Standard_D:
            case TTSVoice.en_US_Standard_E:
            case TTSVoice.en_US_Standard_F:
            case TTSVoice.en_US_Standard_G:
            case TTSVoice.en_US_Standard_H:
            case TTSVoice.en_US_Standard_I:
            case TTSVoice.en_US_Standard_J:
            case TTSVoice.en_US_Wavenet_A:
            case TTSVoice.en_US_Wavenet_B:
            case TTSVoice.en_US_Wavenet_C:
            case TTSVoice.en_US_Wavenet_D:
            case TTSVoice.en_US_Wavenet_E:
            case TTSVoice.en_US_Wavenet_F:
            case TTSVoice.en_US_Wavenet_G:
            case TTSVoice.en_US_Wavenet_H:
            case TTSVoice.en_US_Wavenet_I:
            case TTSVoice.en_US_Wavenet_J:
                return new VoiceSelectionParams
                {
                    Name = voice.GetTTSVoiceString(),
                    LanguageCode = "en-US",
                    SsmlGender = SsmlVoiceGender.Neutral
                };

            case TTSVoice.en_AU_Nicole:
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
            case TTSVoice.fr_FR_Celine:
            case TTSVoice.fr_FR_Lea:
            case TTSVoice.fr_FR_Mathieu:
            case TTSVoice.fr_CA_Chantal:
            case TTSVoice.de_DE_Marlene:
            case TTSVoice.de_DE_Vicki:
            case TTSVoice.de_DE_Hans:
            case TTSVoice.it_IT_Bianca:
            case TTSVoice.it_IT_Carla:
            case TTSVoice.it_IT_Giorgio:
            case TTSVoice.pl_PL_Ewa:
            case TTSVoice.pl_PL_Maja:
            case TTSVoice.pl_PL_Jacek:
            case TTSVoice.pl_PL_Jan:
            case TTSVoice.pt_BR_Vitoria:
            case TTSVoice.pt_BR_Camila:
            case TTSVoice.pt_BR_Ricardo:
            case TTSVoice.ru_RU_Tatyana:
            case TTSVoice.ru_RU_Maxim:
            case TTSVoice.es_ES_Lucia:
            case TTSVoice.es_ES_Conchita:
            case TTSVoice.es_ES_Enrique:
            case TTSVoice.es_MX_Mia:
            case TTSVoice.es_US_Penelope:
            case TTSVoice.es_US_Lupe:
            case TTSVoice.es_US_Miguel:
            case TTSVoice.tr_TR_Filiz:
            case TTSVoice.cy_GB_Gwyneth:
                BGC.Debug.LogError($"Tried to get Google VoiceSelectionParams from AWS TTS Voice {voice}");
                goto case TTSVoice.Unassigned;

            case TTSVoice.en_AU_OliviaNeural:
            case TTSVoice.en_GB_AmyNeural:
            case TTSVoice.en_GB_EmmaNeural:
            case TTSVoice.en_GB_BrianNeural:
            case TTSVoice.en_US_IvyNeural:
            case TTSVoice.en_US_JoannaNeural:
            case TTSVoice.en_US_KendraNeural:
            case TTSVoice.en_US_KimberlyNeural:
            case TTSVoice.en_US_SalliNeural:
            case TTSVoice.en_US_JoeyNeural:
            case TTSVoice.en_US_JustinNeural:
            case TTSVoice.en_US_KevinNeural:
            case TTSVoice.en_US_MatthewNeural:
                BGC.Debug.LogError($"Tried to get Google VoiceSelectionParams from AWS TTS Voice {voice}");
                goto case TTSVoice.Unassigned;

            case TTSVoice.en_AU_Catherine:
            case TTSVoice.en_AU_HayleyRUS:
            case TTSVoice.en_CA_HeatherRUS:
            case TTSVoice.en_CA_Linda:
            case TTSVoice.en_IN_Heera:
            case TTSVoice.en_IN_PriyaRUS:
            case TTSVoice.en_IN_Ravi:
            case TTSVoice.en_IE_Sean:
            case TTSVoice.en_GB_George:
            case TTSVoice.en_GB_HazelRUS:
            case TTSVoice.en_GB_Susan:
            case TTSVoice.en_US_BenjaminRUS:
            case TTSVoice.en_US_GuyRUS:
            case TTSVoice.en_US_AriaRUS:
            case TTSVoice.en_US_ZiraRUS:
                BGC.Debug.LogError($"Tried to get Google VoiceSelectionParams from Azure TTS Voice {voice}");
                goto case TTSVoice.Unassigned;


            case TTSVoice.en_AU_NatashaNeural:
            case TTSVoice.en_AU_WilliamNeural:
            case TTSVoice.en_CA_ClaraNeural:
            case TTSVoice.en_CA_LiamNeural:
            case TTSVoice.en_HK_YanNeural:
            case TTSVoice.en_HK_SamNeural:
            case TTSVoice.en_IN_NeerjaNeural:
            case TTSVoice.en_IN_PrabhatNeural:
            case TTSVoice.en_IE_EmilyNeural:
            case TTSVoice.en_IE_ConnorNeural:
            case TTSVoice.en_NZ_MollyNeural:
            case TTSVoice.en_NZ_MitchellNeural:
            case TTSVoice.en_PH_RosaNeural:
            case TTSVoice.en_PH_JamesNeural:
            case TTSVoice.en_SG_LunaNeural:
            case TTSVoice.en_SG_WayneNeural:
            case TTSVoice.en_ZA_LeahNeural:
            case TTSVoice.en_ZA_LukeNeural:
            case TTSVoice.en_GB_LibbyNeural:
            case TTSVoice.en_GB_MiaNeural:
            case TTSVoice.en_GB_RyanNeural:
            case TTSVoice.en_US_AriaNeural:
            case TTSVoice.en_US_JennyNeural:
            case TTSVoice.en_US_GuyNeural:
                BGC.Debug.LogError($"Tried to get Google VoiceSelectionParams from Azure TTS Voice {voice}");
                goto case TTSVoice.Unassigned;

            default:
                BGC.Debug.LogError($"TTS Voice not supported {voice}");
                goto case TTSVoice.Unassigned;
        }
    }

    //Add to lexicons in startup if necessary
    public static readonly List<string> awsLexicons = new List<string>() { };

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
            case TTSVoice.en_US_Standard_A:
            case TTSVoice.en_US_Standard_B:
            case TTSVoice.en_US_Standard_C:
            case TTSVoice.en_US_Standard_D:
            case TTSVoice.en_US_Standard_E:
            case TTSVoice.en_US_Standard_F:
            case TTSVoice.en_US_Standard_G:
            case TTSVoice.en_US_Standard_H:
            case TTSVoice.en_US_Standard_I:
            case TTSVoice.en_US_Standard_J:
                BGC.Debug.LogError($"Tried to get Amazon VoiceId from Google TTS Voice {voice}");
                goto case TTSVoice.en_GB_Brian;

            case TTSVoice.en_AU_Wavenet_A:
            case TTSVoice.en_AU_Wavenet_B:
            case TTSVoice.en_AU_Wavenet_C:
            case TTSVoice.en_AU_Wavenet_D:
            case TTSVoice.en_IN_Wavenet_A:
            case TTSVoice.en_IN_Wavenet_B:
            case TTSVoice.en_IN_Wavenet_C:
            case TTSVoice.en_IN_Wavenet_D:
            case TTSVoice.en_GB_Wavenet_A:
            case TTSVoice.en_GB_Wavenet_B:
            case TTSVoice.en_GB_Wavenet_C:
            case TTSVoice.en_GB_Wavenet_D:
            case TTSVoice.en_GB_Wavenet_F:
            case TTSVoice.en_US_Wavenet_A:
            case TTSVoice.en_US_Wavenet_B:
            case TTSVoice.en_US_Wavenet_C:
            case TTSVoice.en_US_Wavenet_D:
            case TTSVoice.en_US_Wavenet_E:
            case TTSVoice.en_US_Wavenet_F:
            case TTSVoice.en_US_Wavenet_G:
            case TTSVoice.en_US_Wavenet_H:
            case TTSVoice.en_US_Wavenet_I:
            case TTSVoice.en_US_Wavenet_J:
                BGC.Debug.LogError($"Tried to get Amazon VoiceId from Google TTS Voice {voice}");
                goto case TTSVoice.en_GB_Brian;

            case TTSVoice.en_AU_Catherine:
            case TTSVoice.en_AU_HayleyRUS:
            case TTSVoice.en_CA_HeatherRUS:
            case TTSVoice.en_CA_Linda:
            case TTSVoice.en_IN_Heera:
            case TTSVoice.en_IN_PriyaRUS:
            case TTSVoice.en_IN_Ravi:
            case TTSVoice.en_IE_Sean:
            case TTSVoice.en_GB_George:
            case TTSVoice.en_GB_HazelRUS:
            case TTSVoice.en_GB_Susan:
            case TTSVoice.en_US_BenjaminRUS:
            case TTSVoice.en_US_GuyRUS:
            case TTSVoice.en_US_AriaRUS:
            case TTSVoice.en_US_ZiraRUS:
                BGC.Debug.LogError($"Tried to get Amazon VoiceId from Azure TTS Voice {voice}");
                goto case TTSVoice.en_GB_Brian;

            case TTSVoice.en_AU_NatashaNeural:
            case TTSVoice.en_AU_WilliamNeural:
            case TTSVoice.en_CA_ClaraNeural:
            case TTSVoice.en_CA_LiamNeural:
            case TTSVoice.en_HK_YanNeural:
            case TTSVoice.en_HK_SamNeural:
            case TTSVoice.en_IN_NeerjaNeural:
            case TTSVoice.en_IN_PrabhatNeural:
            case TTSVoice.en_IE_EmilyNeural:
            case TTSVoice.en_IE_ConnorNeural:
            case TTSVoice.en_NZ_MollyNeural:
            case TTSVoice.en_NZ_MitchellNeural:
            case TTSVoice.en_PH_RosaNeural:
            case TTSVoice.en_PH_JamesNeural:
            case TTSVoice.en_SG_LunaNeural:
            case TTSVoice.en_SG_WayneNeural:
            case TTSVoice.en_ZA_LeahNeural:
            case TTSVoice.en_ZA_LukeNeural:
            case TTSVoice.en_GB_LibbyNeural:
            case TTSVoice.en_GB_MiaNeural:
            case TTSVoice.en_GB_RyanNeural:
            case TTSVoice.en_US_AriaNeural:
            case TTSVoice.en_US_JennyNeural:
            case TTSVoice.en_US_GuyNeural:
                BGC.Debug.LogError($"Tried to get Amazon VoiceId from Azure TTS Voice {voice}");
                goto case TTSVoice.en_GB_Brian;

            case TTSVoice.en_AU_Nicole:
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

            case TTSVoice.fr_FR_Celine:
                synthesisRequest.VoiceId = VoiceId.Celine;
                break;

            case TTSVoice.fr_FR_Lea:
                synthesisRequest.VoiceId = VoiceId.Lea;
                break;

            case TTSVoice.fr_FR_Mathieu:
                synthesisRequest.VoiceId = VoiceId.Mathieu;
                break;

            case TTSVoice.fr_CA_Chantal:
                synthesisRequest.VoiceId = VoiceId.Chantal;
                break;

            case TTSVoice.de_DE_Marlene:
                synthesisRequest.VoiceId = VoiceId.Marlene;
                break;

            case TTSVoice.de_DE_Vicki:
                synthesisRequest.VoiceId = VoiceId.Vicki;
                break;

            case TTSVoice.de_DE_Hans:
                synthesisRequest.VoiceId = VoiceId.Hans;
                break;

            case TTSVoice.it_IT_Bianca:
                synthesisRequest.VoiceId = VoiceId.Bianca;
                break;

            case TTSVoice.it_IT_Carla:
                synthesisRequest.VoiceId = VoiceId.Carla;
                break;

            case TTSVoice.it_IT_Giorgio:
                synthesisRequest.VoiceId = VoiceId.Giorgio;
                break;

            case TTSVoice.pl_PL_Ewa:
                synthesisRequest.VoiceId = VoiceId.Ewa;
                break;

            case TTSVoice.pl_PL_Maja:
                synthesisRequest.VoiceId = VoiceId.Maja;
                break;

            case TTSVoice.pl_PL_Jacek:
                synthesisRequest.VoiceId = VoiceId.Jacek;
                break;

            case TTSVoice.pl_PL_Jan:
                synthesisRequest.VoiceId = VoiceId.Jan;
                break;

            case TTSVoice.pt_BR_Vitoria:
                synthesisRequest.VoiceId = VoiceId.Vitoria;
                break;

            case TTSVoice.pt_BR_Camila:
                synthesisRequest.VoiceId = VoiceId.Camila;
                break;

            case TTSVoice.pt_BR_Ricardo:
                synthesisRequest.VoiceId = VoiceId.Ricardo;
                break;

            case TTSVoice.ru_RU_Tatyana:
                synthesisRequest.VoiceId = VoiceId.Tatyana;
                break;

            case TTSVoice.ru_RU_Maxim:
                synthesisRequest.VoiceId = VoiceId.Maxim;
                break;

            case TTSVoice.es_ES_Lucia:
                synthesisRequest.VoiceId = VoiceId.Lucia;
                break;

            case TTSVoice.es_ES_Conchita:
                synthesisRequest.VoiceId = VoiceId.Conchita;
                break;

            case TTSVoice.es_ES_Enrique:
                synthesisRequest.VoiceId = VoiceId.Enrique;
                break;

            case TTSVoice.es_MX_Mia:
                synthesisRequest.VoiceId = VoiceId.Mia;
                break;

            case TTSVoice.es_US_Penelope:
                synthesisRequest.VoiceId = VoiceId.Penelope;
                break;

            case TTSVoice.es_US_Lupe:
                synthesisRequest.VoiceId = VoiceId.Lupe;
                break;

            case TTSVoice.es_US_Miguel:
                synthesisRequest.VoiceId = VoiceId.Miguel;
                break;

            case TTSVoice.tr_TR_Filiz:
                synthesisRequest.VoiceId = VoiceId.Filiz;
                break;

            case TTSVoice.cy_GB_Gwyneth:
                synthesisRequest.VoiceId = VoiceId.Gwyneth;
                break;

            //AWS Neural Voices
            case TTSVoice.en_AU_OliviaNeural:
                synthesisRequest.VoiceId = VoiceId.Olivia;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case TTSVoice.en_GB_AmyNeural:
                synthesisRequest.VoiceId = VoiceId.Amy;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case TTSVoice.en_GB_EmmaNeural:
                synthesisRequest.VoiceId = VoiceId.Emma;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case TTSVoice.en_GB_BrianNeural:
                synthesisRequest.VoiceId = VoiceId.Brian;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case TTSVoice.en_US_IvyNeural:
                synthesisRequest.VoiceId = VoiceId.Ivy;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case TTSVoice.en_US_JoannaNeural:
                synthesisRequest.VoiceId = VoiceId.Joanna;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case TTSVoice.en_US_KendraNeural:
                synthesisRequest.VoiceId = VoiceId.Kendra;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case TTSVoice.en_US_KimberlyNeural:
                synthesisRequest.VoiceId = VoiceId.Kimberly;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case TTSVoice.en_US_SalliNeural:
                synthesisRequest.VoiceId = VoiceId.Salli;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case TTSVoice.en_US_JoeyNeural:
                synthesisRequest.VoiceId = VoiceId.Joey;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case TTSVoice.en_US_JustinNeural:
                synthesisRequest.VoiceId = VoiceId.Justin;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case TTSVoice.en_US_KevinNeural:
                synthesisRequest.VoiceId = VoiceId.Kevin;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case TTSVoice.en_US_MatthewNeural:
                synthesisRequest.VoiceId = VoiceId.Matthew;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case TTSVoice.Unassigned: goto case TTSVoice.en_US_Joanna;

            default:
                BGC.Debug.LogError($"TTS Voice not supported {voice}");
                goto case TTSVoice.Unassigned;
        }

        return synthesisRequest;
    }

    public static bool GetRequiresLangTag(this TTSVoice voice)
    {
        switch (voice)
        {
            case TTSVoice.fr_FR_Celine:
            case TTSVoice.fr_FR_Lea:
            case TTSVoice.fr_FR_Mathieu:
            case TTSVoice.fr_CA_Chantal:
            case TTSVoice.de_DE_Marlene:
            case TTSVoice.de_DE_Vicki:
            case TTSVoice.de_DE_Hans:
            case TTSVoice.it_IT_Bianca:
            case TTSVoice.it_IT_Carla:
            case TTSVoice.it_IT_Giorgio:
            case TTSVoice.pl_PL_Ewa:
            case TTSVoice.pl_PL_Maja:
            case TTSVoice.pl_PL_Jacek:
            case TTSVoice.pl_PL_Jan:
            case TTSVoice.pt_BR_Vitoria:
            case TTSVoice.pt_BR_Camila:
            case TTSVoice.pt_BR_Ricardo:
            case TTSVoice.ru_RU_Tatyana:
            case TTSVoice.ru_RU_Maxim:
            case TTSVoice.es_ES_Lucia:
            case TTSVoice.es_ES_Conchita:
            case TTSVoice.es_ES_Enrique:
            case TTSVoice.es_MX_Mia:
            case TTSVoice.es_US_Penelope:
            case TTSVoice.es_US_Lupe:
            case TTSVoice.es_US_Miguel:
            case TTSVoice.tr_TR_Filiz:
            case TTSVoice.cy_GB_Gwyneth:
                return true;

            default:
                return false;
        }
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

    public static double GetGoogleSpeed(this TTSSpeed speed)
    {
        switch (speed)
        {
            case TTSSpeed.X_Slow: return 0.5;
            case TTSSpeed.Slow: return 0.75;
            case TTSSpeed.Medium: return 1.0;
            case TTSSpeed.Fast: return 1.5;
            case TTSSpeed.X_Fast: return 2.0;

            case TTSSpeed.Unassigned:
                goto case TTSSpeed.Medium;

            default:
                BGC.Debug.LogError($"TTS Speed not supported {speed}");
                goto case TTSSpeed.Unassigned;
        }
    }


    public static string GetPitchShift(this TTSPitch pitch)
    {
        switch (pitch)
        {
            case TTSPitch.X_Low: return "x-low";
            case TTSPitch.Low: return "low";
            case TTSPitch.Medium: return "medium";
            case TTSPitch.High: return "high";
            case TTSPitch.X_High: return "x-high";

            case TTSPitch.Unassigned:
                goto case TTSPitch.Medium;

            default:
                BGC.Debug.LogError($"TTS Pitch not supported {pitch}");
                goto case TTSPitch.Unassigned;
        }
    }

    public static string GetSpeedValue(this TTSSpeed speed)
    {
        switch (speed)
        {
            case TTSSpeed.X_Slow: return "x-slow";
            case TTSSpeed.Slow: return "slow";
            case TTSSpeed.Medium: return "medium";
            case TTSSpeed.Fast: return "fast";
            case TTSSpeed.X_Fast: return "x-fast";

            case TTSSpeed.Unassigned:
                goto case TTSSpeed.Medium;

            default:
                BGC.Debug.LogError($"TTS Speed not supported {speed}");
                goto case TTSSpeed.Unassigned;
        }
    }

    public static string WrapAmazonProsody(this string text, TTSPitch pitch, TTSSpeed speed)
    {
        switch (pitch)
        {
            case TTSPitch.X_Low:
            case TTSPitch.Low:
            case TTSPitch.Medium:
            case TTSPitch.High:
            case TTSPitch.X_High:
                //proceed
                break;

            case TTSPitch.Unassigned:
                pitch = TTSPitch.Medium;
                break;

            default:
                BGC.Debug.LogError($"TTS Pitch not supported {pitch}");
                goto case TTSPitch.Unassigned;
        }

        switch (speed)
        {
            case TTSSpeed.X_Slow:
            case TTSSpeed.Slow:
            case TTSSpeed.Medium:
            case TTSSpeed.Fast:
            case TTSSpeed.X_Fast:
                //proceed
                break;

            case TTSSpeed.Unassigned:
                speed = TTSSpeed.Medium;
                break;

            default:
                BGC.Debug.LogError($"TTS Speed not supported {speed}");
                goto case TTSSpeed.Unassigned;
        }

        if (pitch == TTSPitch.Medium && speed == TTSSpeed.Medium)
        {
            return text;
        }

        return $"<prosody pitch=\"{pitch.GetPitchShift()}\" rate=\"{speed.GetSpeedValue()}\">{text}</prosody>";
    }
}
