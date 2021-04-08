using System;
using System.Text;

namespace TASagentTwitchBot.Core.View.Layout
{
    public class FocusedConsoleLayout : ConsoleLayout
    {
        int consoleWidth = 0;
        int consoleHeight = 0;

        int interiorHeight;

        int mainWidth;
        int secondaryWidth;
        int tertiaryWidth;

        private readonly bool expandSecondary;

        private readonly Frames.ConsoleFrame mainFrame;
        private readonly Frames.ConsoleFrame secondaryFrame;
        private readonly Frames.ConsoleFrame tertiaryFrame;

        //Border-drawing
        private string topLine;
        private string bottomLine;
        private string normalLine;

        public override Frames.ConsoleFrame DefaultSelection => mainFrame;

        public FocusedConsoleLayout(
            Frames.ConsoleFrame mainFrame,
            Frames.ConsoleFrame secondaryFrame,
            Frames.ConsoleFrame tertiaryFrame,
            bool expandSecondary = false)
        {
            this.mainFrame = mainFrame;
            this.secondaryFrame = secondaryFrame;
            this.tertiaryFrame = tertiaryFrame;
            this.expandSecondary = expandSecondary;
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

            tertiaryWidth = tertiaryFrame.MinWidth;

            //Regs width is basically constant (subtracting room for Borders and spacing)
            int remainingWidth = consoleWidth - 10 - tertiaryWidth;

            //Two borders, two interior spaces
            interiorHeight = consoleHeight - 5;

            //Width of SecondaryFrame

            if (expandSecondary)
            {
                secondaryWidth = remainingWidth / 2;
            }
            else
            {
                secondaryWidth = Math.Max(
                    Math.Min(remainingWidth / 2, secondaryFrame.PreferredWidth),
                    secondaryFrame.MinWidth);
            }

            mainWidth = remainingWidth - secondaryWidth;

            BuildBorderStrings();

            mainFrame.UpdateLocation(2, 2, mainWidth, interiorHeight);
            secondaryFrame.UpdateLocation(mainWidth + 5, 2, secondaryWidth, interiorHeight);
            tertiaryFrame.UpdateLocation(consoleWidth - tertiaryWidth - 2, 2, tertiaryWidth, interiorHeight);

            mainFrame.Active = true;
            secondaryFrame.Active = true;
            tertiaryFrame.Active = true;
        }

        public override void FocusLost()
        {
            mainFrame.Active = false;
            secondaryFrame.Active = false;
            tertiaryFrame.Active = false;
        }

        public override void DrawBorders()
        {
            Console.BackgroundColor = backgroundColor;
            Console.ForegroundColor = borderColor;
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(topLine);
            for (int i = 0; i < interiorHeight + 2; i++)
            {
                Console.WriteLine(normalLine);
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
        }
    }
}
