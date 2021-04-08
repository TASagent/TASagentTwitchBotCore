using System;
using System.Collections.Generic;
using System.Linq;

namespace TASagentTwitchBot.Core.View.Frames
{

    /*    Box Drawing Examples
     *       Thanks Wikipedia!
     * 
     *    ┌─┬┐  ╔═╦╗  ╓─╥╖  ╒═╤╕
     *    │ ││  ║ ║║  ║ ║║  │ ││
     *    ├─┼┤  ╠═╬╣  ╟─╫╢  ╞═╪╡
     *    └─┴┘  ╚═╩╝  ╙─╨╜  ╘═╧╛
     *    ┌───────────────────┐
     *    │  ╔═══╗ Some Text  │▒
     *    │  ╚═╦═╝ in the box │▒
     *    ╞═╤══╩══╤═══════════╡▒
     *    │ ├──┬──┤           │▒
     *    │ └──┴──┘           │▒
     *    └───────────────────┘▒
     *     ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
     *     
     *                    
     *     Popup Design:
     *     
     *     ┌────────────────────┐
     *     │       Title        │▒
     *     ├────────────────────┤▒
     *     │      The text      │▒
     *     │      goes here     │▒
     *     │                    │▒
     *     │   ┌────╖  ┌────╖   │▒
     *     │   │ OK ║  │ OK ║   │▒
     *     │   ╘════╝  ╘════╝   │▒
     *     └────────────────────┘▒
     *      ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒
     *     
     *     Button:
     *     
     *     ┌────╖
     *     │ OK ║
     *     ╘════╝
     *     
     */


    public class PopupResponseFrame : ConsoleFrame
    {
        private const ConsoleColor shadowBG = ConsoleColor.Black;
        private const ConsoleColor shadowFG = ConsoleColor.DarkGray;

        private const ConsoleColor colorBG = ConsoleColor.DarkGray;
        private const ConsoleColor colorFG = ConsoleColor.Black;
        private const ConsoleColor colorBorder = ConsoleColor.Black;

        private const int MIN_WIDTH = 30;

        public override string Title => "PopUpResponse";
        public override IEnumerable<string> Commands { get; } = new string[] { "(Enter) Accept", "(Tab) Switch" };

        public override int MinWidth => throw new InvalidOperationException();
        public override int PreferredWidth => throw new InvalidOperationException();

        public override int MinHeight => throw new InvalidOperationException();
        public override int PreferredHeight => throw new InvalidOperationException();

        private string title = "";
        private string[] message = null;
        private Action callbackA = null;
        private Action callbackB = null;

        private readonly ButtonFrame buttonA;
        private readonly ButtonFrame buttonB;

        private ButtonFrame activeButton;

        public PopupResponseFrame()
            : base()
        {
            buttonA = new ButtonFrame();
            buttonB = new ButtonFrame();

            activeButton = buttonA;
        }

        public void ShowPopup(string title, string message, string buttonAText, string buttonBText, Action callbackA, Action callbackB)
        {
            Active = true;

            this.title = title;
            this.message = message.Split('\n');
            this.callbackA = callbackA;
            this.callbackB = callbackB;

            (int minButtonAW, int minButtonAH) = buttonA.PrepareButton(buttonAText, ButtonAClicked);
            (int minButtonBW, int minButtonBH) = buttonB.PrepareButton(buttonBText, ButtonBClicked);

            int minButtonW = Math.Max(minButtonAW, minButtonBW);
            int minButtonH = Math.Max(minButtonAH, minButtonBH);

            //Calculate Size
            int width = Math.Max(
                Math.Max(
                    4 + title.Length,
                    5 + 2 * minButtonW),
                4 + this.message.Max(x => x.Length));
            width = Math.Max(width, MIN_WIDTH);

            int height = 5 + minButtonH + this.message.Length;

            if (width > Console.WindowWidth - 1)
            {
                throw new NotSupportedException($"Requested popup window size is too large: {width}");
            }

            //Calculate coordinates
            int x = (Console.WindowWidth - 1 - width) / 2;
            int y = (Console.WindowHeight - 1 - height) / 2;

            int buttonHorizontalExcess = width - 2 - 2 * minButtonW;

            int firstButtonSpacing = buttonHorizontalExcess / 3;
            int secondButtonSpacing = firstButtonSpacing;

            if (buttonHorizontalExcess % 3 == 1)
            {
                secondButtonSpacing++;
            }
            else if (buttonHorizontalExcess % 3 == 2)
            {
                firstButtonSpacing++;
            }

            //Calculate button coordinates
            int buttonAX = x + 1 + firstButtonSpacing;
            int buttonBX = buttonAX + minButtonW + secondButtonSpacing;
            int buttonY = y + height - 1 - minButtonH;

            buttonA.FinalizePreparation(buttonAX, buttonY, minButtonW, minButtonH);
            buttonA.Highlighted = true;
            activeButton = buttonA;

            buttonB.FinalizePreparation(buttonBX, buttonY, minButtonW, minButtonH);
            buttonB.Highlighted = false;

            //Set Size
            UpdateLocation(x, y, width, height);

            RedrawFrame();
        }

