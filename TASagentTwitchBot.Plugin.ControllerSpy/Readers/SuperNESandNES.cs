//using System;
//using System.Collections.Generic;

//namespace TASagentTwitchBot.Plugin.ControllerSpy.Readers
//{
//    static public class SuperNESandNES
//    {
//        static ControllerState ReadPacketButtons(byte[] packet, string[] buttons)
//        {
//            if (packet.Length < buttons.Length)
//            {
//                return null;
//            }

//            ControllerStateBuilder state = new ControllerStateBuilder();

//            for (int i = 0; i < buttons.Length; ++i)
//            {
//                if (string.IsNullOrEmpty(buttons[i]))
//                {
//                    continue;
//                }

//                state.SetButton(buttons[i], packet[i] != 0x00);
//            }

//            return state.Build();
//        }


//        static readonly string[] BUTTONS_NES = {
//            "a", "b", "select", "start", "up", "down", "left", "right"
//        };

//        static readonly string[] BUTTONS_SNES = {
//            "b", "y", "select", "start", "up", "down", "left", "right", "a", "x", "l", "r", null, null, null, null
//        };

//        static public ControllerState ReadFromPacket_NES(byte[] packet) => ReadPacketButtons(packet, BUTTONS_NES);

//        static public ControllerState ReadFromPacket_SNES(byte[] packet) => ReadPacketButtons(packet, BUTTONS_SNES);
//    }

//}
