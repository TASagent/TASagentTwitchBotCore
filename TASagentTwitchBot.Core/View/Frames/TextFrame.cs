using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.View.Frames
{
    public class TextFrame : ConsoleFrame
    {
        private const ConsoleColor colorBG = ConsoleColor.Black;

        private readonly ConsoleColor[] consoleColors;

        private readonly List<int> rawLineColors = new List<int>();
        private readonly List<string> rawLines = new List<string>();

        private readonly List<int> rectifiedLineColors = new List<int>();
        private readonly List<string> rectifiedLines = new List<string>();

        private readonly ChannelReader<(string, int)> textReader;
        private readonly ChannelWriter<(string, int)> textWriter;

        //private bool hexMode = true;
        private int lineOffset = 0;
        private int maxLineScroll;

        public override string Title { get; }
        public override IEnumerable<string> Commands { get; } = new string[] { "(Navigate) Shift Text" };

        public override int MinWidth => 50;
        public override int PreferredWidth => 200;

        public override int MinHeight => 4;
        public override int PreferredHeight => 100;

        public TextFrame(string title, ConsoleColor[] consoleColors)
            : base()
        {
            Title = title;

            this.consoleColors = consoleColors;

            Channel<(string, int)> channel = Channel.CreateUnbounded<(string, int)>();
            textReader = channel.Reader;
            textWriter = channel.Writer;

            ListenForUpdates();
        }

        private async void ListenForUpdates()
        {
            try
            {
                readers.AddCount();

                while (true)
                {
                    (string newLine, int lineType) = await textReader.ReadAsync(cancellationTokenSource.Token);

                    HandleNewLine(newLine, lineType);
                }
            }
            catch (TaskCanceledException)
            {
                //Swallow
            }
            catch (ThreadAbortException)
            {
                //Swallow
            }
            finally
            {
                readers.Signal();
            }
        }

        protected override bool _HandleKey(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.UpArrow:
                    if (lineOffset < maxLineScroll)
                    {
                        LineScrollUp();
                    }
                    return true;

                case ConsoleKey.DownArrow:
                    if (lineOffset > 0)
                    {
                        LineScrollDown();
                    }
                    return true;

                case ConsoleKey.PageUp:
                    if (lineOffset < maxLineScroll)
                    {
                        lineOffset = Math.Min(maxLineScroll, lineOffset + Height);
                        RedrawFrame();
                    }
                    return true;

                case ConsoleKey.PageDown:
                    if (lineOffset > 0)
                    {
                        lineOffset = Math.Max(0, lineOffset - Height);
                        RedrawFrame();
                    }
                    return true;

                case ConsoleKey.Home:
                    if (lineOffset < maxLineScroll)
                    {
                        lineOffset = maxLineScroll;
                        RedrawFrame();
                    }
                    return true;

                case ConsoleKey.End:
                    if (lineOffset > 0)
                    {
                        lineOffset = 0;
                        RedrawFrame();
                    }
                    return true;
            }

            return false;
        }

        private void LineScrollUp()
        {
            lineOffset++;

            //Move old lines up
            QueueSlide(-1);
            //Render new line
            QueueWrite(Y, GetLine(0), colorBG, GetLineColor(0));
        }

        private void LineScrollDown()
        {
            lineOffset--;

            //Move old lines up
            QueueSlide(1);
            //Render new line
            QueueWrite(Y + Height - 1, GetLine(Height - 1), colorBG, GetLineColor(Height - 1));
        }

        protected override void RecalculateLayout()
        {
            //Recalculate Rectified Outputs
            rectifiedLines.Clear();
            rectifiedLineColors.Clear();

            //Just reset, it's simpler and better behaved.
            lineOffset = 0;

            for (int i = 0; i < rawLines.Count; i++)
            {
                string line = rawLines[i];
                int lineColor = rawLineColors[i];

                int linesAdded = 1;
                int inputIndex = rectifiedLines.Count;

                while (line.Length > Width)
                {
                    int lastSpace = line.Substring(0, Width).LastIndexOf(' ');
                    if (lastSpace == -1)
                    {
                        rectifiedLines.Add($"{line.Substring(0, Width - 1)}-");
                        linesAdded++;
                        line = line.Substring(Width - 1);
                    }
                    else
                    {
                        rectifiedLines.Add(line.Substring(0, lastSpace));
                        linesAdded++;
                        line = line.Substring(lastSpace + 1);
                    }
                }

                rectifiedLines.Add(line);

                for (int j = 0; j < linesAdded; j++)
                {
                    //Accumulate input line flags
                    rectifiedLineColors.Add(lineColor);
                }
            }

            maxLineScroll = Math.Max(0, rectifiedLines.Count - Height);
        }

        protected override void RedrawFrame()
        {
            for (int line = 0; line < Height; line++)
            {
                QueueWrite(Y + line, GetLine(line), colorBG, GetLineColor(line));
            }
        }

        private ConsoleColor GetLineColor(int line)
        {
            int index = rectifiedLines.Count - lineOffset - Height + line;
            if (index >= 0 && index < rectifiedLineColors.Count)
            {
                return consoleColors[rectifiedLineColors[index]];
            }
            else
            {
                return consoleColors[0];
            }
        }

        private string GetLine(int line)
        {
            int outputLine = rectifiedLines.Count - lineOffset - Height + line;
            if (outputLine >= 0 && outputLine < rectifiedLines.Count)
            {
                return rectifiedLines[outputLine].PadRight(Width);
            }
            else
            {
                return new string(' ', Width);
            }
        }


        public void AddLine(string input, int lineType)
        {
            if (input.Contains('\n'))
            {
                //Elimiate \r's
                input = input.Replace("\r", "");

                foreach (string line in input.Split('\n'))
                {
                    textWriter.TryWrite((line, lineType));
                }
            }
            else
            {
                textWriter.TryWrite((input, lineType));
            }
        }

        public void HandleNewLine(string input, int lineType)
        {
            rawLines.Add(input);
            rawLineColors.Add(lineType);

            if (!Active)
            {
                return;
            }

            int linesAdded = 1;

            while (input.Length > Width)
            {
                int lastSpace = input.Substring(0, Width).LastIndexOf(' ');
                if (lastSpace == -1)
                {
                    rectifiedLines.Add($"{input.Substring(0, Width - 1)}-");
                    linesAdded++;
                    input = input.Substring(Width - 1);
                }
                else
                {
                    rectifiedLines.Add(input.Substring(0, lastSpace));
                    linesAdded++;
                    input = input.Substring(lastSpace + 1);
                }
            }

            rectifiedLines.Add(input);

            for (int i = 0; i < linesAdded; i++)
            {
                //Accumulate input line flags
                rectifiedLineColors.Add(lineType);
            }

            if (lineOffset > 0)
            {
                //Track current line if they're offset from the bottom
                lineOffset += linesAdded;
            }
            else if (Active)
            {
                //Move old lines Up
                QueueSlide(linesAdded);

                for (int line = linesAdded; line > 0; line--)
                {
                    QueueWrite(Y + Height - line, GetLine(Height - line), colorBG, GetLineColor(Height - line));
                }
            }

            maxLineScroll = Math.Max(0, rectifiedLines.Count - Height);
        }
    }
}
