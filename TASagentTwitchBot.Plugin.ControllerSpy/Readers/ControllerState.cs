//using System;
//using System.Collections.Generic;

//namespace TASagentTwitchBot.Plugin.ControllerSpy.Readers
//{
//    public class ControllerState
//    {
//        static public readonly ControllerState Zero = new ControllerState
//            (new Dictionary<string, bool>(), new Dictionary<string, float>());

//        public IReadOnlyDictionary<string, bool> Buttons { get; private set; }
//        public IReadOnlyDictionary<string, float> Analogs { get; private set; }

//        public ControllerState(IReadOnlyDictionary<string, bool> buttons, IReadOnlyDictionary<string, float> analogs)
//        {
//            Buttons = buttons;
//            Analogs = analogs;
//        }

//        //public static bool operator ==(in ControllerState lhs, in ControllerState rhs)
//        //{
//        //    foreach (var kvp in lhs.Analogs)
//        //    {
//        //        if (!rhs.Analogs.ContainsKey(kvp.Key) || rhs.Analogs[kvp.Key] != kvp.Value)
//        //        {
//        //            return false;
//        //        }
//        //    }

//        //    foreach (var kvp in lhs.Buttons)
//        //    {
//        //        if (!rhs.Buttons.ContainsKey(kvp.Key) || rhs.Buttons[kvp.Key] != kvp.Value)
//        //        {
//        //            return false;
//        //        }
//        //    }

//        //    return true;
//        //}

//        //public static bool operator !=(in ControllerState lhs, in ControllerState rhs) => !(lhs == rhs);

//        //public override bool Equals(object obj)
//        //{
//        //    if (ReferenceEquals(this, obj))
//        //    {
//        //        return true;
//        //    }

//        //    if (obj is null)
//        //    {
//        //        return false;
//        //    }

//        //    if (obj is ControllerState other)
//        //    {
//        //        return this == other;
//        //    }

//        //    return false;
//        //}

//        //public override int GetHashCode()
//        //{
//        //    throw new NotImplementedException();
//        //}
//    }
//}
