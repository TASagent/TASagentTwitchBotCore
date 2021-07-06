using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core.Web.Hubs;

namespace TASagentTwitchBot.Core.Timer
{
    public interface ITimerManager : IDisposable
    {
        TimerState GetTimerState();

        Task Start();
        Task Stop();
        Task Reset();
        Task Set(double seconds);

        Task MarkLap();
        Task UnmarkLap();
        Task ResetCurrentLap();

        Task MarkLapAtAbsolute(double millisecondValue);
        Task MarkLapAtRelative(double millisecondValue);
        Task DropLap(int index);

        Task UpdateLayout(TimerLayoutData layout);

        List<TimerData> GetSavedTimers();
        Task<bool> LoadTimer(string name);
        void SaveTimer(string name);
    }


    public record TimerState(double CurrentMS, bool Ticking, List<double> Laps, TimerLayoutData Layout);

    public class SavedTimerData
    {
        public TimerLayoutData Layout { get; set; } = new TimerLayoutData("", TimerDisplayMode.Cumulative, "", TimerDisplayMode.None);
        public Dictionary<string, TimerData> Timers { get; set; } = new Dictionary<string, TimerData>();
    }

    public record TimerData(string Name, double EndingTime, List<double> Laps, DateTime LastUpdated);

    public record TimerLayoutData(string MainLabel, TimerDisplayMode MainDisplay, string SecondaryLabel, TimerDisplayMode SecondaryDisplay);

    public enum TimerDisplayMode
    {
        None = 0,
        Cumulative,
        Current,
        LapStart,
        MAX
    }

    public class TimerManager : ITimerManager, IDisposable
    {
        private readonly IHubContext<TimerHub> timerHubContext;
        private readonly ICommunication communication;

        private readonly SavedTimerData savedTimerData;
        private readonly string dataFilePath;
        private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();

        private bool ticking = false;
        private readonly List<double> laps = new List<double>();
        private TimeSpan timerExtraDuration;
        private DateTime timerStartTime;

        private static readonly TimeSpan autosaveInterval = new TimeSpan(0, 5, 0);
        private bool autosaveUpdated = true;

        private bool disposedValue;

        public TimerManager(
            ICommunication communication,
            IHubContext<TimerHub> timerHubContext)
        {
            this.communication = communication;
            this.timerHubContext = timerHubContext;

            dataFilePath = BGC.IO.DataManagement.PathForDataFile("Config", "Timers.json");

            if (File.Exists(dataFilePath))
            {
                savedTimerData = JsonSerializer.Deserialize<SavedTimerData>(File.ReadAllText(dataFilePath));
            }
            else
            {
                savedTimerData = new SavedTimerData();
                File.WriteAllText(dataFilePath, JsonSerializer.Serialize(savedTimerData));
            }

            AutosaveMonitor();
        }

        public TimerState GetTimerState()
        {
            if (!ticking)
            {
                return new TimerState(
                    CurrentMS: timerExtraDuration.TotalMilliseconds,
                    Ticking: ticking,
                    Laps: laps,
                    Layout: savedTimerData.Layout);
            }

            return new TimerState(
                CurrentMS: (timerExtraDuration + (DateTime.Now - timerStartTime)).TotalMilliseconds,
                Ticking: ticking,
                Laps: laps,
                Layout: savedTimerData.Layout);
        }

        private void HandleAutosave()
        {
            if (!autosaveUpdated)
            {
                //Only mark autosave as updated if the timer is paused
                autosaveUpdated = !ticking;
                SaveTimer("Autosave");
            }
        }

        private async void AutosaveMonitor()
        {
            try
            {
                while (true)
                {
                    //Check every 5 minutes for an autosave update
                    await Task.Delay(autosaveInterval, generalTokenSource.Token);

                    if (generalTokenSource.IsCancellationRequested)
                    {
                        break;
                    }

                    HandleAutosave();
                }
            }
            catch (TaskCanceledException) { /* swallow */}
            catch (OperationCanceledException) { /* swallow */}
            catch (Exception ex)
            {
                communication.SendErrorMessage($"TimerManager Exception Type {ex.GetType().Name}: {ex}");
            }
        }


        public async Task Start()
        {
            if (!ticking)
            {
                ticking = true;
                timerStartTime = DateTime.Now;
            }

            //Unset AutosaveUpdated
            autosaveUpdated = false;

            await timerHubContext.Clients.All.SendAsync("SetState", GetTimerState());
        }

        public async Task Stop()
        {
            if (ticking)
            {
                ticking = false;
                timerExtraDuration += DateTime.Now - timerStartTime;
            }

            await timerHubContext.Clients.All.SendAsync("SetState", GetTimerState());
        }

        public async Task Reset()
        {
            ticking = false;
            timerExtraDuration = new TimeSpan(0);
            laps.Clear();

            await timerHubContext.Clients.All.SendAsync("SetState", GetTimerState());
        }

        public async Task Set(double seconds)
        {
            ticking = false;
            timerExtraDuration = new TimeSpan(0, 0, (int)seconds);

            await timerHubContext.Clients.All.SendAsync("SetState", GetTimerState());
        }

        public async Task ResetCurrentLap()
        {
            ticking = false;
            timerExtraDuration = new TimeSpan(0);

            await timerHubContext.Clients.All.SendAsync("SetState", GetTimerState());
        }

