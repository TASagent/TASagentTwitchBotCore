using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.Commands;
using TASagentTwitchBot.Core.Database;

namespace TASagentTwitchBot.Core.TTS
{
    public class TTSSystem : ICommandContainer
    {
        //Subsystems
        private readonly IServiceScopeFactory scopeFactory;
        private readonly Config.BotConfiguration botConfig;
        private readonly TTSConfiguration ttsConfig;
        private readonly ICommunication communication;
        private readonly IAudioEffectSystem audioEffectSystem;
        private readonly Notifications.ITTSHandler ttsHandler;

        public TTSSystem(
            Config.BotConfiguration botConfig,
            TTSConfiguration ttsConfig,
            ICommunication communication,
            IAudioEffectSystem audioEffectSystem,
            Notifications.ITTSHandler ttsHandler,
            IServiceScopeFactory scopeFactory)
        {
            this.botConfig = botConfig;
            this.ttsConfig = ttsConfig;
            this.communication = communication;
            this.audioEffectSystem = audioEffectSystem;
            this.ttsHandler = ttsHandler;
            this.scopeFactory = scopeFactory;
        }

        public void RegisterCommands(
            Dictionary<string, CommandHandler> commands,
            Dictionary<string, HelpFunction> helpFunctions,
            Dictionary<string, SetFunction> setFunctions,
            Dictionary<string, GetFunction> getFunctions)
        {
            commands.Add(ttsConfig.Command.CommandName, HandleTTSRequest);
            helpFunctions.Add(ttsConfig.Command.CommandName, HandleTTSHelpRequest);
            setFunctions.Add(ttsConfig.Command.CommandName, HandleTTSSetRequest);
            getFunctions.Add(ttsConfig.Command.CommandName, HandleTTSGetRequest);
        }

        public IEnumerable<string> GetPublicCommands()
        {
            if (ttsConfig.Command.Enabled)
            {
                yield return ttsConfig.Command.CommandName;
            }
        }

        private string HandleTTSHelpRequest(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Elevated)
            {
                return $"Looks like you're not authorized to use the {ttsConfig.FeatureName} system, @{chatter.User.TwitchUserName}";
            }

            if (remainingCommand == null || remainingCommand.Length == 0)
            {
                return $"{ttsConfig.FeatureName} Command - Send text as spoken audio to the stream with: !{ttsConfig.Command.CommandName} <text>  For more information, visit https://tas.wtf/info/tts";
            }
            else if (remainingCommand[0].ToLower() == "voice")
            {
                return $"{ttsConfig.FeatureName} Voice - Set your personal {ttsConfig.FeatureNameBrief} Voice with !set {ttsConfig.Command.CommandName} voice <Voice>. Eg justin, joanna, brian, or en-US-Standard-B.  For more information, visit https://tas.wtf/info/tts#setting-voice";
            }
            else if (remainingCommand[0].ToLower() == "pitch")
            {
                return $"{ttsConfig.FeatureName} Pitch - Set your personal {ttsConfig.FeatureNameBrief} Voice pitch with !set {ttsConfig.Command.CommandName} pitch <Pitch>. Eg normal, x-low, low, high, x-high.  For more information, visit https://tas.wtf/info/tts#setting-pitch";
            }
            else if (remainingCommand[0].ToLower() == "speed")
            {
                return $"{ttsConfig.FeatureName} Speed - Set your personal {ttsConfig.FeatureNameBrief} Voice speed with !set {ttsConfig.Command.CommandName} speed <Speed>. Eg normal, x-slow, slow, fast, x-fast.  For more information, visit https://tas.wtf/info/tts#setting-speed";
            }
            else if (remainingCommand[0].ToLower() == "sounds")
            {
                return $"{ttsConfig.FeatureName} Sounds - Add sounds to your {ttsConfig.FeatureNameBrief} with commands like /bao, /midway, /jump, /kick, /pipe, /powerup.  For more information, visit https://tas.wtf/info/tts#integrated-sound-effects";
            }
            else
            {
                return $"No TTS subcommand found: {string.Join(' ', remainingCommand)}";
            }
        }

