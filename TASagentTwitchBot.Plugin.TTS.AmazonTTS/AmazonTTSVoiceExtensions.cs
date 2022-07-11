using Amazon.Polly;
using Amazon.Polly.Model;

namespace TASagentTwitchBot.Plugin.TTS.AmazonTTS;

public static class AmazonTTSVoiceExtensions
{
    //Add to lexicons in startup if necessary
    public static readonly List<string> awsLexicons = new List<string>() { };
    private static Dictionary<string, AmazonTTSVoice>? ttsVoiceLookup = null;



    public static string Serialize(this AmazonTTSVoice voice)
    {
        switch (voice)
        {
            case AmazonTTSVoice.en_AU_Nicole: return "Nicole";
            case AmazonTTSVoice.en_AU_Russell: return "Russell";
            case AmazonTTSVoice.en_GB_Amy: return "Amy";
            case AmazonTTSVoice.en_GB_Emma: return "Emma";
            case AmazonTTSVoice.en_GB_Brian: return "Brian";
            case AmazonTTSVoice.en_IN_Aditi: return "Aditi";
            case AmazonTTSVoice.en_IN_Raveena: return "Raveena";
            case AmazonTTSVoice.en_US_Ivy: return "Ivy";
            case AmazonTTSVoice.en_US_Joanna: return "Joanna";
            case AmazonTTSVoice.en_US_Kendra: return "Kendra";
            case AmazonTTSVoice.en_US_Kimberly: return "Kimberly";
            case AmazonTTSVoice.en_US_Salli: return "Salli";
            case AmazonTTSVoice.en_US_Joey: return "Joey";
            case AmazonTTSVoice.en_US_Justin: return "Justin";
            case AmazonTTSVoice.en_US_Matthew: return "Matthew";
            case AmazonTTSVoice.en_GB_WLS_Geraint: return "Geraint";

            case AmazonTTSVoice.fr_FR_Celine: return "Celine";
            case AmazonTTSVoice.fr_FR_Lea: return "Lea";
            case AmazonTTSVoice.fr_FR_Mathieu: return "Mathieu";
            case AmazonTTSVoice.fr_CA_Chantal: return "Chantal";
            case AmazonTTSVoice.de_DE_Marlene: return "Marlene";
            case AmazonTTSVoice.de_DE_Vicki: return "Vicki";
            case AmazonTTSVoice.de_DE_Hans: return "Hans";
            case AmazonTTSVoice.it_IT_Bianca: return "Bianca";
            case AmazonTTSVoice.it_IT_Carla: return "Carla";
            case AmazonTTSVoice.it_IT_Giorgio: return "Giorgio";
            case AmazonTTSVoice.pl_PL_Ewa: return "Ewa";
            case AmazonTTSVoice.pl_PL_Maja: return "Maja";
            case AmazonTTSVoice.pl_PL_Jacek: return "Jacek";
            case AmazonTTSVoice.pl_PL_Jan: return "Jan";
            case AmazonTTSVoice.pt_BR_Vitoria: return "Vitoria";
            case AmazonTTSVoice.pt_BR_Camila: return "Camila";
            case AmazonTTSVoice.pt_BR_Ricardo: return "Ricardo";
            case AmazonTTSVoice.ru_RU_Tatyana: return "Tatyana";
            case AmazonTTSVoice.ru_RU_Maxim: return "Maxim";
            case AmazonTTSVoice.es_ES_Lucia: return "Lucia";
            case AmazonTTSVoice.es_ES_Conchita: return "Conchita";
            case AmazonTTSVoice.es_ES_Enrique: return "Enrique";
            case AmazonTTSVoice.es_MX_Mia: return "Mia";
            case AmazonTTSVoice.es_US_Penelope: return "Penelope";
            case AmazonTTSVoice.es_US_Lupe: return "Lupe";
            case AmazonTTSVoice.es_US_Miguel: return "Miguel";
            case AmazonTTSVoice.tr_TR_Filiz: return "Filiz";
            case AmazonTTSVoice.cy_GB_Gwyneth: return "Gwyneth";


            case AmazonTTSVoice.en_AU_OliviaNeural: return "OliviaNeural";
            case AmazonTTSVoice.en_GB_AmyNeural: return "AmyNeural";
            case AmazonTTSVoice.en_GB_EmmaNeural: return "EmmaNeural";
            case AmazonTTSVoice.en_GB_BrianNeural: return "BrianNeural";
            case AmazonTTSVoice.en_US_IvyNeural: return "IvyNeural";
            case AmazonTTSVoice.en_US_JoannaNeural: return "JoannaNeural";
            case AmazonTTSVoice.en_US_KendraNeural: return "KendraNeural";
            case AmazonTTSVoice.en_US_KimberlyNeural: return "KimberlyNeural";
            case AmazonTTSVoice.en_US_SalliNeural: return "SalliNeural";
            case AmazonTTSVoice.en_US_JoeyNeural: return "JoeyNeural";
            case AmazonTTSVoice.en_US_JustinNeural: return "JustinNeural";
            case AmazonTTSVoice.en_US_KevinNeural: return "KevinNeural";
            case AmazonTTSVoice.en_US_MatthewNeural: return "MatthewNeural";

            default:
                BGC.Debug.LogError($"Unsupported AmazonTTSVoice {voice}");
                goto case AmazonTTSVoice.en_US_Joanna;
        }
    }

