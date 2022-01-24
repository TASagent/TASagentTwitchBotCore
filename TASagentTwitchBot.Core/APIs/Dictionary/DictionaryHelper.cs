using System.Text.Json;
using System.Text.Json.Serialization;
using RestSharp;

namespace TASagentTwitchBot.Core.API.Dictionary;

public class DictionaryHelper
{
    private static readonly Uri DictionaryAPIURI = new Uri("https://api.dictionaryapi.dev/api/v2/entries/en");

    public DictionaryHelper()
    {
    }

    public async Task<List<DictionaryInfo>?> GetDefinition(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return null;
        }

        word = word.Trim();

        if (word.Contains(' '))
        {
            word = word[..word.IndexOf(' ')];
        }

        RestClient restClient = new RestClient(DictionaryAPIURI);
        RestRequest request = new RestRequest(word, Method.Get);

        RestResponse response = await restClient.ExecuteAsync(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<DictionaryInfo>>(response.Content!);
    }
}

public record DictionaryInfo(
    [property: JsonPropertyName("word")] string Word,
    [property: JsonPropertyName("phonetic")] string Phonetic,
    [property: JsonPropertyName("phonetics")] List<PhoneticInfo> Phonetics,
    [property: JsonPropertyName("origin")] string Origin,
    [property: JsonPropertyName("meanings")] List<MeaningInfo> Meanings);

public record MeaningInfo(
    [property: JsonPropertyName("partOfSpeech")] string PartOfSpeech,
    [property: JsonPropertyName("definitions")] List<DefinitionInfo> Definitions);

public record DefinitionInfo(
    [property: JsonPropertyName("definition")] string Definition,
    [property: JsonPropertyName("synonyms")] List<string> Synonyms,
    [property: JsonPropertyName("antonyms")] List<string> Antonyms,
    [property: JsonPropertyName("example")] [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Example = null);

public record PhoneticInfo(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("audio")] string AudioURL);
