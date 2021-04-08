using System;
using System.Text;

namespace TASagentTwitchBot.Core.View.Layout
{
    public class StandardConsoleLayout : ConsoleLayout
    {
        int consoleWidth = 0;
        int consoleHeight = 0;

        int interiorHeight;

        int mainWidth;
        int secondaryWidth;
        int secondaryAHeight;

        private readonly Frames.ConsoleFrame mainFrame;
        private readonly Frames.ConsoleFrame secondaryFrameA;
        private readonly Frames.ConsoleFrame secondaryFrameB;

        //Border-drawing
        private string topLine;
        private string bottomLine;
        private string normalLine;
        private string rightTransitionLine;
        public override Frames.ConsoleFrame DefaultSelection => mainFrame;

        public StandardConsoleLayout(
            Frames.ConsoleFrame mainFrame,
            Frames.ConsoleFrame secondaryFrameA,
            Frames.ConsoleFrame secondaryFrameB)
        {
            this.mainFrame = mainFrame;
            this.secondaryFrameA = secondaryFrameA;
            this.secondaryFrameB = secondaryFrameB;
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

            secondaryWidth = Math.Max(secondaryFrameA.MinWidth, secondaryFrameB.MinWidth);

            int secondaryResidualHeight = interiorHeight - 3 - secondaryFrameA.MinHeight - secondaryFrameB.MinHeight;

            secondaryAHeight = secondaryFrameA.MinHeight + secondaryResidualHeight / 2;

            //Regs width is basically constant (subtracting room for Borders and spacing)
            mainWidth = consoleWidth - 7 - secondaryWidth;

            BuildBorderStrings();
            //DrawBorders();

            mainFrame.UpdateLocation(2, 2, mainWidth, interiorHeight);
            secondaryFrameA.UpdateLocation(mainWidth + 5, 2, secondaryWidth, secondaryAHeight);
            secondaryFrameB.UpdateLocation(mainWidth + 5, secondaryAHeight + 5, secondaryWidth, interiorHeight - secondaryAHeight - 3);

            mainFrame.Active = true;
            secondaryFrameA.Active = true;
            secondaryFrameB.Active = true;
        }

        public override void FocusLost()
        {
            mainFrame.Active = false;
            secondaryFrameA.Active = false;
            secondaryFrameB.Active = false;
        }

        public override void DrawBorders()
        {
            Console.BackgroundColor = backgroundColor;
            Console.ForegroundColor = borderColor;
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(topLine);
            for (int i = 0; i < interiorHeight + 2; i++)
            {
                if (i == secondaryAHeight + 2)
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
            builder.Append('╗');

            topLine = builder.ToString();
            builder.Clear();

            //Build bottom line
            builder.Append('╚');
            builder.Append('═', mainWidth + 2);
            builder.Append('╩');
            builder.Append('═', secondaryWidth + 2);
            builder.Append('╝');

            bottomLine = builder.ToString();
            builder.Clear();

            //Build normal line
            builder.Append('║');
            builder.Append(' ', mainWidth + 2);
            builder.Append('║');
            builder.Append(' ', secondaryWidth + 2);
            builder.Append('║');

            normalLine = builder.ToString();
            builder.Clear();

            //Build right-transition line
            builder.Append('║');
            builder.Append(' ', mainWidth + 2);
            builder.Append('╠');
            builder.Append('═', secondaryWidth + 2);
            builder.Append('╣');

            rightTransitionLine = builder.ToString();
            builder.Clear();
        }
    }
}
