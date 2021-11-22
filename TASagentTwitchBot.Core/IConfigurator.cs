namespace TASagentTwitchBot.Core;

public interface IConfigurator
{
    Task<bool> VerifyConfigured();
}
