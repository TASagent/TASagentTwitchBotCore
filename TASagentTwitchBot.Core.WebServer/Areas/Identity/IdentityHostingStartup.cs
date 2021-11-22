[assembly: HostingStartup(typeof(TASagentTwitchBot.Core.WebServer.Areas.Identity.IdentityHostingStartup))]
namespace TASagentTwitchBot.Core.WebServer.Areas.Identity;

public class IdentityHostingStartup : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
        });
    }
}
