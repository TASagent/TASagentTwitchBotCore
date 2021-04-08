//using System;
//using System.Collections.Generic;

//namespace TASagentTwitchBot.Plugin.ControllerSpy.Readers
//{
//    static public class ClassicController
//    {
//        const int PACKET_SIZE = 48;

//        static readonly string[] BUTTONS = {
//            null, "r", "plus", "home", "minus", "l", "down", "right", "up", "left", "zr", "x", "a", "y", "b", "zl"
//        };

//        static float ReadLeftStick(int input) => (float)(input - 30) / 30;
//        static float ReadRightStick(int input) => (float)(input - 15) / 15;
//        static float ReadTrigger(int input) => (float)(input) / 31;
//        static byte Unencrypt(byte packet) => (byte)((byte)(packet ^ 0x17) + 0x17);

//        static public ControllerState ReadFromPacket(byte[] packet)
//        {
//            if (packet.Length < PACKET_SIZE)
//            {
//                return null;
//            }

//            byte[] data = new byte[PACKET_SIZE / 8];
//            for (int i = 0; i < PACKET_SIZE / 8; i++)
//            {
//                data[i] = Unencrypt(SignalTool.ReadByte(packet, i * 8));
//            }

//            ControllerStateBuilder state = new ControllerStateBuilder();

//            for (int i = 0; i < BUTTONS.Length; ++i)
//            {
//                if (string.IsNullOrEmpty(BUTTONS[i]))
//                {
//                    continue;
//                }

//                state.SetButton(BUTTONS[i], (data[4 + i / 8] & (0x01 << (i % 8))) == 0x00);
//            }

//            state.SetAnalog("lstick_x", ReadLeftStick(data[0] & 0x3F));
//            state.SetAnalog("lstick_y", ReadLeftStick(data[1] & 0x3F));
//            state.SetAnalog("rstick_x", ReadRightStick(((data[0] & 0xC0) >> 3) | ((data[1] & 0xC0) >> 5) | ((data[2] & 0x80) >> 7)));
//            state.SetAnalog("rstick_y", ReadRightStick(data[2] & 0x1F));
//            state.SetAnalog("trig_l", ReadTrigger(((data[2] & 0x60) >> 2) | ((data[3] & 0xE0) >> 5)));
//            state.SetAnalog("trig_r", ReadTrigger(data[3] & 0x1F));

//            return state.Build();
//        }
//    }
//}
