using BGC.Scripting.Parsing;
using TASagentTwitchBot.Core.TTS;

namespace TASagentTwitchBot.Core.Scripting;

public class ScriptingUser
{
    public string TwitchUserName { get; set; } = "";
    public string TwitchUserId { get; set; } = "";
    public string Color { get; set; } = "";
    public int AuthorizationLevel { get; set; } = 0;
    public string TTSVoice { get; set; } = "";
    public string TTSPitch { get; set; } = "";
    public string TTSSpeed { get; set; } = "";
    public string TTSEffect { get; set; } = "";

    public static ScriptingUser FromDB(Database.User user) => new ScriptingUser()
    {
        TwitchUserName = user.TwitchUserName,
        TwitchUserId = user.TwitchUserId,
        Color = string.IsNullOrWhiteSpace(user.Color) ? "#0000FF" : user.Color,
        AuthorizationLevel = (int)user.AuthorizationLevel,

        TTSVoice = user.TTSVoicePreference.Serialize(),
        TTSPitch = user.TTSPitchPreference.GetPitchShift(),
        TTSSpeed = user.TTSSpeedPreference.GetSpeedValue(),
        TTSEffect = user.TTSEffectsChain ?? ""
    };
}