    public static AmazonTTSVoice SafeTranslateAmazonTTSVoice(this string voiceString)
    {
        AmazonTTSVoice voice = voiceString.TranslateAmazonTTSVoice();

        if (voice == AmazonTTSVoice.MAX)
        {
            return AmazonTTSVoice.en_US_Joanna;
        }

        return voice;
    }

    public static AmazonTTSVoice TranslateAmazonTTSVoice(this string voiceString)
    {
        if (ttsVoiceLookup is null)
        {
            ttsVoiceLookup = new Dictionary<string, AmazonTTSVoice>();

            for (AmazonTTSVoice voice = 0; voice < AmazonTTSVoice.MAX; voice++)
            {
                ttsVoiceLookup.Add(voice.Serialize().ToLowerInvariant(), voice);
            }
        }

        if (string.IsNullOrEmpty(voiceString))
        {
            return AmazonTTSVoice.en_US_Joanna;
        }

        string cleanedString = voiceString.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(cleanedString))
        {
            return AmazonTTSVoice.en_US_Joanna;
        }

        if (ttsVoiceLookup.TryGetValue(cleanedString, out AmazonTTSVoice ttsVoice))
        {
            return ttsVoice;
        }

        if (cleanedString == "default" || cleanedString == "unassigned")
        {
            return AmazonTTSVoice.en_US_Joanna;
        }

