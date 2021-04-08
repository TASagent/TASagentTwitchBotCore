using System;

namespace TASagentTwitchBot.Core.View.Layout
{
    public abstract class ConsoleLayout
    {
        protected const ConsoleColor borderColor = ConsoleColor.Yellow;
        protected const ConsoleColor backgroundColor = ConsoleColor.Black;

        public abstract Frames.ConsoleFrame DefaultSelection { get; }

        public abstract void DrawBorders();
        public abstract void FocusAcquired();
        public abstract void FocusLost();

        public abstract void SetKeypressCursor();
        public abstract void SetInputCursor();
        public abstract void SetCommandCursor();
    }
}
