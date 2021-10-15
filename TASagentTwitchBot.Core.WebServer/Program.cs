using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;


using TASagentTwitchBot.Core.WebServer.Database;
using TASagentTwitchBot.Core.WebServer.Models;

namespace TASagentTwitchBot.Core.WebServer
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            //Initialize DataManagement
            BGC.IO.DataManagement.Initialize("TASagentBotWebServer");

            using IHost host = CreateHostBuilder(args).Build();

            using (IServiceScope scope = host.Services.CreateScope())
            {
                IServiceProvider services = scope.ServiceProvider;
                ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();
                try
                {
                    ApplicationDbContext context = services.GetRequiredService<ApplicationDbContext>();
                    UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                    RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    await ContextSeed.SeedRolesAsync(userManager, roleManager);
                    await ContextSeed.SeedSuperAdminAsync(userManager, roleManager);
                }
                catch (Exception ex)
                {
                    ILogger<Program> logger = loggerFactory.CreateLogger<Program>();
                    logger.LogError(ex, "An error occurred seeding the DB.");
                }
            }

            await host.StartAsync();

            TASagentWebServer application = host.Services.GetService(typeof(TASagentWebServer)) as TASagentWebServer;
            await application.RunAsync();

            await host.StopAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://0.0.0.0:5003/");
                    webBuilder.UseStartup<Startup>();
                });
    }
}