        return AmazonTTSVoice.MAX;
    }


    public static bool IsNeuralVoice(this AmazonTTSVoice voice)
    {
        switch (voice)
        {
            case AmazonTTSVoice.en_AU_Nicole:
            case AmazonTTSVoice.en_AU_Russell:
            case AmazonTTSVoice.en_GB_Amy:
            case AmazonTTSVoice.en_GB_Emma:
            case AmazonTTSVoice.en_GB_Brian:
            case AmazonTTSVoice.en_IN_Aditi:
            case AmazonTTSVoice.en_IN_Raveena:
            case AmazonTTSVoice.en_US_Ivy:
            case AmazonTTSVoice.en_US_Joanna:
            case AmazonTTSVoice.en_US_Kendra:
            case AmazonTTSVoice.en_US_Kimberly:
            case AmazonTTSVoice.en_US_Salli:
            case AmazonTTSVoice.en_US_Joey:
            case AmazonTTSVoice.en_US_Justin:
            case AmazonTTSVoice.en_US_Matthew:
            case AmazonTTSVoice.en_GB_WLS_Geraint:
                return false;

            case AmazonTTSVoice.fr_FR_Celine:
            case AmazonTTSVoice.fr_FR_Lea:
            case AmazonTTSVoice.fr_FR_Mathieu:
            case AmazonTTSVoice.fr_CA_Chantal:
            case AmazonTTSVoice.de_DE_Marlene:
            case AmazonTTSVoice.de_DE_Vicki:
            case AmazonTTSVoice.de_DE_Hans:
            case AmazonTTSVoice.it_IT_Bianca:
            case AmazonTTSVoice.it_IT_Carla:
            case AmazonTTSVoice.it_IT_Giorgio:
            case AmazonTTSVoice.pl_PL_Ewa:
            case AmazonTTSVoice.pl_PL_Maja:
            case AmazonTTSVoice.pl_PL_Jacek:
            case AmazonTTSVoice.pl_PL_Jan:
            case AmazonTTSVoice.pt_BR_Vitoria:
            case AmazonTTSVoice.pt_BR_Camila:
            case AmazonTTSVoice.pt_BR_Ricardo:
            case AmazonTTSVoice.ru_RU_Tatyana:
            case AmazonTTSVoice.ru_RU_Maxim:
            case AmazonTTSVoice.es_ES_Lucia:
            case AmazonTTSVoice.es_ES_Conchita:
            case AmazonTTSVoice.es_ES_Enrique:
            case AmazonTTSVoice.es_MX_Mia:
            case AmazonTTSVoice.es_US_Penelope:
            case AmazonTTSVoice.es_US_Lupe:
            case AmazonTTSVoice.es_US_Miguel:
            case AmazonTTSVoice.tr_TR_Filiz:
            case AmazonTTSVoice.cy_GB_Gwyneth:
                return false;

            //AWS Neural Voices
            case AmazonTTSVoice.en_AU_OliviaNeural:
            case AmazonTTSVoice.en_GB_AmyNeural:
            case AmazonTTSVoice.en_GB_EmmaNeural:
            case AmazonTTSVoice.en_GB_BrianNeural:
            case AmazonTTSVoice.en_US_IvyNeural:
            case AmazonTTSVoice.en_US_JoannaNeural:
            case AmazonTTSVoice.en_US_KendraNeural:
            case AmazonTTSVoice.en_US_KimberlyNeural:
            case AmazonTTSVoice.en_US_SalliNeural:
            case AmazonTTSVoice.en_US_JoeyNeural:
            case AmazonTTSVoice.en_US_JustinNeural:
            case AmazonTTSVoice.en_US_KevinNeural:
            case AmazonTTSVoice.en_US_MatthewNeural:
                return true;

            default:
                BGC.Debug.LogError($"AmazonTTSVoice not supported {voice}");
                goto case AmazonTTSVoice.en_US_Joanna;
        }
    }

    public static bool GetRequiresLangTag(this AmazonTTSVoice voice)
    {
        switch (voice)
        {
            case AmazonTTSVoice.fr_FR_Celine:
            case AmazonTTSVoice.fr_FR_Lea:
            case AmazonTTSVoice.fr_FR_Mathieu:
            case AmazonTTSVoice.fr_CA_Chantal:
            case AmazonTTSVoice.de_DE_Marlene:
            case AmazonTTSVoice.de_DE_Vicki:
            case AmazonTTSVoice.de_DE_Hans:
            case AmazonTTSVoice.it_IT_Bianca:
            case AmazonTTSVoice.it_IT_Carla:
            case AmazonTTSVoice.it_IT_Giorgio:
            case AmazonTTSVoice.pl_PL_Ewa:
            case AmazonTTSVoice.pl_PL_Maja:
            case AmazonTTSVoice.pl_PL_Jacek:
            case AmazonTTSVoice.pl_PL_Jan:
            case AmazonTTSVoice.pt_BR_Vitoria:
            case AmazonTTSVoice.pt_BR_Camila:
            case AmazonTTSVoice.pt_BR_Ricardo:
            case AmazonTTSVoice.ru_RU_Tatyana:
            case AmazonTTSVoice.ru_RU_Maxim:
            case AmazonTTSVoice.es_ES_Lucia:
            case AmazonTTSVoice.es_ES_Conchita:
            case AmazonTTSVoice.es_ES_Enrique:
            case AmazonTTSVoice.es_MX_Mia:
            case AmazonTTSVoice.es_US_Penelope:
            case AmazonTTSVoice.es_US_Lupe:
            case AmazonTTSVoice.es_US_Miguel:
            case AmazonTTSVoice.tr_TR_Filiz:
            case AmazonTTSVoice.cy_GB_Gwyneth:
                return true;

            default:
                return false;
        }
    }


    public static SynthesizeSpeechRequest GetAmazonTTSSpeechRequest(this AmazonTTSVoice voice)
    {
        SynthesizeSpeechRequest synthesisRequest = new SynthesizeSpeechRequest
        {
            OutputFormat = OutputFormat.Mp3,
            Engine = Engine.Standard,
            LexiconNames = awsLexicons
        };

        switch (voice)
        {
            case AmazonTTSVoice.en_AU_Nicole:
                synthesisRequest.VoiceId = VoiceId.Nicole;
                break;

            case AmazonTTSVoice.en_AU_Russell:
                synthesisRequest.VoiceId = VoiceId.Russell;
                break;

            case AmazonTTSVoice.en_GB_Amy:
                synthesisRequest.VoiceId = VoiceId.Amy;
                break;

            case AmazonTTSVoice.en_GB_Emma:
                synthesisRequest.VoiceId = VoiceId.Emma;
                break;

            case AmazonTTSVoice.en_GB_Brian:
                synthesisRequest.VoiceId = VoiceId.Brian;
                break;

            case AmazonTTSVoice.en_IN_Aditi:
                synthesisRequest.VoiceId = VoiceId.Aditi;
                synthesisRequest.LanguageCode = LanguageCode.EnIN;
                break;

            case AmazonTTSVoice.en_IN_Raveena:
                synthesisRequest.VoiceId = VoiceId.Raveena;
                synthesisRequest.LanguageCode = LanguageCode.EnIN;
                break;

            case AmazonTTSVoice.en_US_Ivy:
                synthesisRequest.VoiceId = VoiceId.Ivy;
                break;

            case AmazonTTSVoice.en_US_Joanna:
                synthesisRequest.VoiceId = VoiceId.Joanna;
                break;

            case AmazonTTSVoice.en_US_Kendra:
                synthesisRequest.VoiceId = VoiceId.Kendra;
                break;

            case AmazonTTSVoice.en_US_Kimberly:
                synthesisRequest.VoiceId = VoiceId.Kimberly;
                break;

            case AmazonTTSVoice.en_US_Salli:
                synthesisRequest.VoiceId = VoiceId.Salli;
                break;

            case AmazonTTSVoice.en_US_Joey:
                synthesisRequest.VoiceId = VoiceId.Joey;
                break;

            case AmazonTTSVoice.en_US_Justin:
                synthesisRequest.VoiceId = VoiceId.Justin;
                break;

            case AmazonTTSVoice.en_US_Matthew:
                synthesisRequest.VoiceId = VoiceId.Matthew;
                break;

            case AmazonTTSVoice.en_GB_WLS_Geraint:
                synthesisRequest.VoiceId = VoiceId.Geraint;
                break;

            case AmazonTTSVoice.fr_FR_Celine:
                synthesisRequest.VoiceId = VoiceId.Celine;
                break;

            case AmazonTTSVoice.fr_FR_Lea:
                synthesisRequest.VoiceId = VoiceId.Lea;
                break;

            case AmazonTTSVoice.fr_FR_Mathieu:
                synthesisRequest.VoiceId = VoiceId.Mathieu;
                break;

            case AmazonTTSVoice.fr_CA_Chantal:
                synthesisRequest.VoiceId = VoiceId.Chantal;
                break;

            case AmazonTTSVoice.de_DE_Marlene:
                synthesisRequest.VoiceId = VoiceId.Marlene;
                break;

            case AmazonTTSVoice.de_DE_Vicki:
                synthesisRequest.VoiceId = VoiceId.Vicki;
                break;

            case AmazonTTSVoice.de_DE_Hans:
                synthesisRequest.VoiceId = VoiceId.Hans;
                break;

            case AmazonTTSVoice.it_IT_Bianca:
                synthesisRequest.VoiceId = VoiceId.Bianca;
                break;

            case AmazonTTSVoice.it_IT_Carla:
                synthesisRequest.VoiceId = VoiceId.Carla;
                break;

            case AmazonTTSVoice.it_IT_Giorgio:
                synthesisRequest.VoiceId = VoiceId.Giorgio;
                break;

            case AmazonTTSVoice.pl_PL_Ewa:
                synthesisRequest.VoiceId = VoiceId.Ewa;
                break;

            case AmazonTTSVoice.pl_PL_Maja:
                synthesisRequest.VoiceId = VoiceId.Maja;
                break;

            case AmazonTTSVoice.pl_PL_Jacek:
                synthesisRequest.VoiceId = VoiceId.Jacek;
                break;

            case AmazonTTSVoice.pl_PL_Jan:
                synthesisRequest.VoiceId = VoiceId.Jan;
                break;

            case AmazonTTSVoice.pt_BR_Vitoria:
                synthesisRequest.VoiceId = VoiceId.Vitoria;
                break;

            case AmazonTTSVoice.pt_BR_Camila:
                synthesisRequest.VoiceId = VoiceId.Camila;
                break;

            case AmazonTTSVoice.pt_BR_Ricardo:
                synthesisRequest.VoiceId = VoiceId.Ricardo;
                break;

            case AmazonTTSVoice.ru_RU_Tatyana:
                synthesisRequest.VoiceId = VoiceId.Tatyana;
                break;

            case AmazonTTSVoice.ru_RU_Maxim:
                synthesisRequest.VoiceId = VoiceId.Maxim;
                break;

            case AmazonTTSVoice.es_ES_Lucia:
                synthesisRequest.VoiceId = VoiceId.Lucia;
                break;

            case AmazonTTSVoice.es_ES_Conchita:
                synthesisRequest.VoiceId = VoiceId.Conchita;
                break;

            case AmazonTTSVoice.es_ES_Enrique:
                synthesisRequest.VoiceId = VoiceId.Enrique;
                break;

            case AmazonTTSVoice.es_MX_Mia:
                synthesisRequest.VoiceId = VoiceId.Mia;
                break;

            case AmazonTTSVoice.es_US_Penelope:
                synthesisRequest.VoiceId = VoiceId.Penelope;
                break;

            case AmazonTTSVoice.es_US_Lupe:
                synthesisRequest.VoiceId = VoiceId.Lupe;
                break;

            case AmazonTTSVoice.es_US_Miguel:
                synthesisRequest.VoiceId = VoiceId.Miguel;
                break;

            case AmazonTTSVoice.tr_TR_Filiz:
                synthesisRequest.VoiceId = VoiceId.Filiz;
                break;

            case AmazonTTSVoice.cy_GB_Gwyneth:
                synthesisRequest.VoiceId = VoiceId.Gwyneth;
                break;

            //AWS Neural Voices
            case AmazonTTSVoice.en_AU_OliviaNeural:
                synthesisRequest.VoiceId = VoiceId.Olivia;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case AmazonTTSVoice.en_GB_AmyNeural:
                synthesisRequest.VoiceId = VoiceId.Amy;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case AmazonTTSVoice.en_GB_EmmaNeural:
                synthesisRequest.VoiceId = VoiceId.Emma;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case AmazonTTSVoice.en_GB_BrianNeural:
                synthesisRequest.VoiceId = VoiceId.Brian;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case AmazonTTSVoice.en_US_IvyNeural:
                synthesisRequest.VoiceId = VoiceId.Ivy;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case AmazonTTSVoice.en_US_JoannaNeural:
                synthesisRequest.VoiceId = VoiceId.Joanna;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case AmazonTTSVoice.en_US_KendraNeural:
                synthesisRequest.VoiceId = VoiceId.Kendra;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case AmazonTTSVoice.en_US_KimberlyNeural:
                synthesisRequest.VoiceId = VoiceId.Kimberly;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case AmazonTTSVoice.en_US_SalliNeural:
                synthesisRequest.VoiceId = VoiceId.Salli;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case AmazonTTSVoice.en_US_JoeyNeural:
                synthesisRequest.VoiceId = VoiceId.Joey;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case AmazonTTSVoice.en_US_JustinNeural:
                synthesisRequest.VoiceId = VoiceId.Justin;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case AmazonTTSVoice.en_US_KevinNeural:
                synthesisRequest.VoiceId = VoiceId.Kevin;
                synthesisRequest.Engine = Engine.Neural;
                break;

            case AmazonTTSVoice.en_US_MatthewNeural:
                synthesisRequest.VoiceId = VoiceId.Matthew;
                synthesisRequest.Engine = Engine.Neural;
                break;

            default:
                BGC.Debug.LogError($"AmazonTTSVoice not supported {voice}");
                goto case AmazonTTSVoice.en_US_Joanna;
        }

        return synthesisRequest;
    }


}
