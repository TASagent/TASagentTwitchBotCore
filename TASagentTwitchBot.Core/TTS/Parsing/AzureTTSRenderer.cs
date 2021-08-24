using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;

using TASagentTwitchBot.Core.Audio.Effects;

namespace TASagentTwitchBot.Core.TTS.Parsing
{
    public class AzureTTSRenderer : StandardTTSSystemRenderer
    {
        private readonly SpeechConfig azureClient;

        public AzureTTSRenderer(
            SpeechConfig azureClient,
            ICommunication communication,
            TTSVoice voice,
            TTSPitch pitch,
            TTSSpeed speed,
            Effect effectsChain)
            : base(
                  communication: communication,
                  voice: (voice == TTSVoice.Unassigned) ? TTSVoice.en_US_BenjaminRUS : voice,
                  pitch: pitch,
                  speed: speed,
                  effectsChain: effectsChain)
        {
            this.azureClient = azureClient;
        }

        protected override string GetModeMarkup(TTSRenderMode mode, bool start)
        {
            switch (mode)
            {
                case TTSRenderMode.Whisper:
                    return start ? "<emphasis level=\"reduced\">" : "</emphasis>";

                case TTSRenderMode.Emphasis:
                    return start ? "<emphasis level=\"strong\">" : "</emphasis>";

                case TTSRenderMode.Censor:
                    return start ? "<say-as interpret-as=\"expletive\">" : "</say-as>";

                case TTSRenderMode.Normal:
                default:
                    throw new Exception($"Unsupported RenderMode for Markup: {mode}");
            }
        }

        protected override async Task<string> SynthesizeSpeech(string interiorSSML)
        {
            try
            {
                interiorSSML = WrapSSML(interiorSSML);

                // Write the binary AudioContent of the response to file.
                string filepath = Path.Combine(TTSFilesPath, $"{Guid.NewGuid()}.mp3");

                using SpeechSynthesizer synthesizer = new SpeechSynthesizer(azureClient, null);
                using SpeechSynthesisResult result = await synthesizer.SpeakSsmlAsync(interiorSSML);

                if (result.Reason == ResultReason.Canceled)
                {
                    SpeechSynthesisCancellationDetails details = SpeechSynthesisCancellationDetails.FromResult(result);

                    if (details.ErrorCode == CancellationErrorCode.TooManyRequests)
                    {
                        //Retry logic
                        int delay = 1000;

                        while (details.ErrorCode == CancellationErrorCode.TooManyRequests && delay < 64000)
                        {
                            communication.SendWarningMessage($"Azure TTS returned TooManyRequests. Waiting {delay}: {details.ErrorDetails}");
                            await Task.Delay(delay);
                            using SpeechSynthesisResult retryResult = await synthesizer.SpeakSsmlAsync(interiorSSML);
                            details = SpeechSynthesisCancellationDetails.FromResult(retryResult);

                            if (details.ErrorCode == CancellationErrorCode.NoError)
                            {
                                //Success
                                using AudioDataStream retryStream = AudioDataStream.FromResult(retryResult);
                                await retryStream.SaveToWaveFileAsync(filepath);
                                return filepath;
                            }

                            if (details.ErrorCode != CancellationErrorCode.TooManyRequests)
                            {
                                //Some other error
                                communication.SendErrorMessage($"Error caught when rendering Azure TTS: {details.ErrorDetails}");
                                return null;
                            }

                            delay *= 2;
                        }

                        //Timeout failure
                        communication.SendErrorMessage($"Unable to out-wait TooManyRequests: {details.ErrorDetails}");
                        return null;
                    }
                    else
                    {
                        //Failed
                        communication.SendErrorMessage($"Error caught when rendering Azure TTS: {details.ErrorDetails}");
                        return null;
                    }
                }

                using AudioDataStream stream = AudioDataStream.FromResult(result);
                await stream.SaveToWaveFileAsync(filepath);
                return filepath;
            }
            catch (Exception e)
            {
                communication.SendErrorMessage($"Exception caught when rendering Azure TTS {e}");
                return null;
            }
        }

        private string WrapSSML(string interiorSSML)
        {
            if (pitch != TTSPitch.Medium || speed != TTSSpeed.Medium)
            {
                interiorSSML = $"<prosody pitch=\"{pitch.GetPitchShift()}\" rate=\"{speed.GetSpeedValue()}\">{interiorSSML}</prosody>";
            }

            return $"<speak version=\"1.0\" xml:lang=\"en-US\" xmlns:mstts=\"http://www.w3.org/2001/mstts\">" +
                $"<voice name=\"{voice.GetTTSVoiceString()}\">" +
                $"<mstts:silence type=\"Sentenceboundary\" value=\"250ms\"/>" +
                $"<mstts:silence type=\"Tailing\" value=\"0ms\"/>" +
                $"{interiorSSML}" +
                $"</voice></speak>";
        }

        protected override string PrepareText(string text) => SanitizeXML(text);
    }
}
