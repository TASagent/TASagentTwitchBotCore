using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TASagentTwitchBot.Core.WebServer.Database;

[assembly: HostingStartup(typeof(TASagentTwitchBot.Core.WebServer.Areas.Identity.IdentityHostingStartup))]
namespace TASagentTwitchBot.Core.WebServer.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
            });
        }
    }
}