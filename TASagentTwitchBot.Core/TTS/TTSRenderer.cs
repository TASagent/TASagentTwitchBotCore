using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

using Google.Cloud.TextToSpeech.V1;
using Amazon.Polly;
using Microsoft.CognitiveServices.Speech;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.TTS.Parsing;

namespace TASagentTwitchBot.Core.TTS
{
    public class TTSRenderer : ITTSRenderer
    {
        protected readonly TextToSpeechClient googleClient = null;
        protected readonly AmazonPollyClient amazonClient = null;
        protected readonly SpeechConfig azureClient = null;

        protected readonly ICommunication communication;
        protected readonly ISoundEffectSystem soundEffectSystem;

        protected readonly TTSConfiguration ttsConfig;

        public TTSRenderer(
            TTSConfiguration ttsConfig,
            ICommunication communication,
            ISoundEffectSystem soundEffectSystem)
        {
            this.ttsConfig = ttsConfig;
            this.communication = communication;
            this.soundEffectSystem = soundEffectSystem;

            //
            // Prepare Google TTS
            //
            if (ttsConfig.UseGoogleCloudTTS)
            {
                try
                {
                    TextToSpeechClientBuilder builder = new TextToSpeechClientBuilder();

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
                    communication.SendErrorMessage($"Exception thrown while trying to initialize Google TTS. Disabling: {e.Message}");
                    ttsConfig.UseGoogleCloudTTS = false;
                }
            }

            //
            // Prepare Amazon TTS
            //
            if (ttsConfig.UseAWSPolly)
            {
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

                    amazonClient = new AmazonPollyClient(awsCredentials, Amazon.RegionEndpoint.USWest2);
                }
                catch (Exception e)
                {
                    communication.SendErrorMessage($"Exception thrown while trying to initialize AWS Polly. Disabling: {e.Message}");
                    ttsConfig.UseAWSPolly = false;
                }
            }

            //
            // Prepare Azure TTS
            //
            if (ttsConfig.UseAzureSpeechSynthesis)
            {
                try
                {
                    string azureCredentialsPath = BGC.IO.DataManagement.PathForDataFile("Config", "azureSpeechSynthesisCredentials.json");

                    if (!File.Exists(azureCredentialsPath))
                    {
                        throw new FileNotFoundException($"Could not find credentials for Azure SpeechSynthesis at {azureCredentialsPath}");
                    }

                    AzureSpeechSynthesisCredentials azureCredentials = JsonSerializer.Deserialize<AzureSpeechSynthesisCredentials>(File.ReadAllText(azureCredentialsPath));

                    azureClient = SpeechConfig.FromSubscription(azureCredentials.AccessKey, azureCredentials.Region);
                    azureClient.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio24Khz48KBitRateMonoMp3);
                }
                catch (Exception e)
                {
                    communication.SendErrorMessage($"Exception thrown while trying to initialize Azure Speech Synthesis. Disabling: {e.Message}");
                    ttsConfig.UseAzureSpeechSynthesis = false;
                }
            }

            if (ttsConfig.Enabled && ttsConfig.GetASupportedService() == TTSService.MAX)
            {
                communication.SendErrorMessage($"No TTS service enabled properly. Disabling Service.");
                ttsConfig.Enabled = false;
            }
        }

        public async Task<AudioRequest> TTSRequest(
            Commands.AuthorizationLevel authorizationLevel,
            TTSVoice voice,
            TTSPitch pitch,
            TTSSpeed speed,
            Effect effectsChain,
            string ttsText)
        {
            if (!ttsConfig.Enabled)
            {
                communication.SendDebugMessage($"TTS currently disabled - Rejecting request.");
                return null;
            }

            TTSService service = voice.GetTTSService();

            if (!ttsConfig.IsServiceSupported(service))
            {
                communication.SendWarningMessage($"TTS Service {service} unsupported.");

                service = ttsConfig.GetASupportedService();
                voice = TTSVoice.Unassigned;
            }

            //Make sure Neural Voices are allowed
            if (voice.IsNeuralVoice() && !ttsConfig.CanUseNeuralVoice(authorizationLevel))
            {
                communication.SendWarningMessage($"Neural voice {voice} disallowed.  Changing voice to service default.");
                voice = TTSVoice.Unassigned;
            }

            TTSSystemRenderer ttsSystemRenderer;

            switch (service)
            {
                case TTSService.Amazon:
                    ttsSystemRenderer = new AmazonTTSLocalRenderer(amazonClient, communication, voice, pitch, speed, effectsChain);
                    break;

                case TTSService.Google:
                    ttsSystemRenderer = new GoogleTTSLocalRenderer(googleClient, communication, voice, pitch, speed, effectsChain);
                    break;

                case TTSService.Azure:
                    ttsSystemRenderer = new AzureTTSLocalRenderer(azureClient, communication, voice, pitch, speed, effectsChain);
                    break;

                default:
                    communication.SendErrorMessage($"Unsupported TTSVoice for TTSService {service}");
                    goto case TTSService.Google;
            }

            return await TTSParser.ParseTTS(ttsText, ttsSystemRenderer, soundEffectSystem);
        }
    }
}
