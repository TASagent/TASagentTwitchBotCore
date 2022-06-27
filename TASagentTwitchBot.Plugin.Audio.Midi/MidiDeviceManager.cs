using NAudio.Midi;

using TASagentTwitchBot.Core;

namespace TASagentTwitchBot.Plugin.Audio.Midi;

[AutoRegister]
public interface IMidiDeviceManager
{
    List<string> GetMidiDevices();
    string? GetMidiDeviceName(int slot);
    bool UpdateMidiDevice(int slot, string name);
    void RegisterUpdateListener(IMidiDeviceUpdateListener listener);
}

[AutoRegister]
public interface IMidiDeviceUpdateListener
{
    void CloseMidiDevices(int slot);
    void NotifyMidiDeviceUpdate(int slot);
}

[AutoRegister]
public interface INAudioMidiDeviceManager : IMidiDeviceManager
{
    MidiIn? GetMidiDevice(int slot);
}

public class NAudioMidiDeviceManager : INAudioMidiDeviceManager, IMidiDeviceManager
{
    private readonly ICommunication communication;

    private readonly List<IMidiDeviceUpdateListener> updateListeners = new List<IMidiDeviceUpdateListener>();

    private readonly Dictionary<int, string> midiDeviceNames = new Dictionary<int, string>();


    public NAudioMidiDeviceManager(
        ICommunication communication)
    {
        this.communication = communication;
    }

    public List<string> GetMidiDevices()
    {
        List<string> devices = new List<string>();

        for (int i = 0; i < MidiIn.NumberOfDevices; i++)
        {
            devices.Add(MidiIn.DeviceInfo(i).ProductName);
        }

        return devices;
    }

    public string? GetMidiDeviceName(int slot) => midiDeviceNames.GetValueOrDefault(slot);
    public MidiIn? GetMidiDevice(int slot)
    {
        if (!midiDeviceNames.TryGetValue(slot, out string? device))
        {
            return null;
        }

        int deviceNumber = GetMidiDeviceNumber(device);

        if (deviceNumber == -1)
        {
            return null;
        }

        return new MidiIn(deviceNumber);
    }

    public void RegisterUpdateListener(IMidiDeviceUpdateListener listener)
    {
        updateListeners.Add(listener);
    }

    private static int GetMidiDeviceNumber(string name)
    {
        for (int i = 0; i < MidiIn.NumberOfDevices; i++)
        {
            if (MidiIn.DeviceInfo(i).ProductName == name)
            {
                return i;
            }
        }

        return -1;
    }

    public bool UpdateMidiDevice(int slot, string name)
    {
        foreach (IMidiDeviceUpdateListener listener in updateListeners)
        {
            try
            {
                listener.CloseMidiDevices(slot);
            }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"Exception logged trying to close Midi devices for {listener}: {ex.Message}");
            }
        }

        if (string.IsNullOrEmpty(name))
        {
            midiDeviceNames.Remove(slot);
            return true;
        }

        if (GetMidiDeviceNumber(name) == -1)
        {
            //Unable to find device
            return false;
        }

        midiDeviceNames[slot] = name;

        foreach (IMidiDeviceUpdateListener listener in updateListeners)
        {
            try
            {
                listener.NotifyMidiDeviceUpdate(slot);
            }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"Exception logged trying to Update Midi devices for {listener}: {ex.Message}");
            }
        }

        //Success
        return true;
    }
}
