using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.View
{
    public enum ConsoleFrameType
    {
        Output,
        Memory,
        Regs,
        Stack,
        MAX
    }

    public class ConsoleView : IConsoleOutput, IDisposable
    {
        private const ConsoleColor headerColor = ConsoleColor.White;

        private const ConsoleColor selectedHeaderBG = ConsoleColor.Cyan;
        private const ConsoleColor selectedHeaderFG = ConsoleColor.Black;

        private const ConsoleColor textColor = ConsoleColor.White;
        private const ConsoleColor backgroundColor = ConsoleColor.Black;

        private readonly ICommunication communication;
        private readonly ApplicationManagement applicationManagement;

        private readonly Frames.TextFrame chatFrame;
        private readonly Frames.TextFrame debugFrame;
        private readonly Frames.EventsFrame eventsFrame;
        private readonly Frames.PopupFrame popupFrame;
        private readonly Frames.PopupResponseFrame popupResponseFrame;

        private readonly Layout.ConsoleLayout focusedLayout;

        private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
        private readonly CountdownEvent readers = new CountdownEvent(1);

        private readonly Channel<ConsoleKeyInfo> consoleChannel;

        private Frames.ConsoleFrame selectedFrame;
        private Frames.ConsoleFrame overrideFrame = null;

        private Layout.ConsoleLayout selectedLayout = null;

        private bool borderDrawRequested = false;
        private bool frameTitleDrawRequested = false;
        private bool disposedValue = false;

        private void WriteErrorLine(string line) => DebugMessageHandler(line, MessageType.Error);

        public ConsoleView(
            ICommunication communication,
            ApplicationManagement applicationManagement)
        {
            Console.Title = "TASagent TwitchBot";

            this.communication = communication;
            this.applicationManagement = applicationManagement;

            consoleChannel = Channel.CreateUnbounded<ConsoleKeyInfo>();

            chatFrame = new Frames.TextFrame("Chat", new[] { ConsoleColor.White, ConsoleColor.DarkGreen });
            debugFrame = new Frames.TextFrame("Debug", new[] { ConsoleColor.White, ConsoleColor.Yellow, ConsoleColor.Red });

            eventsFrame = new Frames.EventsFrame();
            popupFrame = new Frames.PopupFrame();
            popupResponseFrame = new Frames.PopupResponseFrame();

            focusedLayout = new Layout.FocusedConsoleLayout(
                mainFrame: chatFrame,
                secondaryFrame: debugFrame,
                tertiaryFrame: eventsFrame,
                expandSecondary: true);

            //Initialize selectedFrame
            selectedFrame = chatFrame;
            overrideFrame = null;

            selectedLayout = focusedLayout;

            communication.ReceivePendingNotificationHandlers += ReceivePendingNotification;
            communication.ReceiveEventHandlers += ReceiveEventHandler;
            communication.ReceiveMessageLoggers += ReceiveMessageHandler;
            communication.SendMessageHandlers += SendPublicChatHandler;
            communication.SendWhisperHandlers += SendWhisperHandler;
            communication.DebugMessageHandlers += DebugMessageHandler;

            LaunchListeners();
        }

        private void ReceiveEventHandler(string message) => eventsFrame.AddEvent(message);

        private void ReceivePendingNotification(int id, string message)
        {
            debugFrame.AddLine($"Pending Notification {id}: {message}", 1);
        }

        private void DebugMessageHandler(string message, MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Debug:
                    debugFrame.AddLine(message, 0);
                    break;

                case MessageType.Warning:
                    debugFrame.AddLine(message, 1);
                    break;

                case MessageType.Error:
                    debugFrame.AddLine(message, 2);
                    break;

                default:
                    throw new NotSupportedException($"Unexpected messageType: {messageType}");
            }
        }

        private void SendPublicChatHandler(string message)
        {
            chatFrame.AddLine($" TASagentPuppet: {message}", 1);
        }

        private void SendWhisperHandler(string username, string message)
        {
            chatFrame.AddLine($" TASagentPuppet whispers {username}: {message}", 1);
        }

        private void ReceiveMessageHandler(IRC.TwitchChatter chatter)
        {
            chatFrame.AddLine($" {chatter.User.TwitchUserName}: {chatter.Message}", 0);
        }

        public void LaunchListeners()
        {
            ReadKeysHandler();
            HandleKeysLoop();

            selectedLayout.FocusAcquired();

            borderDrawRequested = true;
            DrawHandler();
            RedrawAllFrames();
        }

        private async Task<ConsoleKeyInfo> WaitForConsoleKeyInfo()
        {
            ConsoleKeyInfo keyInfo = default;
            try
            {
                await Task.Run(() => keyInfo = Console.ReadKey(true));
            }
            catch (Exception ex)
            {
                WriteErrorLine($"Exception: {ex}");
            }

            return keyInfo;
        }

        private async void ReadKeysHandler()
        {
            try
            {
                readers.AddCount();

                while (true)
                {
                    ConsoleKeyInfo nextKey = await WaitForConsoleKeyInfo().WithCancellation(generalTokenSource.Token);

                    //Bail if we're trying to quit
                    if (generalTokenSource.IsCancellationRequested)
                    {
                        break;
                    }

                    await consoleChannel.Writer.WriteAsync(nextKey);
                }
            }
            catch (TaskCanceledException)
            {
                //Swallow
            }
            catch (OperationCanceledException)
            {
                //Swallow
            }
            catch (Exception ex)
            {
                //Log Error
                WriteErrorLine($"Exception: {ex}");
            }
            finally
            {
                readers.Signal();
            }
        }

        private async void DrawHandler()
        {
            await Task.Yield();
            try
            {
                readers.AddCount();

                while (true)
                {
                    if (borderDrawRequested)
                    {
                        borderDrawRequested = false;
                        selectedLayout.DrawBorders();
                    }

                    foreach (Frames.ConsoleFrame frame in GetActiveFrames())
                    {
                        if (frame.HasPendingWrites())
                        {
                            frame.ExecutePendingWrites();
                        }
                    }

                    if (frameTitleDrawRequested)
                    {
                        frameTitleDrawRequested = false;
                        RedrawFrameTitles();
                    }

                    await Task.Delay(100, generalTokenSource.Token);
                }
            }
            catch (TaskCanceledException)
            {
                //Swallow
            }
            catch (OperationCanceledException)
            {
                //Swallow
            }
            catch (Exception ex)
            {
                //Log Error
                WriteErrorLine($"Exception: {ex}");
            }
            finally
            {
                readers.Signal();
            }
        }


        private async void HandleKeysLoop()
        {
            selectedLayout.FocusAcquired();

            while (true)
            {
                bool handled;

                Console.CursorVisible = false;

                ConsoleKeyInfo input = await consoleChannel.Reader.ReadAsync();

                handled = HandleStandardKey(input.Key);

                if (!handled)
                {
                    if (input.Key == ConsoleKey.Q && ((input.Modifiers & ConsoleModifiers.Control) != 0))
                    {
                        QuitProgram();
                        handled = true;
                    }
                }

                if (!handled)
                {
                    Console.Beep();
                }
            }
        }

        private bool HandleStandardKey(ConsoleKey key)
        {
            if (overrideFrame != null)
            {
                if (overrideFrame.HandleKey(key))
                {
                    return true;
                }
            }
            else
            {
                if (selectedFrame.HandleKey(key))
                {
                    return true;
                }
            }

            switch (key)
            {
                case ConsoleKey.Escape:
                    QuitProgram();
                    return true;

                case ConsoleKey.Tab:
                    CycleSelectedFrame();
                    return true;

                case ConsoleKey.F1:
                    TriggerTestPopup();
                    return true;

                case ConsoleKey.F2:
                    TriggerTestPopupResponse();
                    return true;

                case ConsoleKey.F3:
                    //Cycle
                    CycleSelectedLayout();
                    return true;

                case ConsoleKey.F5:
                    //Refresh and redraw
                    HardRedrawAll();
                    return true;

                default:
                    return false;
            }
        }

        private IEnumerable<Frames.ConsoleFrame> GetActiveFrames()
        {
            if (chatFrame.Active)
            {
                yield return chatFrame;
            }

            if (debugFrame.Active)
            {
                yield return debugFrame;
            }

            if (eventsFrame.Active)
            {
                yield return eventsFrame;
            }

            if (overrideFrame != null && overrideFrame.Active)
            {
                yield return overrideFrame;
            }
        }

        private void TriggerTestPopup()
        {
            overrideFrame = popupFrame;
            popupFrame.ShowPopup(
                title: "This is a test",
                message: "How does this message look?\nI tried to format it well.\nI guess we'll see...",
                buttonText: "Donezo",
                callback: () =>
                {
                    overrideFrame = null;
                    RedrawAll();
                });
        }
        private void TriggerTestPopupResponse()
        {
            overrideFrame = popupResponseFrame;
            popupResponseFrame.ShowPopup(
                title: "This is a test",
                message: "How does this message look?\nI tried to format it well.\nI guess we'll see...",
                buttonAText: "That's it!\nI Quit!\nGood day, sir!",
                buttonBText: "No, wait!\nI was having fun!",
                callbackA: () =>
                {
                    overrideFrame = null;
                    RedrawAll();
                },
                callbackB: () =>
                {
                    overrideFrame = null;
                    RedrawAll();
                });
        }

        private void CycleSelectedFrame()
        {
            Frames.ConsoleFrame[] frames = GetActiveFrames().ToArray();

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == selectedFrame)
                {
                    selectedFrame = frames[(i + 1) % frames.Length];
                    frameTitleDrawRequested = true;
                    return;
                }
            }

            WriteErrorLine($"Cycle not implemented for selected frame: {selectedFrame}");
            selectedFrame = frames[0];
            frameTitleDrawRequested = true;
        }

        private void CycleSelectedLayout()
        {
            selectedLayout.FocusLost();

            selectedFrame = selectedLayout.DefaultSelection;

            selectedLayout.FocusAcquired();

            borderDrawRequested = true;
            frameTitleDrawRequested = true;

            RedrawAllFrames();

            //HardRedrawAll();
        }

        private void QuitProgram()
        {
            overrideFrame = popupResponseFrame;
            popupResponseFrame.ShowPopup(
                title: "Quit",
                message: "Are you sure you want to quit?",
                buttonAText: "Quit",
                buttonBText: "No, wait!\nCancel! Cancel!",
                callbackA: () =>
                {
                    overrideFrame = null;
                    Console.Clear();
                    applicationManagement.TriggerExit();
                },
                callbackB: () =>
                {
                    overrideFrame = null;
                    RedrawAll();
                });
        }

        public void RedrawAllFrames()
        {
            frameTitleDrawRequested = true;

            foreach (Frames.ConsoleFrame frame in GetActiveFrames())
            {
                frame.Redraw();
            }
        }

        public void RedrawAll()
        {
            borderDrawRequested = true;
            RedrawAllFrames();
        }

        public void HardRedrawAll()
        {
            Console.Clear();
            selectedLayout.FocusAcquired();
            borderDrawRequested = true;
            RedrawAllFrames();
        }

        private void RedrawFrameTitles()
        {
            foreach (Frames.ConsoleFrame frame in GetActiveFrames())
            {
                Console.BackgroundColor = (frame == selectedFrame ? selectedHeaderBG : backgroundColor);
                Console.ForegroundColor = (frame == selectedFrame ? selectedHeaderFG : headerColor);
                Console.SetCursorPosition(frame.X, frame.Y - 2);
                Console.Write($" {frame.Title} ");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    communication.ReceiveEventHandlers -= ReceiveEventHandler;
                    communication.ReceiveMessageLoggers -= ReceiveMessageHandler;
                    communication.SendMessageHandlers -= SendPublicChatHandler;
                    communication.SendWhisperHandlers -= SendWhisperHandler;
                    communication.DebugMessageHandlers -= DebugMessageHandler;

                    generalTokenSource.Cancel();

                    readers.Signal();
                    readers.Wait();
                    readers.Dispose();

                    generalTokenSource.Dispose();
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
    }
}
