using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

using Google.Cloud.TextToSpeech.V1;
using Amazon.Polly;

using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.TTS.Parsing;

namespace TASagentTwitchBot.Core.TTS
{
    public class NewTTSRenderer : ITTSRenderer
    {
        protected readonly TextToSpeechClient googleClient;
        protected readonly AmazonPollyClient amazonClient;

        protected readonly ICommunication communication;
        protected readonly ISoundEffectSystem soundEffectSystem;

        public NewTTSRenderer(
            ICommunication communication,
            ISoundEffectSystem soundEffectSystem)
        {
            this.communication = communication;
            this.soundEffectSystem = soundEffectSystem;

            //
            // Prepare Google TTS
            // 
            TextToSpeechClientBuilder builder = new TextToSpeechClientBuilder();

            string googleCredentialsPath = BGC.IO.DataManagement.PathForDataFile("Config", "googleCloudCredentials.json");

            if (!File.Exists(googleCredentialsPath))
            {
                throw new FileNotFoundException($"Could not find credentials for Google TTS at {googleCredentialsPath}");
            }

            builder.CredentialsPath = googleCredentialsPath;
            googleClient = builder.Build();

            //
            // Prepare Amazon TTS
            // 
            string awsCredentialsPath = BGC.IO.DataManagement.PathForDataFile("Config", "awsPollyCredentials.json");

            if (!File.Exists(awsCredentialsPath))
            {
                throw new FileNotFoundException($"Could not find credentials for AWS Polly at {awsCredentialsPath}");
            }

            AWSPollyCredentials awsPolyCredentials = JsonSerializer.Deserialize<AWSPollyCredentials>(File.ReadAllText(awsCredentialsPath));

            Amazon.Runtime.BasicAWSCredentials awsCredentials = new Amazon.Runtime.BasicAWSCredentials(
                awsPolyCredentials.AccessKey,
                awsPolyCredentials.SecretKey);

            amazonClient = new AmazonPollyClient(awsCredentials, Amazon.RegionEndpoint.USWest1);
        }

        public async Task<AudioRequest> TTSRequest(
            TTSVoice voice,
            TTSPitch pitch,
            TTSSpeed speed,
            Effect effectsChain,
            string ttsText)
        {
            TTSSystemRenderer ttsSystemRenderer;

            switch (voice.GetTTSService())
            {
                case TTSService.Amazon:
                    ttsSystemRenderer = new AmazonTTSRenderer(amazonClient, communication, voice, pitch, speed, effectsChain);
                    break;

                case TTSService.Google:
                    ttsSystemRenderer = new GoogleTTSRenderer(googleClient, communication, voice, pitch, speed, effectsChain);
                    break;

                default:
                    communication.SendErrorMessage($"Unsupported TTSVoice for TTSService {voice}");
                    goto case TTSService.Google;
            }

            return await TTSParser.ParseTTS(ttsText, ttsSystemRenderer, soundEffectSystem);
        }
    }
}
