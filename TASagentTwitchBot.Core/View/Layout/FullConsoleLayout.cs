using System;
using System.Text;

namespace TASagentTwitchBot.Core.View.Layout
{
    public class FullConsoleLayout : ConsoleLayout
    {
        int consoleWidth = 0;
        int consoleHeight = 0;

        int interiorHeight;

        int mainWidth;
        int secondaryWidth;
        int tertiaryWidth;
        int tertiaryAHeight;

        private readonly Frames.ConsoleFrame mainFrame;
        private readonly Frames.ConsoleFrame secondaryFrame;
        private readonly Frames.ConsoleFrame tertiaryFrameA;
        private readonly Frames.ConsoleFrame tertiaryFrameB;

        //Border-drawing
        private string topLine;
        private string bottomLine;
        private string normalLine;
        private string rightTransitionLine;
        public override Frames.ConsoleFrame DefaultSelection => mainFrame;

        public FullConsoleLayout(
            Frames.ConsoleFrame mainFrame,
            Frames.ConsoleFrame secondaryFrame,
            Frames.ConsoleFrame tertiaryFrameA,
            Frames.ConsoleFrame tertiaryFrameB)
        {
            this.mainFrame = mainFrame;
            this.secondaryFrame = secondaryFrame;
            this.tertiaryFrameA = tertiaryFrameA;
            this.tertiaryFrameB = tertiaryFrameB;
        }

        public override void FocusAcquired()
        {
            consoleWidth = Console.WindowWidth - 1;
            consoleHeight = Console.WindowHeight - 1;

            if (Console.BufferWidth != Console.WindowWidth || Console.BufferHeight != Console.WindowHeight)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
#pragma warning restore CA1416 // Validate platform compatibility
            }

            //Two borders, two interior spaces
            interiorHeight = consoleHeight - 5;

            tertiaryWidth = Math.Max(tertiaryFrameA.MinWidth, tertiaryFrameB.MinWidth);

            int tertiaryResidualHeight = interiorHeight - 3 - tertiaryFrameA.MinHeight - tertiaryFrameB.MinHeight;

            tertiaryAHeight = tertiaryFrameA.MinHeight + tertiaryResidualHeight / 2;

            int remainingWidth = consoleWidth - 10 - tertiaryWidth;

            mainWidth = (remainingWidth + 1) / 2;
            secondaryWidth = remainingWidth - mainWidth;

            BuildBorderStrings();

            mainFrame.UpdateLocation(2, 2, mainWidth, interiorHeight);
            secondaryFrame.UpdateLocation(mainWidth + 5, 2, secondaryWidth, interiorHeight);
            tertiaryFrameA.UpdateLocation(mainWidth + secondaryWidth + 8, 2, tertiaryWidth, tertiaryAHeight);
            tertiaryFrameB.UpdateLocation(mainWidth + secondaryWidth + 8, tertiaryAHeight + 5, tertiaryWidth, interiorHeight - tertiaryAHeight - 3);

            mainFrame.Active = true;
            secondaryFrame.Active = true;
            tertiaryFrameA.Active = true;
            tertiaryFrameB.Active = true;
        }

        public override void FocusLost()
        {
            mainFrame.Active = false;
            secondaryFrame.Active = false;
            tertiaryFrameA.Active = false;
            tertiaryFrameB.Active = false;
        }

        public override void DrawBorders()
        {
            Console.BackgroundColor = backgroundColor;
            Console.ForegroundColor = borderColor;
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(topLine);
            for (int i = 0; i < interiorHeight + 2; i++)
            {
                if (i == tertiaryAHeight + 2)
                {
                    Console.WriteLine(rightTransitionLine);
                }
                else
                {
                    Console.WriteLine(normalLine);
                }
            }

            Console.WriteLine(bottomLine);
        }

        public override void SetKeypressCursor() => Console.SetCursorPosition(1, interiorHeight + 5);
        public override void SetInputCursor() => Console.SetCursorPosition(2, interiorHeight + 4);
        public override void SetCommandCursor() => Console.SetCursorPosition(4, interiorHeight + 5);

        private void BuildBorderStrings()
        {
            StringBuilder builder = new StringBuilder(consoleWidth);

            //Build top line
            builder.Append('╔');
            builder.Append('═', mainWidth + 2);
            builder.Append('╦');
            builder.Append('═', secondaryWidth + 2);
            builder.Append('╦');
            builder.Append('═', tertiaryWidth + 2);
            builder.Append('╗');

            topLine = builder.ToString();
            builder.Clear();

            //Build bottom line
            builder.Append('╚');
            builder.Append('═', mainWidth + 2);
            builder.Append('╩');
            builder.Append('═', secondaryWidth + 2);
            builder.Append('╩');
            builder.Append('═', tertiaryWidth + 2);
            builder.Append('╝');

            bottomLine = builder.ToString();
            builder.Clear();

            //Build normal line
            builder.Append('║');
            builder.Append(' ', mainWidth + 2);
            builder.Append('║');
            builder.Append(' ', secondaryWidth + 2);
            builder.Append('║');
            builder.Append(' ', tertiaryWidth + 2);
            builder.Append('║');

            normalLine = builder.ToString();
            builder.Clear();

            //Build right-transition line
            builder.Append('║');
            builder.Append(' ', mainWidth + 2);
            builder.Append('║');
            builder.Append(' ', secondaryWidth + 2);
            builder.Append('╠');
            builder.Append('═', tertiaryWidth + 2);
            builder.Append('╣');

            rightTransitionLine = builder.ToString();
            builder.Clear();
        }
    }
}
