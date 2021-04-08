using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Plugin.ControllerSpy
{
    public delegate void PacketEventHandler(object sender, byte[] packet);

    public class SerialMonitor : IDisposable
    {
        const int BAUD_RATE = 115200;
        const int TIMER_MS = 30;

        public event PacketEventHandler PacketReceived;
        public event EventHandler Disconnected;

        private SerialPort dataPort;
        readonly List<byte> localBuffer = new List<byte>();

        private bool reading = false;
        private bool disposedValue;

        public SerialMonitor(string portName)
        {
            dataPort = new SerialPort(portName, BAUD_RATE)
            {
                Handshake = Handshake.RequestToSend,
                DtrEnable = true
            };
        }

        public void Start()
        {
            if (reading)
            {
                return;
            }

            localBuffer.Clear();
            dataPort.Open();

            ReadData();
        }

        public void Stop()
        {
            if (dataPort != null)
            {
                try
                {
                    // If the device has been unplugged, Close will throw an IOException.  This is fine, we'll just keep cleaning up.
                    dataPort.Close();
                }
                catch (IOException) { }
                dataPort = null;
            }

            reading = false;
        }

        private async void ReadData()
        {
            reading = true;
            try
            {
                while (reading)
                {
                    Tick();

                    await Task.Delay(TIMER_MS);
                }
            }
            finally
            {
                reading = false;
            }
        }

        void Tick()
        {
            if (dataPort == null || !dataPort.IsOpen || PacketReceived == null)
            {
                return;
            }

            // Try to read some data from the COM port and append it to our localBuffer.
            // If there's an IOException then the device has been disconnected.
            try
            {
                int readCount = dataPort.BytesToRead;
                if (readCount < 1)
                {
                    return;
                }

                byte[] readBuffer = new byte[readCount];
                dataPort.Read(readBuffer, 0, readCount);
                dataPort.DiscardInBuffer();
                localBuffer.AddRange(readBuffer);
            }
            catch (IOException)
            {
                Stop();

                Disconnected?.Invoke(this, EventArgs.Empty);

                return;
            }

            // Try and find 2 splitting characters in our buffer.
            int lastSplitIndex = localBuffer.LastIndexOf(0x0A);
            if (lastSplitIndex <= 1)
            {
                return;
            }

            int sndLastSplitIndex = localBuffer.LastIndexOf(0x0A, lastSplitIndex - 1);
            if (lastSplitIndex == -1)
            {
                return;
            }

            // Grab the latest packet out of the buffer and fire it off to the receive event listeners.
            int packetStart = sndLastSplitIndex + 1;
            int packetSize = lastSplitIndex - packetStart;
            PacketReceived(this, localBuffer.GetRange(packetStart, packetSize).ToArray());

            // Clear our buffer up until the last split character.
            localBuffer.RemoveRange(0, lastSplitIndex);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
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
