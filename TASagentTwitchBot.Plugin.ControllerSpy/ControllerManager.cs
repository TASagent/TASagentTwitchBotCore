using System.IO.Ports;
using Microsoft.AspNetCore.SignalR;

namespace TASagentTwitchBot.Plugin.ControllerSpy;

[Core.AutoRegister]
public interface IControllerManager
{
    List<string> GetPorts();
    string GetCurrentPort();
    bool Attach(string port);
    void Detatch();

    void RegisterAction(
        IEnumerable<Readers.NewControllerState> sequence,
        string name,
        Action callback);
}


public class ControllerManager : IControllerManager, IDisposable
{
    private readonly Core.ICommunication communication;
    private readonly IHubContext<Web.Hubs.ControllerSpyHub> controllerSpyHub;

    private Readers.SerialControllerReader<Readers.SNESControllerState>? currentSerialPortReader = null;
    private Readers.NewControllerState? lastState = null;
    private bool disposedValue;

    private readonly List<ButtonSequence> actionSequences = new List<ButtonSequence>();
    private readonly HashSet<Readers.NewControllerState> sequenceTriggers = new HashSet<Readers.NewControllerState>();
    private readonly List<ActiveSequence> activeSequences = new List<ActiveSequence>();

    public ControllerManager(
        Core.ICommunication communication,
        IHubContext<Web.Hubs.ControllerSpyHub> controllerSpyHub)
    {
        this.communication = communication;
        this.controllerSpyHub = controllerSpyHub;
    }

    public void RegisterAction(
        IEnumerable<Readers.NewControllerState> sequence,
        string name,
        Action callback)
    {
        List<Readers.NewControllerState> sequenceList = sequence.ToList();

        actionSequences.Add(new ButtonSequence(
            Name: name,
            Sequence: sequenceList,
            Callback: callback));

        sequenceTriggers.Add(sequenceList[0]);
    }


    public List<string> GetPorts() => SerialPort.GetPortNames().ToList();
    public string GetCurrentPort() => currentSerialPortReader?.PortName ?? "";

    public void Detatch()
    {
        if (currentSerialPortReader is not null)
        {
            currentSerialPortReader.Dispose();
            currentSerialPortReader = null;
            lastState = null;
        }
    }

    public bool Attach(string port)
    {
        if (currentSerialPortReader is not null)
        {
            currentSerialPortReader.Dispose();
            currentSerialPortReader = null;
            lastState = null;
        }

        try
        {
            currentSerialPortReader = new Readers.SerialControllerReader<Readers.SNESControllerState>(port, Readers.SNESControllerState.Parse);
            currentSerialPortReader.ControllerStateChanged += ControllerStateChanged;
            currentSerialPortReader.ControllerDisconnected += ControllerDisconnected;
        }
        catch (Exception ex)
        {
            communication.SendWarningMessage($"Exception trying to bind ControllerManager to port {port}: {ex}");
            currentSerialPortReader = null;
        }

        return currentSerialPortReader is not null;
    }

    private void ControllerStateChanged(
        Readers.IControllerReader<Readers.SNESControllerState> reader,
        Readers.SNESControllerState newState)
    {
        if (lastState! != newState)
        {
            //Handle new state
            controllerSpyHub.Clients.All.SendAsync("ControllerUpdate", newState);

            for (int i = activeSequences.Count - 1; i >= 0; i--)
            {
                if (activeSequences[i].MatchesNext(newState))
                {
                    if (activeSequences[i].Increment())
                    {
                        activeSequences[i].ButtonSequence.Callback();
                        activeSequences.RemoveAt(i);
                    }
                }
                else
                {
                    activeSequences.RemoveAt(i);
                }
            }

            if (sequenceTriggers.Contains(newState))
            {
                //Queue up
                foreach (ButtonSequence seq in actionSequences)
                {
                    if (seq.Sequence[0] == newState)
                    {
                        activeSequences.Add(new ActiveSequence(seq));
                    }
                }
            }

            lastState = newState;
        }
    }

    private void ControllerDisconnected(object sender)
    {
        if (currentSerialPortReader is not null)
        {
            currentSerialPortReader.ControllerStateChanged -= ControllerStateChanged;
            currentSerialPortReader.ControllerDisconnected -= ControllerDisconnected;

            currentSerialPortReader.Dispose();
            currentSerialPortReader = null;
        }

        lastState = null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                currentSerialPortReader?.Dispose();
                currentSerialPortReader = null;
                lastState = null;
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

    private record ButtonSequence(
        string Name,
        IReadOnlyList<Readers.NewControllerState> Sequence,
        Action Callback);

    private class ActiveSequence
    {
        public ButtonSequence ButtonSequence { get; }
        public int Index { get; set; }

        public ActiveSequence(ButtonSequence buttonSequence)
        {
            ButtonSequence = buttonSequence;
            Index = 1;
        }

        public bool MatchesNext(Readers.NewControllerState newState) => ButtonSequence.Sequence[Index] == newState;
        public bool Increment() => ++Index == ButtonSequence.Sequence.Count;
    }
}
