using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.Commands;
using TASagentTwitchBot.Core.Database;
using Microsoft.Extensions.DependencyInjection;

namespace TASagentTwitchBot.Core.TTS
{
    public class TTSSystem : ICommandContainer
    {
        //Subsystems
        private readonly IServiceScopeFactory scopeFactory;
        private readonly Config.IBotConfigContainer botConfigContainer;
        private readonly ICommunication communication;
        private readonly IAudioEffectSystem audioEffectSystem;
        private readonly Notifications.ITTSHandler ttsHandler;

        private bool enabled = true;

        public TTSSystem(
            Config.IBotConfigContainer botConfigContainer,
            ICommunication communication,
            IAudioEffectSystem audioEffectSystem,
            Notifications.ITTSHandler ttsHandler,
            IServiceScopeFactory scopeFactory)
        {
            this.botConfigContainer = botConfigContainer;
            this.communication = communication;
            this.audioEffectSystem = audioEffectSystem;
            this.ttsHandler = ttsHandler;
            this.scopeFactory = scopeFactory;
        }

        public void RegisterCommands(
            Dictionary<string, CommandHandler> commands,
            Dictionary<string, HelpFunction> helpFunctions,
            Dictionary<string, SetFunction> setFunctions)
        {
            commands.Add("tts", HandleTTSRequest);
            commands.Add("faketts", HandleFakeTTSRequest);
            helpFunctions.Add("tts", HandleTTSHelpRequest);
            setFunctions.Add("tts", HandleTTSSetRequest);
        }

        public IEnumerable<string> GetPublicCommands()
        {
            if (enabled)
            {
                yield return "tts";
            }
        }

        private string HandleTTSHelpRequest(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Elevated)
            {
                return $"Looks like you're not authorized to use the TTS system, {chatter.User.TwitchUserName}";
            }

            if (remainingCommand == null || remainingCommand.Length == 0)
            {
                return "TTS Command - Send text as spoken audio to the stream with: !tts <text>  For more information, visit info.tas.wtf/twitchBotFeatures/tts.html";
            }
            else if (remainingCommand[0].ToLower() == "voice")
            {
                return "TTS Voice - Set your personal TTS Voice with !set tts voice <Voice>. Eg justin, joanna, brian, or en-US-Standard-B.  For more information, visit info.tas.wtf/twitchBotFeatures/tts.html#tts-voice-personalization";
            }
            else if (remainingCommand[0].ToLower() == "pitch")
            {
                return "TTS Pitch - Set your personal TTS Voice pitch with !set tts pitch <Pitch>. Eg normal, x-low, low, high, x-high.  For more information, visit info.tas.wtf/twitchBotFeatures/tts.html#supported-tts-pitches";
            }
            else if (remainingCommand[0].ToLower() == "sounds")
            {
                return "TTS Sounds - Add sounds to your TTS with commands like /bao, /midway, /jump, /kick, /pipe, /powerup.  For more information, visit info.tas.wtf/twitchBotFeatures/tts.html#tts-sound-effects-extension";
            }
            else
            {
                return $"No TTS subcommand found: {string.Join(' ', remainingCommand)}";
            }
        }

