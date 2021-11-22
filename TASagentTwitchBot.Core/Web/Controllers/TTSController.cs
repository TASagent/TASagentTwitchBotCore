using Microsoft.AspNetCore.Mvc;

using TASagentTwitchBot.Core.TTS;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.Web.Middleware;

namespace TASagentTwitchBot.Core.Web.Controllers;

[ApiController]
[Route("/TASagentBotAPI/TTS/[action]")]
[ConditionalFeature("TTS")]
[ConditionalFeature("Audio")]
public class TTSController : ControllerBase
{
    private readonly IAudioEffectSystem audioEffectSystem;
    private readonly Notifications.ITTSHandler ttsHandler;

    public TTSController(
        IAudioEffectSystem audioEffectSystem,
        Notifications.ITTSHandler ttsHandler)
    {
        this.audioEffectSystem = audioEffectSystem;
        this.ttsHandler = ttsHandler;
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Privileged)]
    public IActionResult Play(
        TTSRequest request)
    {
        if (string.IsNullOrEmpty(request.Voice) ||
            string.IsNullOrEmpty(request.Pitch) ||
            string.IsNullOrEmpty(request.Speed) ||
            string.IsNullOrEmpty(request.Text))
        {
            return BadRequest();
        }

        string user = request.User;

        if (string.IsNullOrEmpty(user))
        {
            user = "Admin";
        }
        Effect effect;

        if (string.IsNullOrEmpty(request.Effect) || request.Effect.ToLower() == "none")
        {
            effect = new NoEffect();
        }
        else
        {
            effect = audioEffectSystem.SafeParse(request.Effect);
        }

        ttsHandler.HandleTTS(
            user: new Database.User()
            {
                AuthorizationLevel = Commands.AuthorizationLevel.Elevated,
                TwitchUserName = user,
                TTSVoicePreference = request.Voice.TranslateTTSVoice(),
                TTSPitchPreference = request.Pitch.TranslateTTSPitch(),
                TTSSpeedPreference = request.Speed.TranslateTTSSpeed(),
                TTSEffectsChain = effect.GetEffectsChain()
            },
            message: request.Text,
            approved: true);

        return Ok();
    }

    [HttpGet]
    [AuthRequired(AuthDegree.Admin)]
    public ActionResult<TTSSettings> Settings(
        [FromServices] TTSConfiguration ttsConfig)
    {
        return new TTSSettings(
            Enabled: ttsConfig.Enabled,
            BitThreshold: ttsConfig.BitThreshold,
            CommandEnabled: ttsConfig.Command.Enabled,
            CommandCooldown: ttsConfig.Command.CooldownTime,
            RedemptionEnabled: ttsConfig.Redemption.Enabled,
            AllowNeuralVoices: ttsConfig.AllowNeuralVoices);
    }

    [HttpPost]
    [AuthRequired(AuthDegree.Admin)]
    public IActionResult Settings(
        TTSSettings ttsSettings,
        [FromServices] TTSConfiguration ttsConfig)
    {
        ttsConfig.Enabled = ttsSettings.Enabled;
        ttsConfig.BitThreshold = ttsSettings.BitThreshold;
        ttsConfig.Command.Enabled = ttsSettings.CommandEnabled;
        ttsConfig.Command.CooldownTime = ttsSettings.CommandCooldown;
        ttsConfig.Redemption.Enabled = ttsSettings.RedemptionEnabled;
        ttsConfig.AllowNeuralVoices = ttsSettings.AllowNeuralVoices;

        ttsConfig.Serialize();

        return Ok();
    }
}

public record TTSSettings(
    bool Enabled,
    int BitThreshold,
    bool CommandEnabled,
    int CommandCooldown,
    bool RedemptionEnabled,
    bool AllowNeuralVoices);


public record TTSRequest(
    string Voice,
    string Pitch,
    string Speed,
    string Effect,
    string Text,
    string User);
