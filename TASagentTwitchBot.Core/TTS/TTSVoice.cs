
namespace TASagentTwitchBot.Core.TTS;

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

public static class TTSVoiceExtensions
{
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
