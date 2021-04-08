using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.View.Frames
{
    public class EventsFrame : ConsoleFrame
    {
        private const ConsoleColor colorFG = ConsoleColor.White;
        private const ConsoleColor colorBG = ConsoleColor.Black;

        private int eventOffset = 0;

        private int maxEventScroll;

        public override int MinWidth => 30;
        public override int PreferredWidth => 12;

        public override int MinHeight => 4;
        public override int PreferredHeight => 100;

        public override string Title => "Events";
        public override IEnumerable<string> Commands { get; } = new string[] { "(Navigate) Shift Events" };

        private readonly List<string> rawEvents = new List<string>();

        private readonly ChannelReader<string> eventReader;
        private readonly ChannelWriter<string> eventWriter;


        public EventsFrame()
            : base()
        {
            Channel<string> channel = Channel.CreateUnbounded<string>();
            eventReader = channel.Reader;
            eventWriter = channel.Writer;

            ListenForUpdates();
        }

        private async void ListenForUpdates()
        {
            try
            {
                readers.AddCount();

                while (true)
                {
                    string newEvent = await eventReader.ReadAsync(cancellationTokenSource.Token);

                    rawEvents.Add(newEvent);

                    if (eventOffset != 0)
                    {
                        eventOffset++;
                    }

                    Redraw();
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
                    if (eventOffset > 0)
                    {
                        LineScrollUp();
                    }
                    return true;

                case ConsoleKey.DownArrow:
                    if (eventOffset < maxEventScroll)
                    {
                        LineScrollDown();
                    }
                    return true;

                case ConsoleKey.PageUp:
                    if (eventOffset > 0)
                    {
                        eventOffset = Math.Max(0, eventOffset - Height);
                        RedrawFrame();
                    }
                    return true;

                case ConsoleKey.PageDown:
                    if (eventOffset < maxEventScroll)
                    {
                        eventOffset = Math.Min(maxEventScroll, eventOffset + Height);
                        RedrawFrame();
                    }
                    return true;

                case ConsoleKey.Home:
                    if (eventOffset > 0)
                    {
                        eventOffset = 0;
                        RedrawFrame();
                    }
                    return true;

                case ConsoleKey.End:
                    if (eventOffset < maxEventScroll)
                    {
                        eventOffset = maxEventScroll;
                        RedrawFrame();
                    }
                    return true;
            }

            return false;
        }

        public void AddEvent(string line)
        {
            eventWriter.TryWrite(line);
        }

        protected override void RecalculateLayout()
        {
            maxEventScroll = Math.Max(0, rawEvents.Count - Height);
            eventOffset = Math.Min(maxEventScroll, eventOffset);
        }

        private void LineScrollDown()
        {
            eventOffset++;

            //Move old lines up
            QueueSlide(1);
            //Render new line
            QueueWrite(Y + Height - 1, GetLine(Height - 1), colorBG, colorFG);
        }

        private void LineScrollUp()
        {
            eventOffset--;

            //Move old lines up
            QueueSlide(-1);
            //Render new line
            QueueWrite(Y, GetLine(0), colorBG, colorFG);
        }

        protected override void RedrawFrame()
        {
            maxEventScroll = Math.Max(0, rawEvents.Count - Height);
            eventOffset = Math.Min(maxEventScroll, eventOffset);

            for (int line = 0; line < Height; line++)
            {
                QueueWrite(Y + line, GetLine(line), colorBG, colorFG);
            }
        }

        private string GetLine(int line)
        {
            if (line + eventOffset >= rawEvents.Count)
            {
                //Blank lines below bottom of stack
                return new string(' ', Width);
            }
            else
            {
                return GetPrintedEvent(rawEvents[line + eventOffset]);
            }
        }

        private string GetPrintedEvent(string eventText)
        {
            if (eventText.Length >= Width)
            {
                return eventText.Substring(0, Width);
            }
            else
            {
                return eventText.PadRight(Width);
            }
        }
    }
}