        private async Task HandleTTSRequest(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (!ttsConfig.Enabled || !ttsConfig.Command.Enabled)
            {
                //TTS disabled entirely
                return;
            }

            //Check permissions
            if (!ttsConfig.CanUseCommand(chatter.User.AuthorizationLevel))
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, I'm afraid the {ttsConfig.FeatureName} system is currently disabled.");
                return;
            }

            //Check for cooldown
            if ((chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator || (ttsConfig.Command.ModsIgnoreCooldown && chatter.User.AuthorizationLevel == AuthorizationLevel.Moderator)) &&
                chatter.User.LastSuccessfulTTS.HasValue &&
                DateTime.Now < chatter.User.LastSuccessfulTTS.Value + new TimeSpan(hours: 0, minutes: 0, seconds: ttsConfig.Command.CooldownTime))
            {
                communication.SendDebugMessage($"User {chatter.User.TwitchUserName} rebuked for {ttsConfig.FeatureNameBrief} Spam");

                TimeSpan remainingTime = (chatter.User.LastSuccessfulTTS.Value + new TimeSpan(hours: 0, minutes: 0, seconds: ttsConfig.Command.CooldownTime)) - DateTime.Now;

                string remainingString;
                if (remainingTime.TotalMinutes > 1)
                {
                    remainingString = $"{Math.Floor(remainingTime.TotalMinutes)} minutes and {remainingTime.Seconds} seconds";
                }
                else if (remainingTime.Seconds > 1)
                {
                    remainingString = $"{remainingTime.Seconds} seconds";
                }
                else
                {
                    remainingString = $"1 second";
                }

                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, you must wait {remainingString} before you can do that again.");
                return;
            }

            if (remainingCommand == null || remainingCommand.Length == 0)
            {
                //TTS Error
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, sorry, I can't TTS nothing.");
                return;
            }

            //Update last TTS time
            using IServiceScope scope = scopeFactory.CreateScope();
            BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

            User dbUser = await db.Users.FindAsync(chatter.User.UserId);
            dbUser.LastSuccessfulTTS = DateTime.Now;
            await db.SaveChangesAsync();

