using System.Collections.Generic;
using System.Threading.Tasks;
using TASagentTwitchBot.Core.TTS.Parsing.RenderElements;

namespace TASagentTwitchBot.Core.TTS.Parsing
{
    public abstract class TTSSystemRenderer
    {
        public abstract Task<Audio.AudioRequest> Render(IEnumerable<RenderElement> renderElements);
        public abstract Task<(string filename, int ssmlLength)> RenderRaw(IEnumerable<RenderElement> renderElements);

        protected static bool HasExtraMode(TTSRenderMode oldMode, TTSRenderMode newMode) => (oldMode & ~newMode) > 0;

        protected static bool MissingMode(TTSRenderMode oldMode, TTSRenderMode newMode) => (newMode & ~oldMode) > 0;

        protected static bool RequiresMode(TTSRenderMode oldMode, TTSRenderMode newMode, TTSRenderMode mode) => 
            ((oldMode & mode) == 0) && ((newMode & mode) == mode);

    }
}
