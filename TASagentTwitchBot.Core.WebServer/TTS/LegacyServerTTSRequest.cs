using TASagentTwitchBot.Core.TTS;

namespace TASagentTwitchBot.Core.WebServer.TTS;

[Obsolete("Included for backwards compatibility")]
public record LegacyServerTTSRequest(
    string RequestIdentifier,
    string Ssml,
    LegacyTTSVoice Voice,
    TTSPitch Pitch,
    TTSSpeed Speed);


[Obsolete("Included for backwards compatibility")]
public enum LegacyTTSVoice
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

public static class LegacyTTSVoiceExtensions
{
    [Obsolete("Included for backwards compatibility")]
    public static string Serialize(this LegacyTTSVoice voice)
    {
        switch (voice)
        {
            case LegacyTTSVoice.Unassigned: return "Unassigned";

            case LegacyTTSVoice.en_AU_Standard_A: return "en-AU-Standard-A";
            case LegacyTTSVoice.en_AU_Standard_B: return "en-AU-Standard-B";
            case LegacyTTSVoice.en_AU_Standard_C: return "en-AU-Standard-C";
            case LegacyTTSVoice.en_AU_Standard_D: return "en-AU-Standard-D";
            case LegacyTTSVoice.en_IN_Standard_A: return "en-IN-Standard-A";
            case LegacyTTSVoice.en_IN_Standard_B: return "en-IN-Standard-B";
            case LegacyTTSVoice.en_IN_Standard_C: return "en-IN-Standard-C";
            case LegacyTTSVoice.en_IN_Standard_D: return "en-IN-Standard-D";
            case LegacyTTSVoice.en_GB_Standard_A: return "en-GB-Standard-A";
            case LegacyTTSVoice.en_GB_Standard_B: return "en-GB-Standard-B";
            case LegacyTTSVoice.en_GB_Standard_C: return "en-GB-Standard-C";
            case LegacyTTSVoice.en_GB_Standard_D: return "en-GB-Standard-D";
            case LegacyTTSVoice.en_GB_Standard_F: return "en-GB-Standard-F";
            case LegacyTTSVoice.en_US_Standard_A: return "en-US-Standard-A";
            case LegacyTTSVoice.en_US_Standard_B: return "en-US-Standard-B";
            case LegacyTTSVoice.en_US_Standard_C: return "en-US-Standard-C";
            case LegacyTTSVoice.en_US_Standard_D: return "en-US-Standard-D";
            case LegacyTTSVoice.en_US_Standard_E: return "en-US-Standard-E";
            case LegacyTTSVoice.en_US_Standard_F: return "en-US-Standard-F";
            case LegacyTTSVoice.en_US_Standard_G: return "en-US-Standard-G";
            case LegacyTTSVoice.en_US_Standard_H: return "en-US-Standard-H";
            case LegacyTTSVoice.en_US_Standard_I: return "en-US-Standard-I";
            case LegacyTTSVoice.en_US_Standard_J: return "en-US-Standard-J";

            case LegacyTTSVoice.en_AU_Wavenet_A: return "en-AU-Wavenet-A";
            case LegacyTTSVoice.en_AU_Wavenet_B: return "en-AU-Wavenet-B";
            case LegacyTTSVoice.en_AU_Wavenet_C: return "en-AU-Wavenet-C";
            case LegacyTTSVoice.en_AU_Wavenet_D: return "en-AU-Wavenet-D";
            case LegacyTTSVoice.en_IN_Wavenet_A: return "en-IN-Wavenet-A";
            case LegacyTTSVoice.en_IN_Wavenet_B: return "en-IN-Wavenet-B";
            case LegacyTTSVoice.en_IN_Wavenet_C: return "en-IN-Wavenet-C";
            case LegacyTTSVoice.en_IN_Wavenet_D: return "en-IN-Wavenet-D";
            case LegacyTTSVoice.en_GB_Wavenet_A: return "en-GB-Wavenet-A";
            case LegacyTTSVoice.en_GB_Wavenet_B: return "en-GB-Wavenet-B";
            case LegacyTTSVoice.en_GB_Wavenet_C: return "en-GB-Wavenet-C";
            case LegacyTTSVoice.en_GB_Wavenet_D: return "en-GB-Wavenet-D";
            case LegacyTTSVoice.en_GB_Wavenet_F: return "en-GB-Wavenet-F";
            case LegacyTTSVoice.en_US_Wavenet_A: return "en-US-Wavenet-A";
            case LegacyTTSVoice.en_US_Wavenet_B: return "en-US-Wavenet-B";
            case LegacyTTSVoice.en_US_Wavenet_C: return "en-US-Wavenet-C";
            case LegacyTTSVoice.en_US_Wavenet_D: return "en-US-Wavenet-D";
            case LegacyTTSVoice.en_US_Wavenet_E: return "en-US-Wavenet-E";
            case LegacyTTSVoice.en_US_Wavenet_F: return "en-US-Wavenet-F";
            case LegacyTTSVoice.en_US_Wavenet_G: return "en-US-Wavenet-G";
            case LegacyTTSVoice.en_US_Wavenet_H: return "en-US-Wavenet-H";
            case LegacyTTSVoice.en_US_Wavenet_I: return "en-US-Wavenet-I";
            case LegacyTTSVoice.en_US_Wavenet_J: return "en-US-Wavenet-J";

            case LegacyTTSVoice.en_AU_Nicole: return "Nicole";
            case LegacyTTSVoice.en_AU_Russell: return "Russell";
            case LegacyTTSVoice.en_GB_Amy: return "Amy";
            case LegacyTTSVoice.en_GB_Emma: return "Emma";
            case LegacyTTSVoice.en_GB_Brian: return "Brian";
            case LegacyTTSVoice.en_IN_Aditi: return "Aditi";
            case LegacyTTSVoice.en_IN_Raveena: return "Raveena";
            case LegacyTTSVoice.en_US_Ivy: return "Ivy";
            case LegacyTTSVoice.en_US_Joanna: return "Joanna";
            case LegacyTTSVoice.en_US_Kendra: return "Kendra";
            case LegacyTTSVoice.en_US_Kimberly: return "Kimberly";
            case LegacyTTSVoice.en_US_Salli: return "Salli";
            case LegacyTTSVoice.en_US_Joey: return "Joey";
            case LegacyTTSVoice.en_US_Justin: return "Justin";
            case LegacyTTSVoice.en_US_Matthew: return "Matthew";
            case LegacyTTSVoice.en_GB_WLS_Geraint: return "Geraint";

            case LegacyTTSVoice.fr_FR_Celine: return "Celine";
            case LegacyTTSVoice.fr_FR_Lea: return "Lea";
            case LegacyTTSVoice.fr_FR_Mathieu: return "Mathieu";
            case LegacyTTSVoice.fr_CA_Chantal: return "Chantal";
            case LegacyTTSVoice.de_DE_Marlene: return "Marlene";
            case LegacyTTSVoice.de_DE_Vicki: return "Vicki";
            case LegacyTTSVoice.de_DE_Hans: return "Hans";
            case LegacyTTSVoice.it_IT_Bianca: return "Bianca";
            case LegacyTTSVoice.it_IT_Carla: return "Carla";
            case LegacyTTSVoice.it_IT_Giorgio: return "Giorgio";
            case LegacyTTSVoice.pl_PL_Ewa: return "Ewa";
            case LegacyTTSVoice.pl_PL_Maja: return "Maja";
            case LegacyTTSVoice.pl_PL_Jacek: return "Jacek";
            case LegacyTTSVoice.pl_PL_Jan: return "Jan";
            case LegacyTTSVoice.pt_BR_Vitoria: return "Vitoria";
            case LegacyTTSVoice.pt_BR_Camila: return "Camila";
            case LegacyTTSVoice.pt_BR_Ricardo: return "Ricardo";
            case LegacyTTSVoice.ru_RU_Tatyana: return "Tatyana";
            case LegacyTTSVoice.ru_RU_Maxim: return "Maxim";
            case LegacyTTSVoice.es_ES_Lucia: return "Lucia";
            case LegacyTTSVoice.es_ES_Conchita: return "Conchita";
            case LegacyTTSVoice.es_ES_Enrique: return "Enrique";
            case LegacyTTSVoice.es_MX_Mia: return "Mia";
            case LegacyTTSVoice.es_US_Penelope: return "Penelope";
            case LegacyTTSVoice.es_US_Lupe: return "Lupe";
            case LegacyTTSVoice.es_US_Miguel: return "Miguel";
            case LegacyTTSVoice.tr_TR_Filiz: return "Filiz";
            case LegacyTTSVoice.cy_GB_Gwyneth: return "Gwyneth";

            case LegacyTTSVoice.en_AU_Catherine: return "Catherine";
            case LegacyTTSVoice.en_AU_HayleyRUS: return "Hayley";
            case LegacyTTSVoice.en_CA_HeatherRUS: return "Heather";
            case LegacyTTSVoice.en_CA_Linda: return "Linda";
            case LegacyTTSVoice.en_IN_Heera: return "Heera";
            case LegacyTTSVoice.en_IN_PriyaRUS: return "Priya";
            case LegacyTTSVoice.en_IN_Ravi: return "Ravi";
            case LegacyTTSVoice.en_IE_Sean: return "Sean";
            case LegacyTTSVoice.en_GB_George: return "George";
            case LegacyTTSVoice.en_GB_HazelRUS: return "Hazel";
            case LegacyTTSVoice.en_GB_Susan: return "Susan";
            case LegacyTTSVoice.en_US_BenjaminRUS: return "Benjamin";
            case LegacyTTSVoice.en_US_GuyRUS: return "Guy";
            case LegacyTTSVoice.en_US_AriaRUS: return "Aria";
            case LegacyTTSVoice.en_US_ZiraRUS: return "Zira";

            case LegacyTTSVoice.en_AU_NatashaNeural: return "NatashaNeural";
            case LegacyTTSVoice.en_AU_WilliamNeural: return "WilliamNeural";
            case LegacyTTSVoice.en_CA_ClaraNeural: return "ClaraNeural";
            case LegacyTTSVoice.en_CA_LiamNeural: return "LiamNeural";
            case LegacyTTSVoice.en_HK_YanNeural: return "YanNeural";
            case LegacyTTSVoice.en_HK_SamNeural: return "SamNeural";
            case LegacyTTSVoice.en_IN_NeerjaNeural: return "NeerjaNeural";
            case LegacyTTSVoice.en_IN_PrabhatNeural: return "PrabhatNeural";
            case LegacyTTSVoice.en_IE_EmilyNeural: return "EmilyNeural";
            case LegacyTTSVoice.en_IE_ConnorNeural: return "ConnorNeural";
            case LegacyTTSVoice.en_NZ_MollyNeural: return "MollyNeural";
            case LegacyTTSVoice.en_NZ_MitchellNeural: return "MitchellNeural";
            case LegacyTTSVoice.en_PH_RosaNeural: return "RosaNeural";
            case LegacyTTSVoice.en_PH_JamesNeural: return "JamesNeural";
            case LegacyTTSVoice.en_SG_LunaNeural: return "LunaNeural";
            case LegacyTTSVoice.en_SG_WayneNeural: return "WayneNeural";
            case LegacyTTSVoice.en_ZA_LeahNeural: return "LeahNeural";
            case LegacyTTSVoice.en_ZA_LukeNeural: return "LukeNeural";
            case LegacyTTSVoice.en_GB_LibbyNeural: return "LibbyNeural";
            case LegacyTTSVoice.en_GB_MiaNeural: return "MiaNeural";
            case LegacyTTSVoice.en_GB_RyanNeural: return "RyanNeural";
            case LegacyTTSVoice.en_US_AriaNeural: return "AriaNeural";
            case LegacyTTSVoice.en_US_JennyNeural: return "JennyNeural";
            case LegacyTTSVoice.en_US_GuyNeural: return "GuyNeural";

            case LegacyTTSVoice.en_AU_OliviaNeural: return "OliviaNeural";
            case LegacyTTSVoice.en_GB_AmyNeural: return "AmyNeural";
            case LegacyTTSVoice.en_GB_EmmaNeural: return "EmmaNeural";
            case LegacyTTSVoice.en_GB_BrianNeural: return "BrianNeural";
            case LegacyTTSVoice.en_US_IvyNeural: return "IvyNeural";
            case LegacyTTSVoice.en_US_JoannaNeural: return "JoannaNeural";
            case LegacyTTSVoice.en_US_KendraNeural: return "KendraNeural";
            case LegacyTTSVoice.en_US_KimberlyNeural: return "KimberlyNeural";
            case LegacyTTSVoice.en_US_SalliNeural: return "SalliNeural";
            case LegacyTTSVoice.en_US_JoeyNeural: return "JoeyNeural";
            case LegacyTTSVoice.en_US_JustinNeural: return "JustinNeural";
            case LegacyTTSVoice.en_US_KevinNeural: return "KevinNeural";
            case LegacyTTSVoice.en_US_MatthewNeural: return "MatthewNeural";

            default:
                BGC.Debug.LogError($"Unsupported TTSVoice {voice}");
                goto case LegacyTTSVoice.en_US_Standard_B;
        }
    }
}