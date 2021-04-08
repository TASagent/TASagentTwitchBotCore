using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TASagentTwitchBot.Core.View.Frames
{
    public class ButtonFrame : ConsoleFrame
    {
        private const ConsoleColor colorBG = ConsoleColor.DarkGray;
        private const ConsoleColor colorText = ConsoleColor.Black;
        private const ConsoleColor colorBorder = ConsoleColor.Black;
        private const ConsoleColor highlightBG = ConsoleColor.Yellow;
        private const ConsoleColor highlightFG = ConsoleColor.Black;

        public override string Title => "Button";
        public override IEnumerable<string> Commands { get; } = new string[] { "(Enter) Accept" };

        private bool highlighted = false;
        public bool Highlighted
        {
            get => highlighted;
            set
            {
                if (highlighted != value)
                {
                    highlighted = value;
                    RedrawFrame();
                }
            }
        }

        public override int MinWidth => minWidth;
        public override int PreferredWidth => minWidth;

        public override int MinHeight => minHeight;
        public override int PreferredHeight => minHeight;

        private int minWidth = 4;
        private int minHeight = 3;

        private string[] buttonText = null;
        private Action callback = null;

        public ButtonFrame()
            : base()
        {
        }

        /// <summary>
        /// Sets the text and callback, returns the minimum size
        /// </summary>
        public (int minWidth, int minHeight) PrepareButton(string buttonText, Action callback)
        {
            Active = false;
            Highlighted = false;

            this.buttonText = buttonText.Split('\n');
            this.callback = callback;

            minWidth = 4 + this.buttonText.Max(x => x.Length);
            minHeight = 2 + this.buttonText.Length;

            return (minWidth, minHeight);
        }

        public void FinalizePreparation(int x, int y, int w, int h)
        {
            //Set Size
            UpdateLocation(x, y, w, h);
            Active = true;

            //Still requires a Redraw
        }

        protected override bool _HandleKey(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.Enter:
                    callback?.Invoke();
                    return true;

                default:
                    break;
            }
            return false;
        }

        protected override void RecalculateLayout()
        {
            Debug.Assert(buttonText != null);
            Debug.Assert(Width >= minWidth);
            Debug.Assert(Height >= minHeight);
        }

        protected override void RedrawFrame()
        {
            //Top Of Button
            Console.SetCursorPosition(X, Y);

            Console.BackgroundColor = Highlighted ? highlightBG : colorBG;
            Console.ForegroundColor = Highlighted ? highlightFG : colorBorder;

            Console.Write($"┌{new string('─', Width - 2)}╖");

            int textSpaceU = (Height - 2 - buttonText.Length) / 2;
            int textSpaceD = (Height - 1 - buttonText.Length) / 2;

            //Above text spaces
            for (int line = 0; line < textSpaceU; line++)
            {
                Console.SetCursorPosition(X, Y + line + 1);
                Console.Write($"│{new string(' ', Width - 2)}║");
            }

            //Button
            for (int line = 0; line < buttonText.Length; line++)
            {
                Console.SetCursorPosition(X, Y + textSpaceU + line + 1);

                int textSpaceL = (Width - 2 - buttonText[line].Length) / 2;
                int textSpaceR = (Width - 1 - buttonText[line].Length) / 2;
                Console.Write($"│{new string(' ', textSpaceL)}");

                Console.BackgroundColor = Highlighted ? highlightBG : colorBG;
                Console.ForegroundColor = Highlighted ? highlightFG : colorText;
                Console.Write($"{buttonText[line]}");


                Console.BackgroundColor = Highlighted ? highlightBG : colorBG;
                Console.ForegroundColor = Highlighted ? highlightFG : colorBorder;
                Console.Write($"{new string(' ', textSpaceR)}║");
            }

            //Below text spaces
            for (int line = 0; line < textSpaceD; line++)
            {
                Console.SetCursorPosition(X, Y + Height - 2 - line);
                Console.Write($"│{new string(' ', Width - 2)}║");
            }

            //Bottom of Button
            Console.SetCursorPosition(X, Y + Height - 1);
            Console.Write($"╘{new string('═', Width - 2)}╝");
        }
    }
}
