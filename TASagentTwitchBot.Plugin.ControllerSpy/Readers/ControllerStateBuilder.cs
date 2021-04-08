//using System;
//using System.Collections.Generic;

//namespace TASagentTwitchBot.Plugin.ControllerSpy.Readers
//{
//    sealed public class ControllerStateBuilder
//    {
//        readonly Dictionary<string, bool> buttons = new Dictionary<string, bool>();
//        readonly Dictionary<string, float> analogs = new Dictionary<string, float>();

//        public void SetButton(string name, bool value) => buttons[name] = value;

//        public void SetAnalog(string name, float value) => analogs[name] = value;

//        public ControllerState Build() => new ControllerState(buttons, analogs);
//    }
//}
