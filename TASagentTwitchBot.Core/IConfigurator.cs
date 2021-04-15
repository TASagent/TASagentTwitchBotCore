using System.Threading.Tasks;

namespace TASagentTwitchBot.Core
{
    public interface IConfigurator
    {
        Task<bool> VerifyConfigured();
    }
}
