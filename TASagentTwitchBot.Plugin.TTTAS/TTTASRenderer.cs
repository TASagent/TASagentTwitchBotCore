using System.Text.RegularExpressions;

using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Audio;

namespace TASagentTwitchBot.Plugin.TTTAS;

[AutoRegister]
public interface ITTTASRenderer
{
    Task<AudioRequest?> TTTASRequest(string tttasText);
}

public class TTTASRenderer : ITTTASRenderer
{
    private readonly ICommunication communication;
    private readonly ISoundEffectSystem soundEffectSystem;
    private readonly ITTTASProvider tttasProvider;

    private readonly TTTASConfiguration tttasConfig;

    //This captures:
    //  Commands like !thing or !thing(argument) or !thing(argument,argument) or !thing(argument, argument)
    //  Sound effects like /bao
    //  Words containing letters and apostrophes, and optionally ending in a period, questionmark, comma, or exclamation mark
    //  Numbers containing commas and periods and optionally ending with punctuation and optionally starting with a negative sign
    //  Slashes, quotation marks, dashes, question marks
    private static readonly Regex wordRegex = new Regex(
        @"(?:\![\p{L}']+(?:\([0-9, ]+\))?|\/[\p{L}']+|[\p{L}']+[?.,!]?|\-?[0-9,.]+[?.,!]?|[\/\\\""\-\?]|(?:\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff]))");
    private static readonly Regex numberLikeSplitter = new Regex(@"(?:[0-9]+|.)");


    public TTTASRenderer(
        ICommunication communication,
        ISoundEffectSystem soundEffectSystem,
        ITTTASProvider tttasProvider,
        TTTASConfiguration tttasConfig)
    {
        this.communication = communication;
        this.soundEffectSystem = soundEffectSystem;
        this.tttasProvider = tttasProvider;

        this.tttasConfig = tttasConfig;
    }

    public Task<AudioRequest?> TTTASRequest(string tttasText)
    {
        if (string.IsNullOrWhiteSpace(tttasText))
        {
            communication.SendWarningMessage($"{tttasConfig.FeatureNameBrief} Request of null or whitespace.");
            return Task.FromResult<AudioRequest?>(new AudioDelay(500));
        }

        //Trim, lowercase, and normalize the text
        tttasText = NormalizeString(tttasText.Trim().ToLowerInvariant());
        tttasText = tttasText.Normalize();

        List<string> splitTTTASText = wordRegex.Matches(tttasText).Select(x => x.Value).ToList();

        if (splitTTTASText.Count == 0)
        {
            communication.SendWarningMessage($"{tttasConfig.FeatureNameBrief} Request of {tttasText} yielded no words.");
            return Task.FromResult<AudioRequest?>(new AudioDelay(500));
        }

        return TTTASRequest(splitTTTASText);
    }


    private async Task<AudioRequest?> TTTASRequest(List<string> splitTTTASText)
    {
        List<AudioRequest> audioFragments = new List<AudioRequest>();
        List<Task<bool>> pendingRequests = new List<Task<bool>>();

        string requestId = Guid.NewGuid().ToString();

        foreach (string tttasWord in splitTTTASText)
        {
            HandleWord(tttasWord, requestId, audioFragments, pendingRequests);
        }

        bool[] results = await Task.WhenAll(pendingRequests);

        if (!results.All(x => x))
        {
            //At least one recording was rejected
            communication.SendWarningMessage($"TTTAS Aborted");
            return null;
        }

        return new ConcatenatedAudioRequest(audioFragments);
    }


    private void HandleWord(
        string tttasWord,
        string requestId,
        List<AudioRequest> audioFragments,
        List<Task<bool>> pendingRequests)
    {
        //Just in case
        if (tttasWord.Length == 0)
        {
            return;
        }

        //Handle some special characters
        if (tttasWord.Length == 1)
        {
            switch (tttasWord[0])
            {
                case '/':
                    HandleWord("slash", requestId, audioFragments, pendingRequests);
                    return;

                case '\\':
                    HandleWord("backslash", requestId, audioFragments, pendingRequests);
                    return;

                case '!':
                    HandleWord("exclamation", requestId, audioFragments, pendingRequests);
                    HandleWord("mark", requestId, audioFragments, pendingRequests);
                    return;

                case '"':
                    HandleWord("quote", requestId, audioFragments, pendingRequests);
                    return;

                case '-':
                    HandleWord("hyphen", requestId, audioFragments, pendingRequests);
                    return;

                case '?':
                    HandleWord("question", requestId, audioFragments, pendingRequests);
                    HandleWord("mark", requestId, audioFragments, pendingRequests);
                    return;

                default:
                    //Pass through to rest of function
                    break;
            }
        }

        if (tttasWord.StartsWith('/'))
        {
            //Handle Sound Effect
            SoundEffect? soundEffect = soundEffectSystem.GetSoundEffectByAlias(tttasWord);

            if (soundEffect is null)
            {
                //Unrecognized, append as is
                HandleWord("/", requestId, audioFragments, pendingRequests);
                HandleWord(tttasWord[1..], requestId, audioFragments, pendingRequests);

                return;
            }

            audioFragments.Add(new SoundEffectRequest(soundEffect));

            return;
        }

        if (tttasWord.StartsWith('!'))
        {
            //Handle Command
            AudioRequest? commandRequest = AudioRequest.ParseCommand(tttasWord);

            if (commandRequest is null)
            {
                //Unrecognized, append as is
                HandleWord("!", requestId, audioFragments, pendingRequests);
                HandleWord(tttasWord[1..], requestId, audioFragments, pendingRequests);

                return;
            }

            audioFragments.Add(commandRequest);

            return;
        }

        if (char.IsDigit(tttasWord[0]) || (tttasWord[0] == '-' && tttasWord.Length > 1 && char.IsDigit(tttasWord[1])))
        {
            //Handle number
            HandleNumberPhrase(tttasWord, requestId, audioFragments, pendingRequests);

            return;
        }

        //Regular word

        if (tttasWord.EndsWith('.') || tttasWord.EndsWith(','))
        {
            //Strip off trailing periods and commas.
            tttasWord = tttasWord[..^1];
        }

        AudioRequest request = tttasProvider.GetWord(tttasWord, requestId);
        audioFragments.Add(request);

        if (request is TTTASPendingAudioRequest pendingAudioRequest)
        {
            pendingRequests.Add(pendingAudioRequest.WaitForReadyAsync());
        }
    }


    private void HandleNumberPhrase(
        string tttasWord,
        string requestId,
        List<AudioRequest> audioFragments,
        List<Task<bool>> pendingRequests)
    {
        //Just in case
        if (tttasWord.Length == 0)
        {
            return;
        }

        //Strip commas
        tttasWord = tttasWord.Replace(",", "");

        if (tttasWord.EndsWith('.') || tttasWord.EndsWith(',') || tttasWord.EndsWith('!') || tttasWord.EndsWith('?'))
        {
            //Strip off trailing punctuation
            tttasWord = tttasWord[..^1];
        }

        if (tttasWord.StartsWith('-'))
        {
            HandleWord("negative", requestId, audioFragments, pendingRequests);
            tttasWord = tttasWord[1..];
        }

        int decimalCount = tttasWord.Count(x => x == '.');

        if (decimalCount > 1)
        {
            //treat like IP Address or something

            foreach (string numberSegment in numberLikeSplitter.Matches(tttasWord))
            {
                if (numberSegment == ".")
                {
                    HandleWord("dot", requestId, audioFragments, pendingRequests);
                }
                else
                {
                    HandleBareNumber(numberSegment, requestId, audioFragments, pendingRequests);
                }
            }
        }
        else if (decimalCount == 1)
        {
            //Decimal number
            int decimalIndex = tttasWord.IndexOf('.');

            if (decimalIndex > 0)
            {
                HandleBareNumber(tttasWord[0..decimalIndex], requestId, audioFragments, pendingRequests);
                HandleWord("point", requestId, audioFragments, pendingRequests);
                HandleNumberSequence(tttasWord[(decimalIndex + 1)..], requestId, audioFragments, pendingRequests);
            }
        }
        else
        {
            //No decimal
            HandleBareNumber(tttasWord, requestId, audioFragments, pendingRequests);
        }
    }


    private void HandleBareNumber(
        string tttasWord,
        string requestId,
        List<AudioRequest> audioFragments,
        List<Task<bool>> pendingRequests)
    {
        //Just in case
        while (tttasWord.Length > 0 && tttasWord[0] == '0')
        {
            HandleWord("zero", requestId, audioFragments, pendingRequests);
            tttasWord = tttasWord[1..];
        }

        if (tttasWord.Length == 0)
        {
            //Done
            return;
        }

        int tripletCount = (tttasWord.Length + 2) / 3;
        tttasWord = tttasWord.PadLeft(3 * tripletCount, '0');

        for (int triplet = 0; triplet < tripletCount; triplet++)
        {
            string tripletString = tttasWord[(triplet * 3)..(triplet * 3 + 3)];

            if (tripletString == "000")
            {
                continue;
            }

            HandleBareNumberTriplet(tripletString, requestId, audioFragments, pendingRequests);
            HandleNumberTripletWord(tripletCount - triplet - 1, requestId, audioFragments, pendingRequests);
        }
    }


    private void HandleBareNumberTriplet(
        string tttasWord,
        string requestId,
        List<AudioRequest> audioFragments,
        List<Task<bool>> pendingRequests)
    {
        if (tttasWord.Length > 3)
        {
            communication.SendWarningMessage($"Skipping digits in unexpected BareNumberTriplet: \"{tttasWord}\"");
            tttasWord = tttasWord[0..3];
        }

        if (tttasWord.Length < 3)
        {
            tttasWord = tttasWord.PadLeft(3, '0');
        }

        if (tttasWord == "000")
        {
            //Triplet is empty
            return;
        }

        if (tttasWord[0] != '0')
        {
            //100's place is populated
            HandleNumberChar(tttasWord[0], requestId, audioFragments, pendingRequests);
            HandleWord("hundred", requestId, audioFragments, pendingRequests);
        }

        if (tttasWord[1] == '0')
        {
            //There is no 10's place

            if (tttasWord[2] == '0')
            {
                //No 10's or 1's place
                return;
            }

            HandleNumberChar(tttasWord[2], requestId, audioFragments, pendingRequests);
            return;
        }
        else if (tttasWord[1] == '1')
        {
            //The Teens
            switch (tttasWord[2])
            {
                case '0':
                    HandleWord("ten", requestId, audioFragments, pendingRequests);
                    return;

                case '1':
                    HandleWord("eleven", requestId, audioFragments, pendingRequests);
                    return;

                case '2':
                    HandleWord("twelve", requestId, audioFragments, pendingRequests);
                    return;

                case '3':
                    HandleWord("thirteen", requestId, audioFragments, pendingRequests);
                    return;

                case '4':
                    HandleWord("fourteen", requestId, audioFragments, pendingRequests);
                    return;

                case '5':
                    HandleWord("fifteen", requestId, audioFragments, pendingRequests);
                    return;

                case '6':
                    HandleWord("sixteen", requestId, audioFragments, pendingRequests);
                    return;

                case '7':
                    HandleWord("seventeen", requestId, audioFragments, pendingRequests);
                    return;

                case '8':
                    HandleWord("eighteen", requestId, audioFragments, pendingRequests);
                    return;

                case '9':
                    HandleWord("nineteen", requestId, audioFragments, pendingRequests);
                    return;

                default:
                    communication.SendWarningMessage($"Skipping unexpected NumberSequence Character: \"{tttasWord[2]}\"");
                    return;
            }
        }
        else
        {
            //20-99
            switch (tttasWord[1])
            {
                case '2':
                    HandleWord("twenty", requestId, audioFragments, pendingRequests);
                    break;

                case '3':
                    HandleWord("thirty", requestId, audioFragments, pendingRequests);
                    break;

                case '4':
                    HandleWord("fourty", requestId, audioFragments, pendingRequests);
                    break;

                case '5':
                    HandleWord("fifty", requestId, audioFragments, pendingRequests);
                    break;

                case '6':
                    HandleWord("sixty", requestId, audioFragments, pendingRequests);
                    break;

                case '7':
                    HandleWord("seventy", requestId, audioFragments, pendingRequests);
                    break;

                case '8':
                    HandleWord("eighty", requestId, audioFragments, pendingRequests);
                    break;

                case '9':
                    HandleWord("ninety", requestId, audioFragments, pendingRequests);
                    break;

                default:
                    communication.SendWarningMessage($"Skipping unexpected NumberSequence Character: \"{tttasWord[2]}\"");
                    return;
            }

            if (tttasWord[2] != '0')
            {
                HandleNumberChar(tttasWord[2], requestId, audioFragments, pendingRequests);
            }
        }
    }

    private void HandleNumberSequence(
        string tttasWord,
        string requestId,
        List<AudioRequest> audioFragments,
        List<Task<bool>> pendingRequests)
    {
        //Just in case
        if (tttasWord.Length == 0)
        {
            return;
        }

        for (int i = 0; i < tttasWord.Length; i++)
        {
            HandleNumberChar(tttasWord[i], requestId, audioFragments, pendingRequests);
        }
    }

    private void HandleNumberChar(
        char tttasDigit,
        string requestId,
        List<AudioRequest> audioFragments,
        List<Task<bool>> pendingRequests)
    {
        switch (tttasDigit)
        {
            case '0':
                HandleWord("zero", requestId, audioFragments, pendingRequests);
                return;

            case '1':
                HandleWord("one", requestId, audioFragments, pendingRequests);
                return;

            case '2':
                HandleWord("two", requestId, audioFragments, pendingRequests);
                return;

            case '3':
                HandleWord("three", requestId, audioFragments, pendingRequests);
                return;

            case '4':
                HandleWord("four", requestId, audioFragments, pendingRequests);
                return;

            case '5':
                HandleWord("five", requestId, audioFragments, pendingRequests);
                return;

            case '6':
                HandleWord("six", requestId, audioFragments, pendingRequests);
                return;

            case '7':
                HandleWord("seven", requestId, audioFragments, pendingRequests);
                return;

            case '8':
                HandleWord("eight", requestId, audioFragments, pendingRequests);
                return;

            case '9':
                HandleWord("nine", requestId, audioFragments, pendingRequests);
                return;

            default:
                communication.SendWarningMessage($"Skipping unexpected NumberSequence Character: \"{tttasDigit}\"");
                break;
        }
    }

    private void HandleNumberTripletWord(
        int tripletNum,
        string requestId,
        List<AudioRequest> audioFragments,
        List<Task<bool>> pendingRequests)
    {
        switch (tripletNum)
        {
            case 0:
                //None
                return;

            case 1:
                HandleWord("thousand", requestId, audioFragments, pendingRequests);
                return;

            case 2:
                HandleWord("million", requestId, audioFragments, pendingRequests);
                return;

            case 3:
                HandleWord("billion", requestId, audioFragments, pendingRequests);
                return;

            case 4:
                HandleWord("trillion", requestId, audioFragments, pendingRequests);
                return;

            case 5:
                HandleWord("quadrillion", requestId, audioFragments, pendingRequests);
                return;

            case 6:
                HandleWord("quintillion", requestId, audioFragments, pendingRequests);
                return;

            case 7:
                HandleWord("sextillion", requestId, audioFragments, pendingRequests);
                return;

            case 8:
                HandleWord("septillion", requestId, audioFragments, pendingRequests);
                return;

            case 9:
                HandleWord("octillion", requestId, audioFragments, pendingRequests);
                return;

            case 10:
                HandleWord("nonillion", requestId, audioFragments, pendingRequests);
                return;

            case 11:
                HandleWord("decillion", requestId, audioFragments, pendingRequests);
                return;

            case 12:
                HandleWord("undecillion", requestId, audioFragments, pendingRequests);
                return;

            case 13:
                HandleWord("duodecillion", requestId, audioFragments, pendingRequests);
                return;

            case 14:
                HandleWord("tredecillion", requestId, audioFragments, pendingRequests);
                return;

            case 15:
                HandleWord("quattuordecillion", requestId, audioFragments, pendingRequests);
                return;

            case 16:
                HandleWord("quindecillion", requestId, audioFragments, pendingRequests);
                return;

            case 17:
                HandleWord("sexdecillion", requestId, audioFragments, pendingRequests);
                return;

            case 18:
                HandleWord("septendecillion", requestId, audioFragments, pendingRequests);
                return;

            case 19:
                HandleWord("octodecillion", requestId, audioFragments, pendingRequests);
                return;

            case 20:
                HandleWord("novemdecillion", requestId, audioFragments, pendingRequests);
                return;

            case 21:
                HandleWord("vigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 22:
                HandleWord("unvigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 23:
                HandleWord("duovigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 24:
                HandleWord("tresvigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 25:
                HandleWord("quattuorvigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 26:
                HandleWord("quinvigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 27:
                HandleWord("sesvigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 28:
                HandleWord("septemvigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 29:
                HandleWord("octovigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 30:
                HandleWord("novemvigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 31:
                HandleWord("trigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 32:
                HandleWord("untrigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 33:
                HandleWord("duotrigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 34:
                HandleWord("trestrigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 35:
                HandleWord("quattuortrigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 36:
                HandleWord("quintrigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 37:
                HandleWord("sestrigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 38:
                HandleWord("septentrigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 39:
                HandleWord("octotrigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 40:
                HandleWord("noventrigintillion", requestId, audioFragments, pendingRequests);
                return;

            case 41:
                HandleWord("quadragintillion", requestId, audioFragments, pendingRequests);
                return;

            case 42:
                HandleWord("unquadragintillion", requestId, audioFragments, pendingRequests);
                return;

            case 43:
                HandleWord("duoquadragintillion", requestId, audioFragments, pendingRequests);
                return;

            case 44:
                HandleWord("tresquadragintillion", requestId, audioFragments, pendingRequests);
                return;

            case 45:
                HandleWord("quattuorquadragintillion", requestId, audioFragments, pendingRequests);
                return;

            case 46:
                HandleWord("quinquadragintillion", requestId, audioFragments, pendingRequests);
                return;

            case 47:
                HandleWord("sesquadragintillion", requestId, audioFragments, pendingRequests);
                return;

            case 48:
                HandleWord("septenquadragintillion", requestId, audioFragments, pendingRequests);
                return;

            case 49:
                HandleWord("octoquadragintillion", requestId, audioFragments, pendingRequests);
                return;

            case 50:
                HandleWord("novenquadragintillion", requestId, audioFragments, pendingRequests);
                return;

            case 51:
                HandleWord("quinquagintillion", requestId, audioFragments, pendingRequests);
                return;

            case 52:
                HandleWord("unquinquagintillion", requestId, audioFragments, pendingRequests);
                return;

            case 53:
                HandleWord("duoquinquagintillion", requestId, audioFragments, pendingRequests);
                return;

            case 54:
                HandleWord("tresquinquagintillion", requestId, audioFragments, pendingRequests);
                return;

            case 55:
                HandleWord("quattuorquinquagintillion", requestId, audioFragments, pendingRequests);
                return;

            case 56:
                HandleWord("quinquinquagintillion", requestId, audioFragments, pendingRequests);
                return;

            case 57:
                HandleWord("sesquinquagintillion", requestId, audioFragments, pendingRequests);
                return;

            case 58:
                HandleWord("septenquinquagintillion", requestId, audioFragments, pendingRequests);
                return;

            case 59:
                HandleWord("octoquinquagintillion", requestId, audioFragments, pendingRequests);
                return;

            case 60:
                HandleWord("novenquinquagintillion", requestId, audioFragments, pendingRequests);
                return;

            default:
                communication.SendWarningMessage($"Skipping unexpected NumberTriplet Word: {tripletNum}");
                break;
        }
    }

    private static string NormalizeString(string input)
    {
        char[] inputArray = input.ToCharArray();

        for (int i = 0; i < inputArray.Length; i++)
        {
            switch (inputArray[i])
            {
                // en dash
                case '\u2013':
                    inputArray[i] = '-';
                    break;

                // em dash
                case '\u2014':
                    inputArray[i] = '-';
                    break;

                // horizontal line
                case '\u2015':
                    inputArray[i] = '-';
                    break;


                // double low line
                case '\u2017':
                    inputArray[i] = '_';
                    break;


                //Single Quotes
                // left single quotation mark
                case '\u2018':
                    inputArray[i] = '\'';
                    break;

                // right single quotation mark
                case '\u2019':
                    inputArray[i] = '\'';
                    break;

                // single low-9 quotation mark
                case '\u201a':
                    inputArray[i] = ',';
                    break;

                // single high-reversed-9 quotation mark
                case '\u201b':
                    inputArray[i] = '\'';
                    break;

                // prime
                case '\u2032':
                    inputArray[i] = '\'';
                    break;

                // back-tick
                case '`':
                    inputArray[i] = '\'';
                    break;


                //Double Quotes
                // left double quotation mark
                case '\u201c':
                    inputArray[i] = '\"';
                    break;

                // right double quotation mark
                case '\u201d':
                    inputArray[i] = '\"';
                    break;

                // double low-9 quotation mark
                case '\u201e':
                    inputArray[i] = '\"';
                    break;

                // double prime
                case '\u2033':
                    inputArray[i] = '\"';
                    break;

                default:
                    //Do nothing
                    break;
            }
        }

        return new string(inputArray);
    }
}
