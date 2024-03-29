﻿namespace TASagentTwitchBot.Plugin.ControllerSpy.Readers;

sealed public class SerialControllerReader<T> : IControllerReader<T>, IDisposable
    where T : NewControllerState
{
    public event StateEventHandler<T>? ControllerStateChanged;
    public event ControllerDisconnectedHandler? ControllerDisconnected;

    private readonly Func<byte[], T> packetParser;
    private readonly SerialMonitor serialMonitor;
    public string PortName { get; init; }

    private bool disposedValue;

    public SerialControllerReader(
        string portName,
        Func<byte[], T> packetParser)
    {
        PortName = portName;
        this.packetParser = packetParser;

        serialMonitor = new SerialMonitor(portName, PacketReceived, Disconnected);
    }

    void Disconnected(object sender)
    {
        serialMonitor.Stop();
        ControllerDisconnected?.Invoke(this);
    }

    void PacketReceived(object sender, byte[] packet)
    {
        if (ControllerStateChanged is not null && packetParser(packet) is T state)
        {
            ControllerStateChanged(this, state);
        }
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                serialMonitor.Stop();
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
