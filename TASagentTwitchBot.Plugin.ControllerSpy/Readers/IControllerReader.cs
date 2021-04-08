using System;
using System.Collections.Generic;

namespace TASagentTwitchBot.Plugin.ControllerSpy.Readers
{
    public delegate void StateEventHandler<T>(IControllerReader<T> sender, T state) where T : NewControllerState;

    public interface IControllerReader<T> : IDisposable
        where T : NewControllerState
    {
        event StateEventHandler<T> ControllerStateChanged;
        event EventHandler ControllerDisconnected;
    }
}
