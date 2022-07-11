using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.TTS;
using TASagentTwitchBot.Core.TTS.Parsing;
using TASagentTwitchBot.Core.WebServer.Controllers;
using TASagentTwitchBot.Core.WebServer.Models;

namespace TASagentTwitchBot.Core.WebServer.TTS;

[AutoRegister]
public interface IServerTTSRenderer
{
    Task HandleTTSRequest(UserManager<ApplicationUser> userManager, ApplicationUser user, ServerTTSRequest ttsRequest);
    Task HandleRawTTSRequest(UserManager<ApplicationUser> userManager, ApplicationUser user, Web.Hubs.RawServerTTSRequest rawTTSRequest);
    Task<byte[]?> HandleRawExternalTTSRequest(UserManager<ApplicationUser> userManager, ApplicationUser user, RawServerExternalTTSRequest rawTTSRequest);
}

public class ServerTTSRenderer : IServerTTSRenderer
{
    private readonly ILogger<ServerTTSRenderer> logger;
    private readonly IHubContext<Web.Hubs.BotTTSHub> botTTSHub;

    private readonly ITTSSystem[] ttsSystems;
    private readonly Dictionary<string, ITTSSystem> voiceLookup = new Dictionary<string, ITTSSystem>();

    public ServerTTSRenderer(
        ILogger<ServerTTSRenderer> logger,
        IHubContext<Web.Hubs.BotTTSHub> botTTSHub,
        IEnumerable<ITTSSystem> ttsSystems)
    {
        this.logger = logger;
        this.botTTSHub = botTTSHub;

        this.ttsSystems = ttsSystems.ToArray();

        foreach (ITTSSystem system in this.ttsSystems)
        {
            foreach (string voice in system.GetVoices())
            {
                voiceLookup.Add(voice.ToLowerInvariant(), system);
            }
        }
    }

    public async Task HandleTTSRequest(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        ServerTTSRequest ttsRequest)
    {
        if (string.IsNullOrEmpty(user.TwitchBroadcasterId))
        {
            logger.LogWarning("Received TTS Request from user {UserName} with null or empty BroadcasterId", user.UserName);
            return;
        }

        try
        {
            int requestedCharacters = ttsRequest.Ssml.Length;

            //Deduce TTS System
            if (!voiceLookup.TryGetValue(ttsRequest.Voice.ToLowerInvariant(), out ITTSSystem? ttsSystem))
            {
                ttsSystem = ttsSystems[0];
            }

            TTSVoiceInfo voiceInfo = ttsSystem.GetTTSVoiceInfo(ttsRequest.Voice);

            //Check permissions
            if (voiceInfo.IsNeural)
            {
                if (!await userManager.IsInRoleAsync(user, "TTSNeural"))
                {
                    //Fix neural request for unauthorized user
                    ttsRequest = ttsRequest with { Voice = ttsSystem.GetDefaultVoice() };
                    //Refresh TTSVoiceInfo
                    voiceInfo = ttsSystem.GetTTSVoiceInfo(ttsRequest.Voice);

                    logger.LogInformation("{UserName} requested unauthorized Neural TTS", user.TwitchBroadcasterName);

                    await botTTSHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
                        method: "ReceiveWarning",
                        arg1: $"Submitted a Neural TTS request when you're not authorized to use Neural voices. Changing to default.");
                }
            }

            //Check Neural voices and apply increased cost
            if (voiceInfo.IsNeural)
            {
                //Neural voices are 4 times the cost across all services
                requestedCharacters *= 4;
            }

            if (user.MonthlyTTSLimit != -1 &&
                (user.MonthlyTTSUsage + requestedCharacters) >= user.MonthlyTTSLimit)
            {
                logger.LogInformation("{UserName} hit monthly allocation", user.TwitchBroadcasterName);

                await botTTSHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
                        method: "CancelRequest",
                        arg1: ttsRequest.RequestIdentifier,
                        arg2: $"This adjusted TTS request of {requestedCharacters} characters " +
                            $"(combinded with Month-to-Date usage of {user.MonthlyTTSUsage} characters) " +
                            $"would exceed monthly allotment of {user.MonthlyTTSLimit} characters.");

                return;
            }

            //Update usage
            user.MonthlyTTSUsage += requestedCharacters;
            await userManager.UpdateAsync(user);

            TTSSystemRenderer renderer = ttsSystem.CreateRenderer(ttsRequest.Voice, ttsRequest.Pitch, ttsRequest.Speed, new NoEffect());