        public async Task MarkLap()
        {
            double priorLap;

            if (ticking)
            {
                priorLap = (timerExtraDuration + (DateTime.Now - timerStartTime)).TotalMilliseconds;
                timerStartTime = DateTime.Now;
            }
            else
            {
                priorLap = timerExtraDuration.TotalMilliseconds;
            }

            timerExtraDuration = new TimeSpan(0);
            laps.Add(priorLap);

            await timerHubContext.Clients.All.SendAsync("SetState", GetTimerState());
        }

        public async Task UnmarkLap()
        {
            if (laps.Count == 0)
            {
                //Do nothing
                return;
            }

            timerExtraDuration += TimeSpan.FromMilliseconds(laps[^1]);
            laps.RemoveAt(laps.Count - 1);

            await timerHubContext.Clients.All.SendAsync("SetState", GetTimerState());
        }

        public async Task MarkLapAtAbsolute(double millisecondValue)
        {
            int lapIndex = -1;
            for (int i = 0; i < laps.Count; i++)
            {
                if (millisecondValue <= laps[i])
                {
                    lapIndex = i;
                    break;
                }

                millisecondValue -= laps[i];
            }

            if (lapIndex == -1)
            {
                //Lap is being added to the end
                double currentTime;

                if (ticking)
                {
                    //Make sure active + stored time is enough
                    currentTime = (timerExtraDuration + (DateTime.Now - timerStartTime)).TotalMilliseconds;
                }
                else
                {
                    //Make sure stored time is enough
                    currentTime = timerExtraDuration.TotalMilliseconds;
                }

                if (currentTime < millisecondValue)
                {
                    //Not enough time has elapsed to allow this - Advance
                    laps.Add(millisecondValue);
                    timerStartTime = DateTime.Now;
                    timerExtraDuration = new TimeSpan(0);
                }
                else
                {
                    //Enough time - Decrement and add lap
                    laps.Add(millisecondValue);
                    timerExtraDuration = TimeSpan.FromMilliseconds(currentTime - millisecondValue);
                    timerStartTime = DateTime.Now;
                }
            }
            else
            {
                //Lap is being inserted in the middle
                laps.Insert(lapIndex, millisecondValue);
                //Decrement time from next lap
                laps[lapIndex + 1] -= millisecondValue;
            }

            //Send Updates
            await timerHubContext.Clients.All.SendAsync("SetState", GetTimerState());
        }

        public async Task MarkLapAtRelative(double millisecondValue)
        {
            if (millisecondValue < 0)
            {
                return;
            }

            double currentTime;

            if (ticking)
            {
                //Make sure active + stored time is enough
                currentTime = (timerExtraDuration + (DateTime.Now - timerStartTime)).TotalMilliseconds;
            }
            else
            {
                //Make sure stored time is enough
                currentTime = timerExtraDuration.TotalMilliseconds;
            }

            if (currentTime < millisecondValue)
            {
                //Not enough time has elapsed to allow this - Advance
                laps.Add(millisecondValue);
                timerStartTime = DateTime.Now;
                timerExtraDuration = new TimeSpan(0);
            }
            else
            {
                //Enough time - Decrement and add lap
                laps.Add(millisecondValue);
                timerExtraDuration = TimeSpan.FromMilliseconds(currentTime - millisecondValue);
                timerStartTime = DateTime.Now;
            }

            //Send Updates
            await timerHubContext.Clients.All.SendAsync("SetState", GetTimerState());
        }

        public async Task DropLap(int index)
        {
            if (index < 0 || index >= laps.Count)
            {
                //Bad Lap
                return;
            }

            double droppingTime = laps[index];
            laps.RemoveAt(index);

            if (index == laps.Count)
            {
                //Add time to current
                timerExtraDuration += TimeSpan.FromMilliseconds(droppingTime);
            }
            else
            {
                //Add time to next
                laps[index] += droppingTime;
            }

            //Send Updates
            await timerHubContext.Clients.All.SendAsync("SetState", GetTimerState());
        }

        public List<TimerData> GetSavedTimers() => new List<TimerData>(savedTimerData.Timers.Values);

        public async Task UpdateLayout(TimerLayoutData layout)
        {
            lock (savedTimerData)
            {
                savedTimerData.Layout = layout;
                Serialize();
            }

            await timerHubContext.Clients.All.SendAsync("SetState", GetTimerState());
        }

        public async Task<bool> LoadTimer(string name)
        {
            if (!savedTimerData.Timers.ContainsKey(name))
            {
                return false;
            }

            TimerData timerData = savedTimerData.Timers[name];
            ticking = false;
            timerExtraDuration = TimeSpan.FromMilliseconds(timerData.EndingTime);
            laps.Clear();
            laps.AddRange(timerData.Laps);

            await timerHubContext.Clients.All.SendAsync("SetState", GetTimerState());

            return true;
        }

        public void SaveTimer(string name)
        {
            if (savedTimerData.Timers.ContainsKey(name))
            {
                savedTimerData.Timers.Remove(name);
            }

            double currentTime;
            if (ticking)
            {
                //Make sure active + stored time is enough
                currentTime = (timerExtraDuration + (DateTime.Now - timerStartTime)).TotalMilliseconds;
            }
            else
            {
                //Make sure stored time is enough
                currentTime = timerExtraDuration.TotalMilliseconds;
            }

            savedTimerData.Timers.Add(name, new TimerData(name, currentTime, new List<double>(laps), DateTime.Now));

            Serialize();
        }

        private void Serialize()
        {
            lock (savedTimerData)
            {
                File.WriteAllText(dataFilePath, JsonSerializer.Serialize(savedTimerData));
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    HandleAutosave();

                    generalTokenSource.Cancel();
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