        protected override bool _HandleKey(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.Tab:
                    activeButton.Highlighted = false;
                    if (activeButton == buttonA)
                    {
                        activeButton = buttonB;
                    }
                    else
                    {
                        activeButton = buttonA;
                    }
                    activeButton.Highlighted = true;
                    buttonA.Redraw();
                    buttonB.Redraw();
                    return true;

                default:
                    return activeButton.HandleKey(key);
            }
        }

        protected override void RecalculateLayout()
        {

        }

        private void ButtonAClicked()
        {
            Active = false;
            //TriggerRedraw
            callbackA?.Invoke();
        }
        private void ButtonBClicked()
        {
            Active = false;
            //TriggerRedraw
            callbackB?.Invoke();
        }

        protected override void RedrawFrame()
        {
            //Full Overdraw

            //
            //Draw Shadow
            //
            Console.BackgroundColor = shadowBG;
            Console.ForegroundColor = shadowFG;

            for (int y = Y + 1; y < Y + Height; y++)
            {
                Console.SetCursorPosition(X + Width, y);
                Console.Write('▒');
            }

            Console.SetCursorPosition(X + 1, Y + Height);
            Console.Write(new string('▒', Width));

            //
            //Draw Border
            //
            Console.BackgroundColor = colorBG;
            Console.ForegroundColor = colorBorder;

            //Top Border
            Console.SetCursorPosition(X, Y);
            Console.Write($"┌{new string('─', Width - 2)}┐");

            //Top Title
            int titleAreaSpaces = Width - 2 - title.Length;
            Console.SetCursorPosition(X, Y + 1);
            Console.Write($"│{new string(' ', titleAreaSpaces / 2)}");
            Console.ForegroundColor = colorFG;
            Console.Write(title);
            Console.ForegroundColor = colorBorder;
            Console.Write($"{new string(' ', (titleAreaSpaces + 1) / 2)}│");

            //Bottom Title Bar
            Console.SetCursorPosition(X, Y + 2);
            Console.Write($"├{new string('─', Width - 2)}┤");

            //Body
            for (int line = 0; line < message.Length; line++)
            {
                Console.SetCursorPosition(X, Y + 3 + line);

                int lineAreaSpaces = Width - 2 - message[line].Length;
                Console.Write($"│{new string(' ', lineAreaSpaces /2)}");
                Console.ForegroundColor = colorFG;
                Console.Write(message[line]);
                Console.ForegroundColor = colorBorder;
                Console.Write($"{new string(' ', (lineAreaSpaces + 1) /2)}│");
            }
            
            //Draw frame behind button
            for (int y = Y + message.Length + 3; y < Y + Height - 1; y++)
            {
                Console.SetCursorPosition(X, y);
                Console.Write($"│{new string(' ', Width - 2)}│");
            }

            //Bottom Border
            Console.SetCursorPosition(X, Y + Height - 1);
            Console.Write($"└{new string('─', Width - 2)}┘");

            //Draw Buttons
            buttonA.Redraw();
            buttonB.Redraw();
        }
    }
}
