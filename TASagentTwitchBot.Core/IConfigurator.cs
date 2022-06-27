namespace TASagentTwitchBot.Core;

[AutoRegister]
public interface IConfigurator
{
    Task<bool> VerifyConfigured();
}
