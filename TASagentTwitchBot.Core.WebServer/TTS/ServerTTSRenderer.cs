using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using TASagentTwitchBot.Core.TTS;
using TASagentTwitchBot.Core.WebServer.Models;

namespace TASagentTwitchBot.Core.WebServer.TTS
{
    public interface IServerTTSRenderer
    {
        Task HandleTTSRequest(UserManager<ApplicationUser> userManager, ApplicationUser user, ServerTTSRequest ttsRequest);
    }


    public class ServerTTSRenderer : IServerTTSRenderer
    {
        private readonly ILogger<ServerTTSRenderer> logger;
        private readonly IHubContext<Web.Hubs.BotTTSHub> botTTSHub;

        private static string TTSFilesPath => BGC.IO.DataManagement.PathForDataDirectory("TTSFiles");

        private readonly Google.Cloud.TextToSpeech.V1.TextToSpeechClient googleClient = null;
        private readonly Amazon.Polly.AmazonPollyClient amazonClient = null;
        private readonly Microsoft.CognitiveServices.Speech.SpeechConfig azureClient = null;

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

                AWSPollyCredentials awsPolyCredentials = JsonSerializer.Deserialize<AWSPollyCredentials>(File.ReadAllText(awsCredentialsPath));

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

                AzureSpeechSynthesisCredentials azureCredentials = JsonSerializer.Deserialize<AzureSpeechSynthesisCredentials>(File.ReadAllText(azureCredentialsPath));

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

                        logger.LogInformation($"{user.TwitchBroadcasterName} requested unauthorized Neural TTS");

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
                    logger.LogInformation($"{user.TwitchBroadcasterName} hit monthly allocation");

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

                (string fileName, int finalCharCount) = await SynthesizeSpeech(ttsRequest);

                if (finalCharCount > requestedCharacters)
                {
                    //Update final actual usage
                    user.MonthlyTTSUsage += finalCharCount - requestedCharacters;
                    await userManager.UpdateAsync(user);
                }

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

                using Stream file = new FileStream(fileName, FileMode.Open);
                int totalData = (int)file.Length;
                int dataPacketSize = Math.Min(totalData, 1 << 13);
                byte[] dataPacket = new byte[dataPacketSize];

                int bytesReady;

                while((bytesReady = await file.ReadAsync(dataPacket)) > 0)
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


        private async Task<(string filepath, int finalCharCount)> SynthesizeSpeech(ServerTTSRequest ttsRequest)
        {
            //Submit request
            switch (ttsRequest.Voice.GetTTSService())
            {
                case TTSService.Amazon:
                    return await SynthesizeAWSPollySpeech(ttsRequest);

                case TTSService.Google:
                    return await SynthesizeGoogleSpeech(ttsRequest);

                case TTSService.Azure:
                    return await SynthesizeAzureSpeech(ttsRequest);

                default:
                    throw new Exception($"Unsupported TTSService: {ttsRequest.Voice.GetTTSService()}");
            }
        }


