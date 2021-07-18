using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.TTS.Parsing.RenderElements;

namespace TASagentTwitchBot.Core.TTS.Parsing
{
    public abstract class StandardTTSSystemRenderer : TTSSystemRenderer
    {
        protected readonly TTSVoice voice;
        protected readonly TTSPitch pitch;
        protected readonly TTSSpeed speed;
        protected readonly Effect effectsChain;

        protected readonly ICommunication communication;
        protected static string TTSFilesPath => BGC.IO.DataManagement.PathForDataDirectory("TTSFiles");

        public StandardTTSSystemRenderer(
            ICommunication communication,
            TTSVoice voice,
            TTSPitch pitch,
            TTSSpeed speed,
            Effect effectsChain)
        {
            this.communication = communication;

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

            this.voice = voice;
            this.pitch = pitch;
            this.speed = speed;

            if (effectsChain is null)
            {
                effectsChain = new NoEffect();
            }

            this.effectsChain = effectsChain;
        }


        public override async Task<Audio.AudioRequest> Render(IEnumerable<RenderElement> renderElements)
        {
            List<Audio.AudioRequest> audioRequests = new List<Audio.AudioRequest>();

            TTSRenderMode renderMode = TTSRenderMode.Normal;
            Stack<TTSRenderMode> modifierStack = new Stack<TTSRenderMode>();

            StringBuilder ssmlBuilder = new StringBuilder();

            foreach(RenderElement renderElement in renderElements)
            {
                switch (renderElement)
                {
                    case SoundElement soundElement:
                        //Output text
                        if (ssmlBuilder.Length > 0)
                        {
                            ssmlBuilder.Append(UpdateRenderMode(ref renderMode, TTSRenderMode.Normal, modifierStack));

                            string interiorSSML = ssmlBuilder.ToString();
                            ssmlBuilder.Clear();

                            if (!string.IsNullOrWhiteSpace(interiorSSML))
                            {
                                Audio.AudioRequest ttsRequest = await GetAudio(interiorSSML);

                                if (ttsRequest is not null)
                                {
                                    audioRequests.Add(ttsRequest);
                                }
                                else
                                {
                                    //Add audio delay for consistency
                                    audioRequests.Add(new Audio.AudioDelay(200));
                                }
                            }
                        }
                        audioRequests.Add(soundElement.audioRequest);
                        break;

                    case TextElement textElement:
                        ssmlBuilder.Append(UpdateRenderMode(ref renderMode, textElement.renderMode, modifierStack));
                        ssmlBuilder.Append(PrepareText(textElement.text));
                        break;

                    default:
                        throw new NotImplementedException($"RenderElement Support not implemented: {renderElement.GetType()}");
                }
            }

            //Output remaining text
            if (ssmlBuilder.Length > 0)
            {
                ssmlBuilder.Append(UpdateRenderMode(ref renderMode, TTSRenderMode.Normal, modifierStack));

                string interiorSSML = ssmlBuilder.ToString();
                ssmlBuilder.Clear();

                if (!string.IsNullOrWhiteSpace(interiorSSML))
                {
                    Audio.AudioRequest ttsRequest = await GetAudio(interiorSSML);

                    if (ttsRequest is not null)
                    {
                        audioRequests.Add(ttsRequest);
                    }
                    else
                    {
                        //Add audio delay for consistency
                        audioRequests.Add(new Audio.AudioDelay(200));
                    }
                }
            }

            if (audioRequests.Count == 0)
            {
                return new Audio.AudioDelay(200);
            }

            if (audioRequests.Count == 1)
            {
                return audioRequests[0];
            }

            return new Audio.ConcatenatedAudioRequest(audioRequests);
        }

        public async Task<Audio.AudioRequest> GetAudio(
            string interiorSSML,
            string filename = null)
        {
            string filePath = await SynthesizeSpeech(interiorSSML, filename);

            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            return new Audio.AudioFileRequest(filePath, effectsChain);
        }

        protected abstract Task<string> SynthesizeSpeech(string interiorSSML, string filename = null);

        protected string UpdateRenderMode(
            ref TTSRenderMode oldMode,
            TTSRenderMode newMode,
            Stack<TTSRenderMode> modifierStack)
        {
            if (oldMode == newMode)
            {
                return "";
            }

            StringBuilder builder = new StringBuilder();

            //You must always pop off Censor, since that must be the inner-most element.
            //It will be automatically re-added if needed
            while (HasExtraMode(oldMode, newMode) ||
                (modifierStack.Count > 0 && modifierStack.Peek() == TTSRenderMode.Censor))
            {
                TTSRenderMode removingMode = modifierStack.Pop();
                oldMode &= ~removingMode;
                builder.Append(GetModeMarkup(removingMode, false));
            }

            if (MissingMode(oldMode, newMode))
            {
                for (int modeValue = 1; modeValue < (int)TTSRenderMode.MASK; modeValue *= 2)
                {
                    TTSRenderMode mode = (TTSRenderMode)modeValue;
                    if (RequiresMode(oldMode, newMode, mode))
                    {
                        oldMode |= mode;
                        modifierStack.Push(mode);
                        builder.Append(GetModeMarkup(mode, true));
                    }
                }
            }

            if (oldMode != newMode)
            {
                communication.SendErrorMessage($"Mode switch failed. {oldMode} to {newMode}");
            }

            return builder.ToString();
        }

        protected abstract string PrepareText(string text);

        protected static string SanitizeXML(string text) =>
            text.Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("\'", "&apos;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

        protected abstract string GetModeMarkup(TTSRenderMode mode, bool start);
    }
}
