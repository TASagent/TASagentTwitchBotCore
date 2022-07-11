using BGC.Scripting.Parsing;
using TASagentTwitchBot.Core.Commands;
using TASagentTwitchBot.Core.TTS;

namespace TASagentTwitchBot.Core.Scripting;

public class ScriptingUser
{
    public string TwitchUserName { get; }
    public string TwitchUserId { get; }
    public string Color { get; set; }
    public AuthorizationLevel AuthorizationLevel { get; set; }
    public string TTSVoice { get; }
    public TTSPitch TTSPitch { get; }
    public TTSSpeed TTSSpeed { get; }
    public string TTSEffect { get; }

    private readonly IPersistentDataManager persistentDataManager;

    public ScriptingUser(
        string twitchUserName,
        string twitchUserId,
        string color,
        AuthorizationLevel authorizationLevel,
        string ttsVoice,
        TTSPitch ttsPitch,
        TTSSpeed ttsSpeed,
        string ttsEffect,
        IPersistentDataManager persistentDataManager)
    {
        TwitchUserName = twitchUserName;
        TwitchUserId = twitchUserId;
        Color = color;
        AuthorizationLevel = authorizationLevel;
        TTSVoice = ttsVoice;
        TTSPitch = ttsPitch;
        TTSSpeed = ttsSpeed;
        TTSEffect = ttsEffect;
        this.persistentDataManager = persistentDataManager;
    }

    public bool HasDatum(string key) => persistentDataManager.HasUserDatum(TwitchUserId, key);
    public T? GetDatum<T>(string key) => persistentDataManager.GetUserDatum<T>(TwitchUserId, key);
    public void SetDatum<T>(string key, T value) => persistentDataManager.SetUserDatum(TwitchUserId, key, value);
}