        private async Task HandleFakeTTSRequest(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (!enabled && chatter.User.AuthorizationLevel != AuthorizationLevel.Admin)
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, I'm afraid the TTS system is currently disabled.");
                return;
            }

            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Admin)
            {
                communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
                return;
            }

            if (remainingCommand is null || remainingCommand.Length == 0)
            {
                //TTS Error
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, missing a username to fake a TTS from.");
                return;
            }

            string username = remainingCommand[0].ToLower();

            using IServiceScope scope = scopeFactory.CreateScope();
            BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

            User fakeTTSUser = db.Users.FirstOrDefault(x => x.TwitchUserName.ToLower() == username);
            
            if (fakeTTSUser is null)
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unable to find \"{username}\" in db.");
                return;
            }

            remainingCommand = remainingCommand[1..];


            IRC.TwitchChatter fakeChatter = new IRC.TwitchChatter()
            { 
                User = fakeTTSUser,
                CreatedAt = chatter.CreatedAt,
                Badges = chatter.Badges,
                Message = $"!tts {string.Join(' ', remainingCommand)}",
                MessageId = chatter.MessageId,
                Whisper = chatter.Whisper,
                Bits = 0
            };

            await HandleTTSRequest(fakeChatter, remainingCommand);
        }

        private async Task HandleTTSRequest(IRC.TwitchChatter chatter, string[] remainingCommand)
        {
            if (!enabled && chatter.User.AuthorizationLevel != AuthorizationLevel.Admin)
            {
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, I'm afraid the TTS system is currently disabled.");
                return;
            }

            if (chatter.User.AuthorizationLevel < AuthorizationLevel.None)
            {
                communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
                return;
            }

            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator &&
                chatter.User.LastSuccessfulTTS.HasValue &&
                DateTime.Now < chatter.User.LastSuccessfulTTS.Value + new TimeSpan(hours: 0, minutes: 0, seconds: botConfigContainer.BotConfig.TTSTimeoutTime))
            {
                communication.SendDebugMessage($"User {chatter.User.TwitchUserName} rebuked for TTS Spam");
                communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, you must wait before you can do that again.");
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

            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Elevated)
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
            if (chatter.User.AuthorizationLevel < AuthorizationLevel.Elevated)
            {
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
                case "enabled":
                case "disabled":
                    if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
                    {
                        communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
                        return;
                    }

                    enabled = (settingName == "enabled");
                    communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, TTS service has been {settingName}.");
                    break;

                case "bitthreshold":
                    if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
                    {
                        communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
                        return;
                    }

                    if (remainingCommand.Length == 1)
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no Bit Threshold specified.");
                        return;
                    }

                    if (!int.TryParse(remainingCommand[1], out int bitThreshold))
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unable to parse Bit Threshold {remainingCommand[1]}.");
                        return;
                    }

                    if (bitThreshold < 0)
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, nonsense Bit Threshold {bitThreshold}.");
                        return;
                    }

                    botConfigContainer.BotConfig.BitTTSThreshold = bitThreshold;
                    botConfigContainer.SerializeData();
                    break;

                case "timeout":
                    if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
                    {
                        communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
                        return;
                    }

                    if (remainingCommand.Length == 1)
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no timeout time specified.");
                        return;
                    }

                    if (!int.TryParse(remainingCommand[1], out int timeoutValue))
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, unable to parse timeout time {remainingCommand[1]}.");
                        return;
                    }

                    if (timeoutValue < 0)
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, nonsense timeout time {timeoutValue}.");
                        return;
                    }

                    botConfigContainer.BotConfig.TTSTimeoutTime = timeoutValue;
                    botConfigContainer.SerializeData();
                    break;

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
                            $"TTS Voice not in approved list: info.tas.wtf/twitchBotFeatures/tts.html#supported-tts-voices " +
                            $"submitted: ({remainingCommand[1]})");
                    }
                    else
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
                            $"TTS Pitch not in approved list: info.tas.wtf/twitchBotFeatures/tts.html#supported-tts-pitches " +
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

                case "effect":
                case "effects":
                case "effectchain":
                case "effect_chain":
                    if (remainingCommand.Length == 1)
                    {
                        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no TTS effects specified.");
                        return;
                    }

                    Effect parsedEffects = null;

                    if (remainingCommand.Length == 2)
                    {
                        parsedEffects = remainingCommand[1].TranslateTTSEffect();
                    }

                    if (parsedEffects is null)
                    {
                        parsedEffects = audioEffectSystem.Parse(string.Join(' ', remainingCommand[1..]));
                    }

                    if (parsedEffects is null)
                    {
                        //Parsing failed
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

                    communication.SendPublicChatMessage(
                        $"@{chatter.User.TwitchUserName}, your TTS Effect has been updated.");

                    communication.SendDebugMessage($"Updated User TTS EffectChain: {chatter.User.TwitchUserName} to {parsedEffects.GetEffectsChain()}");
                    break;

                default:
                    communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, TTS setting not recognized ({string.Join(' ', remainingCommand)}).");
                    break;
            }
        }
    }
}