            if (!ttsConfig.HasCommandApproval(chatter.User.AuthorizationLevel))
            {
                communication.SendPublicChatMessage($"TTS Message queued for approval, @{chatter.User.TwitchUserName}.");
                ttsHandler.HandleTTS(
                    user: chatter.User,
                    message: string.Join(' ', remainingCommand),
                    approved: false);
            }
            else
            {
                ttsHandler.HandleTTS(
                    user: chatter.User,
                    message: string.Join(' ', remainingCommand),
                    approved: true);
            }
        }

        private async Task HandleTTSSetRequest(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (!ttsConfig.Enabled)
            {
                //TTS disabled entirely
                return;
            }

            if (!ttsConfig.CanCustomize(chatter.User.AuthorizationLevel))
            {
                //No set permissions
                communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
                return;
            }

            if (remainingCommand == null || remainingCommand.Length == 0)
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no TTS setting specified.");
                return;
            }

            string settingName = remainingCommand[0].ToLowerInvariant();

            switch (settingName)
            {
                case "voice":
                    if (remainingCommand.Length == 1)
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no TTS voice specified.");
                        return;
                    }

                    TTSVoice voicePreference = remainingCommand[1].TranslateTTSVoice();

                    if (voicePreference == TTSVoice.MAX)
                    {
                        //Invalid voice
                        communication.SendPublicChatMessage(
                            $"@{chatter.User.TwitchUserName}, TTS Voice not in approved list: https://tas.wtf/info/tts#setting-voice " +
                            $"submitted: ({remainingCommand[1]})");
                        return;
                    }

                    //Check if service is approved
                    if (!ttsConfig.IsServiceSupported(voicePreference.GetTTSService()))
                    {
                        //Invalid voice
                        communication.SendPublicChatMessage(
                            $"@{chatter.User.TwitchUserName}, TTS Service {voicePreference.GetTTSService()} for voice {voicePreference.Serialize()} is not enabled.");
                        return;
                    }

                    if (voicePreference.IsNeuralVoice() && !ttsConfig.CanUseNeuralVoice(chatter.User.AuthorizationLevel))
                    {
                        //Invalid neural voice
                        communication.SendPublicChatMessage(
                            $"@{chatter.User.TwitchUserName}, you are not authorized to select neural voice {voicePreference.Serialize()}.");
                        return;
                    }


                    {
                        //Accepted voice
                        using IServiceScope scope = scopeFactory.CreateScope();
                        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();
                        User dbUser = await db.Users.FindAsync(chatter.User.UserId);
                        dbUser.TTSVoicePreference = voicePreference;
                        await db.SaveChangesAsync();

                        communication.SendPublicChatMessage(
                            $"@{chatter.User.TwitchUserName}, your TTS Voice has been updated.");

                        communication.SendDebugMessage($"Updated User voice preference: {chatter.User.TwitchUserName} to {voicePreference}");
                        await db.SaveChangesAsync();
                    }
                    break;

                case "pitch":
                    if (remainingCommand.Length == 1)
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no TTS pitch specified.");
                        return;
                    }

                    TTSPitch pitchPreference = remainingCommand[1].TranslateTTSPitch();

                    if (pitchPreference == TTSPitch.MAX)
                    {
                        //Invalid pitch
                        communication.SendPublicChatMessage(
                            $"@{chatter.User.TwitchUserName}, " +
                            $"TTS Pitch not in approved list: https://tas.wtf/info/tts#setting-pitch " +
                            $"submitted: ({remainingCommand[1]})");
                    }
                    else
                    {
                        //Accepted pitch
                        using IServiceScope scope = scopeFactory.CreateScope();
                        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();
                        User dbUser = await db.Users.FindAsync(chatter.User.UserId);
                        dbUser.TTSPitchPreference = pitchPreference;
                        await db.SaveChangesAsync();

                        communication.SendPublicChatMessage(
                            $"@{chatter.User.TwitchUserName}, your TTS Pitch has been updated.");

                        communication.SendDebugMessage($"Updated User TTS Pitch preference: {chatter.User.TwitchUserName} to {pitchPreference}");
                        await db.SaveChangesAsync();
                    }
                    break;

                case "speed":
                    if (remainingCommand.Length == 1)
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no TTS speed specified.");
                        return;
                    }

                    TTSSpeed speedPreference = remainingCommand[1].TranslateTTSSpeed();

                    if (speedPreference == TTSSpeed.MAX)
                    {
                        //Invalid speed
                        communication.SendPublicChatMessage(
                            $"@{chatter.User.TwitchUserName}, " +
                            $"TTS Speed not in approved list: https://tas.wtf/info/tts#setting-speed " +
                            $"submitted: ({remainingCommand[1]})");
                    }
                    else
                    {
                        //Accepted speed
                        using IServiceScope scope = scopeFactory.CreateScope();
                        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();
                        User dbUser = await db.Users.FindAsync(chatter.User.UserId);
                        dbUser.TTSSpeedPreference = speedPreference;
                        await db.SaveChangesAsync();

                        communication.SendPublicChatMessage(
                            $"@{chatter.User.TwitchUserName}, your TTS Speed has been updated.");

                        communication.SendDebugMessage($"Updated User TTS Speed preference: {chatter.User.TwitchUserName} to {speedPreference}");
                        await db.SaveChangesAsync();
                    }
                    break;

                case "effect":
                case "effects":
                case "effectchain":
                case "effect_chain":
                case "effectschain":
                case "effects_chain":
                    if (remainingCommand.Length == 1)
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no TTS effects specified.");
                        return;
                    }

                    //Try to parse
                    if (!audioEffectSystem.TryParse(string.Join(' ', remainingCommand[1..]), out Effect parsedEffects, out string errorMessage))
                    {
                        //Parsing failed
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unable to parse effects chain: {errorMessage}");
                        return;
                    }

                    if (parsedEffects is null)
                    {
                        //Process failed, somehow
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unable to parse effects chain.");
                        return;
                    }

                    {
                        using IServiceScope scope = scopeFactory.CreateScope();
                        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();
                        User dbUser = await db.Users.FindAsync(chatter.User.UserId);
                        dbUser.TTSEffectsChain = parsedEffects.GetEffectsChain();
                        await db.SaveChangesAsync();
                    }

                    communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, your TTS Effect has been updated.");

                    communication.SendDebugMessage($"Updated User TTS EffectChain: {chatter.User.TwitchUserName} to {parsedEffects.GetEffectsChain()}");
                    break;

                default:
                    communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, TTS setting not recognized ({string.Join(' ', remainingCommand)}).");
                    break;
            }
        }

        private static string GetUserTTSSettings(User user) =>
            $"{DisplayTTSVoice(user.TTSVoicePreference)} with {DisplayTTSPitch(user.TTSPitchPreference)} pitch and {DisplayTTSSpeed(user.TTSSpeedPreference)} speed. Effects: {user.TTSEffectsChain}";

        private static string DisplayTTSVoice(TTSVoice voice)
        {
            if (voice == TTSVoice.Unassigned)
            {
                return "Joanna";
            }

            return voice.Serialize();
        }

        private static string DisplayTTSPitch(TTSPitch pitch) =>
            pitch switch
            {
                TTSPitch.X_Low => "X-Low",
                TTSPitch.Low => "Low",
                TTSPitch.High => "High",
                TTSPitch.X_High => "X-High",
                TTSPitch.Unassigned or TTSPitch.Medium => "Normal",
                _ => "Normal",
            };

        private static string DisplayTTSSpeed(TTSSpeed speed) =>
            speed switch
            {
                TTSSpeed.X_Slow => "X-Slow",
                TTSSpeed.Slow => "Slow",
                TTSSpeed.Fast => "Fast",
                TTSSpeed.X_Fast => "X-Fast",
                TTSSpeed.Unassigned or TTSSpeed.Medium => "Normal",
                _ => "Normal",
            };

        private async Task HandleTTSGetRequest(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (remainingCommand == null || remainingCommand.Length == 0)
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, your TTS settings: {GetUserTTSSettings(chatter.User)}");
                return;
            }

            if (remainingCommand.Length == 1 && remainingCommand[0].StartsWith('@'))
            {
                //Try to find other user

                string userName = remainingCommand[0];

                //Strip off optional leading @
                if (userName.StartsWith('@'))
                {
                    userName = userName[1..].ToLower();
                }

                {
                    string lowerUserName = userName.ToLower();
                    using IServiceScope scope = scopeFactory.CreateScope();
                    BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();
                    User dbUser = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserName.ToLower() == lowerUserName);

                    if (dbUser is null)
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no user named {userName} found.");
                        return;
                    }

                    communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, the TTS settings of @{dbUser.TwitchUserName}: {GetUserTTSSettings(dbUser)}");
                    return;
                }
            }

            string settingName = remainingCommand[0].ToLowerInvariant();

            switch (settingName)
            {
                case "voice":
                case "pitch":
                case "effect":
                case "effects":
                case "effectchain":
                case "effect_chain":
                    if (remainingCommand.Length == 1)
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, your TTS settings: {GetUserTTSSettings(chatter.User)}");
                        return;
                    }

                    if (remainingCommand.Length == 2 && remainingCommand[1].Length > 1)
                    {
                        //Try to find other user

                        string userName = remainingCommand[1];

                        //Strip off optional leading @
                        if (userName.StartsWith('@'))
                        {
                            userName = userName[1..].ToLower();
                        }

                        {
                            string lowerUserName = userName.ToLower();
                            using IServiceScope scope = scopeFactory.CreateScope();
                            BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();
                            User dbUser = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserName.ToLower() == lowerUserName);

                            if (dbUser is null)
                            {
                                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no user named {userName} found.");
                                return;
                            }

                            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, the TTS settings of @{dbUser.TwitchUserName}: {GetUserTTSSettings(dbUser)}");
                            return;
                        }
                    }
                    else
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unable to query specified TTS settings.");
                    }
                    break;

                default:
                    communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, TTS setting not recognized ({string.Join(' ', remainingCommand)}).");
                    break;
            }
        }
    }
}
