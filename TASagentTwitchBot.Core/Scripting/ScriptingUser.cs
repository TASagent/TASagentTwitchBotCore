using BGC.Scripting.Parsing;
using TASagentTwitchBot.Core.TTS;

namespace TASagentTwitchBot.Core.Scripting;

public class ScriptingUser
{
    [ScriptingAccess]
    public string TwitchUserName { get; set; } = "";

    [ScriptingAccess]
    public string TwitchUserId { get; set; } = "";

    [ScriptingAccess]
    public string Color { get; set; } = "";

    [ScriptingAccess]
    public int AuthorizationLevel { get; set; } = 0;

    [ScriptingAccess]
    public string TTSVoice { get; set; } = "";

    [ScriptingAccess]
    public string TTSPitch { get; set; } = "";

    [ScriptingAccess]
    public string TTSSpeed { get; set; } = "";

    [ScriptingAccess]
    public string TTSEffect { get; set; } = "";

    public static ScriptingUser FromDB(Database.User user) => new ScriptingUser()
    {
        TwitchUserName = user.TwitchUserName,
        TwitchUserId = user.TwitchUserId,
        Color = string.IsNullOrEmpty(user.Color) ? "#0000FF" : user.Color,
        AuthorizationLevel = (int)user.AuthorizationLevel,

        TTSVoice = user.TTSVoicePreference.Serialize(),
        TTSPitch = user.TTSPitchPreference.GetPitchShift(),
        TTSSpeed = user.TTSSpeedPreference.GetSpeedValue(),
        TTSEffect = user.TTSEffectsChain ?? ""
    };
}

