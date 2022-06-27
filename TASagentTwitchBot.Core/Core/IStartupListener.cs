namespace TASagentTwitchBot.Core;

//These classes are explicitly constructed at startup
[AutoRegister]
public interface IStartupListener
{
    void NotifyStartup() { }
}
