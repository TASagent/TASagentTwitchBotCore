using System.Text.Json.Serialization;

namespace TASagentTwitchBot.Plugin.ControllerSpy.Readers;

public abstract class NewControllerState
{
    [JsonIgnore]
    protected readonly bool[] buttons;
    [JsonIgnore]
    protected readonly float[] sticks;

    public NewControllerState(
        bool[] buttons,
        float[] sticks)
    {
        this.buttons = buttons;
        this.sticks = sticks;
    }

    public override bool Equals(object? obj) =>
        obj is NewControllerState state &&
        state == this;

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();

        for (int i = 0; i < buttons.Length; i++)
        {
            hash.Add(buttons[i]);
        }

        for (int i = 0; i < sticks.Length; i++)
        {
            hash.Add(sticks[i]);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(in NewControllerState lhs, in NewControllerState rhs)
    {
        if (lhs is null)
        {
            return rhs is null;
        }

        if (rhs is null)
        {
            return false;
        }

        if (lhs.buttons.Length != rhs.buttons.Length || lhs.sticks.Length != rhs.sticks.Length)
        {
            return false;
        }

        for (int i = 0; i < lhs.buttons.Length; i++)
        {
            if (lhs.buttons[i] != rhs.buttons[i])
            {
                return false;
            }
        }

        for (int i = 0; i < lhs.sticks.Length; i++)
        {
            if (lhs.sticks[i] != rhs.sticks[i])
            {
                return false;
            }
        }

        return true;
    }

    public static bool operator !=(in NewControllerState lhs, in NewControllerState rhs) => !(lhs == rhs);
}

public class SNESControllerState : NewControllerState
{
    public bool A => buttons[8];
    public bool B => buttons[0];
    public bool X => buttons[9];
    public bool Y => buttons[1];
    public bool L => buttons[10];
    public bool R => buttons[11];
    public bool Start => buttons[3];
    public bool Select => buttons[2];
    public bool Up => buttons[4];
    public bool Down => buttons[5];
    public bool Left => buttons[6];
    public bool Right => buttons[7];

    public static SNESControllerState Parse(byte[] packet) =>
        new SNESControllerState(ExtractButtons(packet), ExtractSticks(packet));

    public SNESControllerState(
        bool[] buttons,
        float[] sticks)
        : base(buttons, sticks)
    {

    }

    public SNESControllerState(
        bool a = false,
        bool b = false,
        bool x = false,
        bool y = false,
        bool l = false,
        bool r = false,
        bool start = false,
        bool select = false,
        bool up = false,
        bool down = false,
        bool left = false,
        bool right = false)
        : base(new[] { b, y, select, start, up, down, left, right, a, x, l, r }, Array.Empty<float>())
    {

    }

    private static bool[] ExtractButtons(byte[] packet)
    {
        bool[] buttons = new bool[12];

        for (int i = 0; i < buttons.Length && i < packet.Length; i++)
        {
            buttons[i] = packet[i] != 0x00;
        }

        return buttons;
    }

    private static float[] ExtractSticks(byte[] packet)
    {
        return Array.Empty<float>();
    }
}