            if (renderer is not StandardTTSSystemRenderer standardTTSRenderer)
            {
                //Only standard renderers supported for web at the moment
                throw new NotSupportedException($"Non-standard renderer received: {renderer}");
            }

            string? fileName = await standardTTSRenderer.SynthesizeSpeech(ttsRequest.Ssml);

            if (string.IsNullOrEmpty(fileName))
            {
                //Failed somewhere
                throw new Exception("Received null filename.");
            }

            if (!File.Exists(fileName))
            {
                //Failed somewhere
                throw new Exception("Received filename not found.");
            }

            //Now stream the file back to the requester
            using FileStream file = new FileStream(fileName, FileMode.Open);
            int totalData = (int)file.Length;
            int dataPacketSize = Math.Min(totalData, 1 << 13);
            byte[] dataPacket = new byte[dataPacketSize];

            int bytesReady;
            while ((bytesReady = await file.ReadAsync(dataPacket)) > 0)
            {
                await botTTSHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
                    method: "ReceiveData",
                    arg1: ttsRequest.RequestIdentifier,
                    arg2: dataPacket,
                    arg3: bytesReady,
                    arg4: totalData);
            }

            file.Close();

            //Delete file from system
            File.Delete(fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception caught trying to render TTS");

            await botTTSHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
                    method: "CancelRequest",
                    arg1: ttsRequest.RequestIdentifier,
                    arg2: "An exception was encountered trying to render TTS");
        }
    }


    public async Task HandleRawTTSRequest(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        Web.Hubs.RawServerTTSRequest ttsRequest)
    {
        if (string.IsNullOrEmpty(user.TwitchBroadcasterId))
        {
            logger.LogWarning("Received TTS Request from user {UserName} with null or empty BroadcasterId", user.UserName);
            return;
        }

        try
        {
            int requestedCharacters = ttsRequest.Text.Length;

            string voice = ttsRequest.Voice ?? "unassigned";
            TTSPitch pitch = ttsRequest.Pitch?.TranslateTTSPitch() ?? TTSPitch.Unassigned;
            TTSSpeed speed = ttsRequest.Speed?.TranslateTTSSpeed() ?? TTSSpeed.Unassigned;

            //Deduce TTS System
            if (!voiceLookup.TryGetValue(voice.ToLowerInvariant(), out ITTSSystem? ttsSystem))
            {
                ttsSystem = ttsSystems[0];
            }

            TTSVoiceInfo voiceInfo = ttsSystem.GetTTSVoiceInfo(voice);

            //Check permissions
            if (voiceInfo.IsNeural)
            {
                if (!await userManager.IsInRoleAsync(user, "TTSNeural"))
                {
                    //Fix neural request for unauthorized user
                    voice = ttsSystem.GetDefaultVoice();

                    //Refresh TTSVoiceInfo
                    voiceInfo = ttsSystem.GetTTSVoiceInfo(voice);

                    logger.LogInformation("{UserName} requested unauthorized Neural TTS", user.TwitchBroadcasterName);

                    await botTTSHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
                        method: "ReceiveWarning",
                        arg1: $"Submitted a Neural TTS request when you're not authorized to use Neural voices. Changing to default.");
                }
            }

            //Check Neural voices
            if (voiceInfo.IsNeural)
            {
                //Neural voices are 4 times the cost across all services
                requestedCharacters *= 4;
            }

            if (user.MonthlyTTSLimit != -1 &&
                (user.MonthlyTTSUsage + requestedCharacters) >= user.MonthlyTTSLimit)
            {
                logger.LogInformation("{UserName} hit monthly allocation", user.TwitchBroadcasterName);

                await botTTSHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
                        method: "CancelRequest",
                        arg1: ttsRequest.RequestIdentifier,
                        arg2: $"This adjusted TTS request of {requestedCharacters} characters " +
                            $"(combinded with Month-to-Date usage of {user.MonthlyTTSUsage} characters) " +
                            $"would exceed monthly allotment of {user.MonthlyTTSLimit} characters.");

                return;
            }

            //Update initial usage
            user.MonthlyTTSUsage += requestedCharacters;
            await userManager.UpdateAsync(user);

            TTSSystemRenderer renderer = ttsSystem.CreateRenderer(voice, pitch, speed, new NoEffect());

            (string? fileName, int finalCharCount) = await TTSParser.ParseTTSNoSoundEffects(ttsRequest.Text, renderer);

            if (string.IsNullOrEmpty(fileName))
            {
                //Failed somewhere
                throw new Exception("Received null filename.");
            }

            //Check Neural voices
            if (voiceInfo.IsNeural)
            {
                //Neural voices are 4 times the cost across all services
                finalCharCount *= 4;
            }

            if (finalCharCount > requestedCharacters)
            {
                //Update final actual usage
                user.MonthlyTTSUsage += finalCharCount - requestedCharacters;
                await userManager.UpdateAsync(user);
            }

            if (!File.Exists(fileName))
            {
                //Failed somewhere
                throw new Exception("Received filename not found.");
            }

            //Now stream the file back to the requester
            using FileStream file = new FileStream(fileName, FileMode.Open);
            int totalData = (int)file.Length;
            int dataPacketSize = Math.Min(totalData, 1 << 13);
            byte[] dataPacket = new byte[dataPacketSize];

            int bytesReady;
            while ((bytesReady = await file.ReadAsync(dataPacket)) > 0)
            {
                await botTTSHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
                    method: "ReceiveData",
                    arg1: ttsRequest.RequestIdentifier,
                    arg2: dataPacket,
                    arg3: bytesReady,
                    arg4: totalData);
            }

            file.Close();

            //Delete file from system
            File.Delete(fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception caught trying to render TTS");

            await botTTSHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
                    method: "CancelRequest",
                    arg1: ttsRequest.RequestIdentifier,
                    arg2: "An exception was encountered trying to render TTS");
        }
    }

    public async Task<byte[]?> HandleRawExternalTTSRequest(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        RawServerExternalTTSRequest rawTTSRequest)
    {
        try
        {
            int requestedCharacters = rawTTSRequest.Text.Length;

            string voice = rawTTSRequest.Voice ?? "unassigned";
            TTSPitch pitch = rawTTSRequest.Pitch?.TranslateTTSPitch() ?? TTSPitch.Unassigned;
            TTSSpeed speed = rawTTSRequest.Speed?.TranslateTTSSpeed() ?? TTSSpeed.Unassigned;

            //Deduce TTS System
            if (!voiceLookup.TryGetValue(voice.ToLowerInvariant(), out ITTSSystem? ttsSystem))
            {
                ttsSystem = ttsSystems[0];
            }

            TTSVoiceInfo voiceInfo = ttsSystem.GetTTSVoiceInfo(voice);

            //Check permissions
            if (voiceInfo.IsNeural)
            {
                if (!await userManager.IsInRoleAsync(user, "TTSNeural"))
                {
                    //Fix neural request for unauthorized user
                    voice = ttsSystem.GetDefaultVoice();

                    //Refresh TTSVoiceInfo
                    voiceInfo = ttsSystem.GetTTSVoiceInfo(voice);

                    logger.LogInformation("{UserName} requested unauthorized Neural TTS", user.TwitchBroadcasterName);
                }
            }

            //Check Neural voices
            if (voiceInfo.IsNeural)
            {
                //Neural voices are 4 times the cost across all services
                requestedCharacters *= 4;
            }

            if (user.MonthlyTTSLimit != -1 &&
                (user.MonthlyTTSUsage + requestedCharacters) >= user.MonthlyTTSLimit)
            {
                logger.LogInformation("{UserName} hit monthly allocation", user.TwitchBroadcasterName);
                return null;
            }

            //Update initial usage
            user.MonthlyTTSUsage += requestedCharacters;
            await userManager.UpdateAsync(user);

            TTSSystemRenderer renderer = ttsSystem.CreateRenderer(voice, pitch, speed, new NoEffect());

            (string? fileName, int finalCharCount) = await TTSParser.ParseTTSNoSoundEffects(rawTTSRequest.Text, renderer);

            if (string.IsNullOrEmpty(fileName))
            {
                //Failed somewhere
                throw new Exception("Received null filename.");
            }

            //Check Neural voices
            if (voiceInfo.IsNeural)
            {
                //Neural voices are 4 times the cost across all services
                finalCharCount *= 4;
            }

            if (finalCharCount > requestedCharacters)
            {
                //Update final actual usage
                user.MonthlyTTSUsage += finalCharCount - requestedCharacters;
                await userManager.UpdateAsync(user);
            }

            if (!File.Exists(fileName))
            {
                //Failed somewhere
                throw new Exception("Received filename not found.");
            }

            //Now stream the file back to the requester
            using FileStream file = new FileStream(fileName, FileMode.Open);
            byte[] dataPacket = new byte[(int)file.Length];
            await file.ReadAsync(dataPacket);

            file.Close();

            //Delete file from system
            File.Delete(fileName);

            return dataPacket;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception caught trying to render TTS");

            return null;
        }
    }
}
