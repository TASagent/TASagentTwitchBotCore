using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TASagentTwitchBot.Core.Audio;

namespace TASagentTwitchBot.Core.TTS.Parsing.RenderElements
{
    public abstract class RenderElement
    {
    }


    public class TextElement : RenderElement
    {
        public readonly string text;
        public readonly TTSRenderMode renderMode;

        public TextElement(string text, TTSRenderMode renderMode)
        {
            this.text = text;
            this.renderMode = renderMode;
        }
    }

    public class SoundElement : RenderElement
    {
        public readonly AudioRequest audioRequest;

        public SoundElement(AudioRequest audioRequest)
        {
            this.audioRequest = audioRequest;
        }
    }

}
