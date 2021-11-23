using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.TTS;
using TASagentTwitchBot.Core.TTS.Parsing;
using TASagentTwitchBot.Core.WebServer.Controllers;
using TASagentTwitchBot.Core.WebServer.Models;

namespace TASagentTwitchBot.Core.WebServer.TTS;

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

    private readonly Google.Cloud.TextToSpeech.V1.TextToSpeechClient? googleClient = null;
    private readonly Amazon.Polly.AmazonPollyClient? amazonClient = null;
    private readonly Microsoft.CognitiveServices.Speech.SpeechConfig? azureClient = null;

    public ServerTTSRenderer(
        ILogger<ServerTTSRenderer> logger,
        IHubContext<Web.Hubs.BotTTSHub> botTTSHub)
    {
        this.logger = logger;
        this.botTTSHub = botTTSHub;


        //
        // Prepare Google TTS
        //
        try
        {
            Google.Cloud.TextToSpeech.V1.TextToSpeechClientBuilder builder = new Google.Cloud.TextToSpeech.V1.TextToSpeechClientBuilder();

            string googleCredentialsPath = BGC.IO.DataManagement.PathForDataFile("Config", "googleCloudCredentials.json");

            if (!File.Exists(googleCredentialsPath))
            {
                throw new FileNotFoundException($"Could not find credentials for Google TTS at {googleCredentialsPath}");
            }

            builder.CredentialsPath = googleCredentialsPath;
            googleClient = builder.Build();
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Exception thrown while trying to initialize Google TTS.");
        }

        //
        // Prepare Amazon TTS
        //
        try
        {
            string awsCredentialsPath = BGC.IO.DataManagement.PathForDataFile("Config", "awsPollyCredentials.json");

            if (!File.Exists(awsCredentialsPath))
            {
                throw new FileNotFoundException($"Could not find credentials for AWS Polly at {awsCredentialsPath}");
            }

            AWSPollyCredentials awsPolyCredentials = JsonSerializer.Deserialize<AWSPollyCredentials>(File.ReadAllText(awsCredentialsPath))!;

            Amazon.Runtime.BasicAWSCredentials awsCredentials = new Amazon.Runtime.BasicAWSCredentials(
                awsPolyCredentials.AccessKey,
                awsPolyCredentials.SecretKey);

            amazonClient = new Amazon.Polly.AmazonPollyClient(awsCredentials, Amazon.RegionEndpoint.USWest2);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Exception thrown while trying to initialize AWS Polly.");
        }

        //
        // Prepare Azure TTS
        //
        try
        {
            string azureCredentialsPath = BGC.IO.DataManagement.PathForDataFile("Config", "azureSpeechSynthesisCredentials.json");

            if (!File.Exists(azureCredentialsPath))
            {
                throw new FileNotFoundException($"Could not find credentials for Azure SpeechSynthesis at {azureCredentialsPath}");
            }

            AzureSpeechSynthesisCredentials azureCredentials = JsonSerializer.Deserialize<AzureSpeechSynthesisCredentials>(File.ReadAllText(azureCredentialsPath))!;

            azureClient = Microsoft.CognitiveServices.Speech.SpeechConfig.FromSubscription(azureCredentials.AccessKey, azureCredentials.Region);
            azureClient.SetSpeechSynthesisOutputFormat(Microsoft.CognitiveServices.Speech.SpeechSynthesisOutputFormat.Audio24Khz48KBitRateMonoMp3);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Exception thrown while trying to initialize Azure Speech Synthesis.");
        }
    }


    public async Task HandleTTSRequest(UserManager<ApplicationUser> userManager, ApplicationUser user, ServerTTSRequest ttsRequest)
    {
        if (string.IsNullOrEmpty(user.TwitchBroadcasterId))
        {
            logger.LogWarning("Received TTS Request from user {UserName} with null or empty BroadcasterId", user.UserName);
            return;
        }

        try
        {
            int requestedCharacters = ttsRequest.Ssml.Length;

            //Check permissions
            if (ttsRequest.Voice.IsNeuralVoice())
            {
                if (!await userManager.IsInRoleAsync(user, "TTSNeural"))
                {
                    //Fix neural request for unauthorized user
                    ttsRequest = ttsRequest with { Voice = TTSVoice.Unassigned };

                    logger.LogInformation("{UserName} requested unauthorized Neural TTS", user.TwitchBroadcasterName);

                    await botTTSHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
                        method: "ReceiveWarning",
                        arg1: $"Submitted a Neural TTS request when you're not authorized to use Neural voices. Changing to default.");
                }
            }

            //Check Neural voices
            if (ttsRequest.Voice.IsNeuralVoice())
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

            StandardTTSSystemRenderer renderer;

            switch (ttsRequest.Voice.GetTTSService())
            {
                case TTSService.Amazon:
                    renderer = new AmazonTTSLocalRenderer(amazonClient!, logger, ttsRequest.Voice, ttsRequest.Pitch, ttsRequest.Speed, new NoEffect());
                    break;

                case TTSService.Google:
                    renderer = new GoogleTTSLocalRenderer(googleClient!, logger, ttsRequest.Voice, ttsRequest.Pitch, ttsRequest.Speed, new NoEffect());
                    break;

                case TTSService.Azure:
                    renderer = new AzureTTSLocalRenderer(azureClient!, logger, ttsRequest.Voice, ttsRequest.Pitch, ttsRequest.Speed, new NoEffect());
                    break;

                default:
                    throw new Exception($"Unrecognized Service: {ttsRequest.Voice.GetTTSService()}");
            }

            string? fileName = await renderer.SynthesizeSpeech(ttsRequest.Ssml);

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

            TTSVoice voice = ttsRequest.Voice?.TranslateTTSVoice() ?? TTSVoice.Unassigned;
            TTSPitch pitch = ttsRequest.Pitch?.TranslateTTSPitch() ?? TTSPitch.Unassigned;
            TTSSpeed speed = ttsRequest.Speed?.TranslateTTSSpeed() ?? TTSSpeed.Unassigned;

            //Check permissions
            if (voice.IsNeuralVoice())
            {
                if (!await userManager.IsInRoleAsync(user, "TTSNeural"))
                {
                    //Fix neural request for unauthorized user
                    voice = TTSVoice.Unassigned;

                    logger.LogInformation("{UserName} requested unauthorized Neural TTS", user.TwitchBroadcasterName);

                    await botTTSHub.Clients.Groups(user.TwitchBroadcasterId).SendAsync(
                        method: "ReceiveWarning",
                        arg1: $"Submitted a Neural TTS request when you're not authorized to use Neural voices. Changing to default.");
                }
            }

            //Check Neural voices
            if (voice.IsNeuralVoice())
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

            StandardTTSSystemRenderer renderer;

            switch (voice.GetTTSService())
            {
                case TTSService.Amazon:
                    renderer = new AmazonTTSLocalRenderer(amazonClient!, logger, voice, pitch, speed, new NoEffect());
                    break;

                case TTSService.Google:
                    renderer = new GoogleTTSLocalRenderer(googleClient!, logger, voice, pitch, speed, new NoEffect());
                    break;

                case TTSService.Azure:
                    renderer = new AzureTTSLocalRenderer(azureClient!, logger, voice, pitch, speed, new NoEffect());
                    break;

                default:
                    throw new Exception($"Unrecognized Service: {voice.GetTTSService()}");
            }

            (string? fileName, int finalCharCount) = await TTSParser.ParseTTSNoSoundEffects(ttsRequest.Text, renderer);

            if (string.IsNullOrEmpty(fileName))
            {
                //Failed somewhere
                throw new Exception("Received null filename.");
            }

            //Check Neural voices
            if (voice.IsNeuralVoice())
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

            TTSVoice voice = rawTTSRequest.Voice?.TranslateTTSVoice() ?? TTSVoice.Unassigned;
            TTSPitch pitch = rawTTSRequest.Pitch?.TranslateTTSPitch() ?? TTSPitch.Unassigned;
            TTSSpeed speed = rawTTSRequest.Speed?.TranslateTTSSpeed() ?? TTSSpeed.Unassigned;

            //Check permissions
            if (voice.IsNeuralVoice())
            {
                if (!await userManager.IsInRoleAsync(user, "TTSNeural"))
                {
                    //Fix neural request for unauthorized user
                    voice = TTSVoice.Unassigned;
                    logger.LogInformation("{UserName} requested unauthorized Neural TTS", user.TwitchBroadcasterName);
                }
            }

            //Check Neural voices
            if (voice.IsNeuralVoice())
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

            StandardTTSSystemRenderer renderer;

            switch (voice.GetTTSService())
            {
                case TTSService.Amazon:
                    renderer = new AmazonTTSLocalRenderer(amazonClient!, logger, voice, pitch, speed, new NoEffect());
                    break;

                case TTSService.Google:
                    renderer = new GoogleTTSLocalRenderer(googleClient!, logger, voice, pitch, speed, new NoEffect());
                    break;

                case TTSService.Azure:
                    renderer = new AzureTTSLocalRenderer(azureClient!, logger, voice, pitch, speed, new NoEffect());
                    break;

                default:
                    throw new Exception($"Unrecognized Service: {voice.GetTTSService()}");
            }

            (string? fileName, int finalCharCount) = await TTSParser.ParseTTSNoSoundEffects(rawTTSRequest.Text, renderer);

            if (string.IsNullOrEmpty(fileName))
            {
                //Failed somewhere
                throw new Exception("Received null filename.");
            }

            //Check Neural voices
            if (voice.IsNeuralVoice())
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
