using System;
using System.Collections.Generic;
using System.Threading;

namespace TASagentTwitchBot.Core.View.Frames
{
    public abstract class ConsoleFrame : IDisposable
    {
        protected readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        protected readonly CountdownEvent readers = new CountdownEvent(1);

        public abstract string Title { get; }

        public abstract IEnumerable<string> Commands { get; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private bool active = true;
        private bool disposedValue;

        public bool Active
        {
            get => active;
            set
            {
                if (!active && value)
                {
                    OnActivate();
                }
                active = value;
            }
        }

        public abstract int MinWidth { get; }
        public abstract int PreferredWidth { get; }

        public abstract int MinHeight { get; }
        public abstract int PreferredHeight { get; }

        public ConsoleFrame()
        {
            X = 0;
            Y = 0;
            Width = 1;
            Height = 1;

            active = false;
        }

        public ConsoleFrame(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;

            active = true;
        }

        public void UpdateLocation(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;

            if (Active)
            {
                RecalculateLayout();
            }
        }


        protected virtual void OnActivate() => RecalculateLayout();
        protected abstract void RecalculateLayout();

        public void Redraw()
        {
            ClearPendingWrites();

            if (Active)
            {
                RedrawFrame();
            }
        }

        protected abstract void RedrawFrame();

        /// <summary>
        /// Specialized frame key-handling.  Returns whether the frame intercepted the key
        /// </summary>
        protected abstract bool _HandleKey(ConsoleKey key);

        public bool HandleKey(ConsoleKey key)
        {
            switch (key)
            {
                default:
                    return _HandleKey(key);
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cancellationTokenSource.Cancel();

                    readers.Signal();
                    readers.Wait();
                    readers.Dispose();

                    cancellationTokenSource.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private int pendingSlides = 0;
        private readonly List<PendingWrite> pendingWrites = new List<PendingWrite>();

        protected void QueueWrite(int y, string line, ConsoleColor bgColor, ConsoleColor fgColor)
        {
            pendingWrites.Add(new PendingWrite(y, line, bgColor, fgColor));
        }

        protected void QueueSlide(int slide)
        {
            foreach (PendingWrite write in pendingWrites)
            {
                write.NotifySlide(slide);
            }

            pendingSlides += slide;
        }

        public bool HasPendingWrites() => pendingSlides != 0 || pendingWrites.Count > 0;

        public void ExecutePendingWrites()
        {
            //Skip slides if there are none, or if they slide everything out of the readable region
            if (pendingSlides != 0 && Math.Abs(pendingSlides) < Height)
            {
                int sourceTop = pendingSlides > 0 ? Y + pendingSlides : Y;
                int targetTop = pendingSlides > 0 ? Y : Y + pendingSlides;

#pragma warning disable CA1416 // Validate platform compatibility
                Console.MoveBufferArea(X, sourceTop, Width, Height - Math.Abs(pendingSlides), X, targetTop);
#pragma warning restore CA1416 // Validate platform compatibility
            }

            pendingSlides = 0;

            if (pendingWrites.Count > 0)
            {
                PendingWrite pendingWrite;
                HashSet<int> writtenLines = new HashSet<int>(pendingWrites.Count);

                for (int i = pendingWrites.Count - 1; i >= 0; i--)
                {
                    pendingWrite = pendingWrites[i];
                    if (writtenLines.Add(pendingWrite.y))
                    {
                        if (pendingWrite.y >= Y && pendingWrite.y < Y + Height)
                        {
                            Console.SetCursorPosition(X, pendingWrite.y);
                            Console.ForegroundColor = pendingWrite.fgColor;
                            Console.BackgroundColor = pendingWrite.bgColor;
                            Console.Write(pendingWrite.line);
                        }
                    }
                }

                pendingWrites.Clear();
            }
        }

        protected void ClearPendingWrites()
        {
            pendingSlides = 0;
            pendingWrites.Clear();
        }


        private class PendingWrite
        {
            public int y;
            public readonly string line;
            public readonly ConsoleColor bgColor;
            public readonly ConsoleColor fgColor;

            public PendingWrite(int y, string line, ConsoleColor bgColor, ConsoleColor fgColor)
            {
                this.y = y;
                this.line = line;
                this.bgColor = bgColor;
                this.fgColor = fgColor;
            }

            public void NotifySlide(int slide)
            {
                //Positive Slide is Up
                y -= slide;
            }
        }

    }
}
