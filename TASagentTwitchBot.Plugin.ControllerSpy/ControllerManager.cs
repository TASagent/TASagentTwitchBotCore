using System.IO.Ports;
using Microsoft.AspNetCore.SignalR;

namespace TASagentTwitchBot.Plugin.ControllerSpy;

public interface IControllerManager
{
    List<string> GetPorts();
    string GetCurrentPort();
    bool Attach(string port);
    void Detatch();
}


public class ControllerManager : IControllerManager, IDisposable
{
    private readonly Core.ICommunication communication;
    private readonly IHubContext<Web.Hubs.ControllerSpyHub> controllerSpyHub;

    private Readers.SerialControllerReader<Readers.SNESControllerState>? currentSerialPortReader = null;
    private Readers.NewControllerState? lastState = null;
    private bool disposedValue;

    public ControllerManager(
        Core.ICommunication communication,
        IHubContext<Web.Hubs.ControllerSpyHub> controllerSpyHub)
    {
        this.communication = communication;
        this.controllerSpyHub = controllerSpyHub;
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
}
