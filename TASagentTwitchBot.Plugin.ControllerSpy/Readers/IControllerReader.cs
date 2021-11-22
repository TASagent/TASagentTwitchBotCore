namespace TASagentTwitchBot.Plugin.ControllerSpy.Readers;

public delegate void StateEventHandler<T>(IControllerReader<T> sender, T state) where T : NewControllerState;
public delegate void ControllerDisconnectedHandler(object sender);

public interface IControllerReader<T> : IDisposable
    where T : NewControllerState
{
    event StateEventHandler<T> ControllerStateChanged;
    event ControllerDisconnectedHandler ControllerDisconnected;
}
