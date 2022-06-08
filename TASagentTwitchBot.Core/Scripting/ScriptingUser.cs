using BGC.Scripting.Parsing;
using TASagentTwitchBot.Core.TTS;

namespace TASagentTwitchBot.Core.Scripting;

public class ScriptingUser
{
    public string TwitchUserName { get; set; } = "";
    public string TwitchUserId { get; set; } = "";
    public string Color { get; set; } = "";
    public Commands.AuthorizationLevel AuthorizationLevel { get; set; } = Commands.AuthorizationLevel.Restricted;
    public TTSVoice TTSVoice { get; set; } = TTSVoice.Unassigned;
    public TTSPitch TTSPitch { get; set; } = TTSPitch.Unassigned;
    public TTSSpeed TTSSpeed { get; set; } = TTSSpeed.Unassigned;
    public string TTSEffect { get; set; } = "";

    public static ScriptingUser FromDB(Database.User user) => new ScriptingUser()
    {
        TwitchUserName = user.TwitchUserName,
        TwitchUserId = user.TwitchUserId,
        Color = string.IsNullOrWhiteSpace(user.Color) ? "#0000FF" : user.Color,
        AuthorizationLevel = user.AuthorizationLevel,

        TTSVoice = user.TTSVoicePreference,
        TTSPitch = user.TTSPitchPreference,
        TTSSpeed = user.TTSSpeedPreference,
        TTSEffect = user.TTSEffectsChain ?? ""
    };
}