        private async Task<(string filepath, int finalCharCount)> SynthesizeGoogleSpeech(ServerTTSRequest ttsRequest)
        {
            try
            {
                string ssml = $"<speak>{ttsRequest.Ssml}</speak>";

                Google.Cloud.TextToSpeech.V1.VoiceSelectionParams voiceParams = ttsRequest.Voice.GetGoogleVoiceSelectionParams();

                Google.Cloud.TextToSpeech.V1.AudioConfig config = new Google.Cloud.TextToSpeech.V1.AudioConfig
                {
                    AudioEncoding = Google.Cloud.TextToSpeech.V1.AudioEncoding.Mp3,
                    Pitch = ttsRequest.Pitch.GetSemitoneShift(),
                    SpeakingRate = ttsRequest.Speed.GetGoogleSpeed()
                };

                //TTS
                Google.Cloud.TextToSpeech.V1.SynthesisInput input = new Google.Cloud.TextToSpeech.V1.SynthesisInput
                {
                    Ssml = ssml
                };

                // Perform the Text-to-Speech request, passing the text input
                // with the selected voice parameters and audio file type
                Google.Cloud.TextToSpeech.V1.SynthesizeSpeechResponse response = await googleClient.SynthesizeSpeechAsync(input, voiceParams, config);

                // Write the binary AudioContent of the response to file.
                string filepath = Path.Combine(TTSFilesPath, $"{Guid.NewGuid()}.mp3");

                using (Stream file = new FileStream(filepath, FileMode.Create))
                {
                    response.AudioContent.WriteTo(file);
                }

                return (filepath, ssml.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("Exception caught trying to render Google TTS", ex);
            }
        }


        private async Task<(string filepath, int finalCharCount)> SynthesizeAWSPollySpeech(ServerTTSRequest ttsRequest)
        {
            try
            {
                string ssml = ttsRequest.Ssml;
                if (ttsRequest.Voice.IsNeuralVoice())
                {
                    if (ttsRequest.Speed != TTSSpeed.Medium)
                    {
                        ssml = $"<prosody rate=\"{ttsRequest.Speed.GetSpeedValue()}\">{ssml}</prosody>";
                    }
                }
                else
                {
                    if (ttsRequest.Pitch != TTSPitch.Medium || ttsRequest.Speed != TTSSpeed.Medium)
                    {
                        ssml = $"<prosody pitch=\"{ttsRequest.Pitch.GetPitchShift()}\" rate=\"{ttsRequest.Speed.GetSpeedValue()}\">{ssml}</prosody>";
                    }
                }

                if (ttsRequest.Voice.GetRequiresLangTag())
                {
                    ssml = $"<lang xml:lang=\"en-US\">{ssml}</lang>";
                }

                ssml = $"<speak>{ssml}</speak>";


                Amazon.Polly.Model.SynthesizeSpeechRequest synthesisRequest = ttsRequest.Voice.GetAmazonTTSSpeechRequest();
                synthesisRequest.TextType = Amazon.Polly.TextType.Ssml;
                synthesisRequest.Text = ssml;

                // Perform the Text-to-Speech request, passing the text input
                // with the selected voice parameters and audio file type
                Amazon.Polly.Model.SynthesizeSpeechResponse synthesisResponse = await amazonClient.SynthesizeSpeechAsync(synthesisRequest);

                // Write the binary AudioContent of the response to file.
                string filepath = Path.Combine(TTSFilesPath, $"{Guid.NewGuid()}.mp3");

                using (Stream file = new FileStream(filepath, FileMode.Create))
                {
                    await synthesisResponse.AudioStream.CopyToAsync(file);
                    await file.FlushAsync();
                    file.Close();
                }

                return (filepath, ssml.Length);
            }
            catch (Exception ex)
            {
                throw new Exception("Exception caught trying to render AWS Polly TTS", ex);
            }
        }

        private async Task<(string filepath, int finalCharCount)> SynthesizeAzureSpeech(ServerTTSRequest ttsRequest)
        {
            try
            {
                string ssml = ttsRequest.Ssml;

                if (ttsRequest.Pitch != TTSPitch.Medium || ttsRequest.Speed != TTSSpeed.Medium)
                {
                    ssml = $"<prosody pitch=\"{ttsRequest.Pitch.GetPitchShift()}\" rate=\"{ttsRequest.Speed.GetSpeedValue()}\">{ssml}</prosody>";
                }

                ssml = $"<speak version=\"1.0\" xml:lang=\"en-US\" xmlns:mstts=\"http://www.w3.org/2001/mstts\">" +
                    $"<voice name=\"{ttsRequest.Voice.GetTTSVoiceString()}\">" +
                    $"<mstts:silence type=\"Sentenceboundary\" value=\"250ms\"/>" +
                    $"<mstts:silence type=\"Tailing\" value=\"0ms\"/>" +
                    $"{ssml}" +
                    $"</voice></speak>";

                // Write the binary AudioContent of the response to file.
                string filepath = Path.Combine(TTSFilesPath, $"{Guid.NewGuid()}.mp3");

                using Microsoft.CognitiveServices.Speech.SpeechSynthesizer synthesizer = new Microsoft.CognitiveServices.Speech.SpeechSynthesizer(azureClient, null);
                using Microsoft.CognitiveServices.Speech.SpeechSynthesisResult result = await synthesizer.SpeakSsmlAsync(ssml);

                if (result.Reason == Microsoft.CognitiveServices.Speech.ResultReason.Canceled)
                {
                    Microsoft.CognitiveServices.Speech.SpeechSynthesisCancellationDetails details = Microsoft.CognitiveServices.Speech.SpeechSynthesisCancellationDetails.FromResult(result);

                    if (details.ErrorCode == Microsoft.CognitiveServices.Speech.CancellationErrorCode.TooManyRequests)
                    {
                        //Retry logic
                        int delay = 1000;

                        while (details.ErrorCode == Microsoft.CognitiveServices.Speech.CancellationErrorCode.TooManyRequests && delay < 64000)
                        {
                            logger.LogWarning($"Azure TTS returned TooManyRequests. Waiting {delay}: {details.ErrorDetails}");

                            await Task.Delay(delay);
                            using Microsoft.CognitiveServices.Speech.SpeechSynthesisResult retryResult = await synthesizer.SpeakSsmlAsync(ssml);
                            details = Microsoft.CognitiveServices.Speech.SpeechSynthesisCancellationDetails.FromResult(retryResult);

                            if (details.ErrorCode == Microsoft.CognitiveServices.Speech.CancellationErrorCode.NoError)
                            {
                                //Success
                                using Microsoft.CognitiveServices.Speech.AudioDataStream retryStream = Microsoft.CognitiveServices.Speech.AudioDataStream.FromResult(retryResult);
                                await retryStream.SaveToWaveFileAsync(filepath);
                                return (filepath, ssml.Length);
                            }

                            if (details.ErrorCode != Microsoft.CognitiveServices.Speech.CancellationErrorCode.TooManyRequests)
                            {
                                //Some other error
                                throw new Exception($"Error caught when rendering Azure TTS: {details.ErrorDetails}");
                            }

                            delay *= 2;
                        }

                        //Timeout failure
                        throw new Exception($"Error caught when rendering Azure TTS. Unable to out-wait TooManyRequests: {details.ErrorDetails}");
                    }
                    else
                    {
                        //Failed
                        throw new Exception($"Error caught when rendering Azure TTS: {details.ErrorDetails}");
                    }
                }

                using Microsoft.CognitiveServices.Speech.AudioDataStream stream = Microsoft.CognitiveServices.Speech.AudioDataStream.FromResult(result);
                await stream.SaveToWaveFileAsync(filepath);
                return (filepath, ssml.Length);
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception caught when rendering Azure TTS", ex);
            }
        }
    }
}
