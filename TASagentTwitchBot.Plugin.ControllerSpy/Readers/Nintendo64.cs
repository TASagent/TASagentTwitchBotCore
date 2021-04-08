//using System;
//using System.Collections.Generic;

//namespace TASagentTwitchBot.Plugin.ControllerSpy.Readers
//{
//    static public class Nintendo64
//    {
//        const int PACKET_SIZE = 32;

//        static readonly string[] BUTTONS = {
//            "a", "b", "z", "start", "up", "down", "left", "right", null, null, "l", "r", "cup", "cdown", "cleft", "cright"
//        };

//        static float ReadStick(byte input) => (float)((sbyte)input) / 128;

//        static public ControllerState ReadFromPacket(byte[] packet)
//        {
//            if (packet.Length < PACKET_SIZE)
//            {
//                return null;
//            }

//            ControllerStateBuilder state = new ControllerStateBuilder();

//            for (int i = 0; i < BUTTONS.Length; ++i)
//            {
//                if (string.IsNullOrEmpty(BUTTONS[i]))
//                {
//                    continue;
//                }

//                state.SetButton(BUTTONS[i], packet[i] != 0x00);
//            }

//            state.SetAnalog("stick_x", ReadStick(SignalTool.ReadByte(packet, BUTTONS.Length)));
//            state.SetAnalog("stick_y", ReadStick(SignalTool.ReadByte(packet, BUTTONS.Length + 8)));

//            return state.Build();
//        }
//    }
//}
