using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TASagentTwitchBot.BotConfigurator
{
    public class Startup : Core.StartupCore
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
        }

        protected override string[] GetExcludedFeatures() =>
            new string[] { "Database", "TTS", "Overlay", "Audio" };

        protected override void ConfigureCustomServices(IServiceCollection services)
        {
            //Swapping out view for basic
            services.AddSingleton<Core.View.IConsoleOutput, Core.View.BasicView>();

            //Add Core Application
            services.AddSingleton<BotConfigurator>();
        }

        //Database is unused
        protected override void ConfigureDatabases(IServiceCollection services) { }

        //Static files are disabled
        protected override void ConfigureStaticFiles(IApplicationBuilder app, IWebHostEnvironment env) { }

        //WebSub is disabled
        protected override void ConfigureCoreWebSubServices(IServiceCollection services) { }

        //Disabling Core Middleware
        protected override void ConfigureCoreMiddleware(IApplicationBuilder app) { }

        protected override void ConstructCoreSingletons(IServiceProvider serviceProvider)
        {
            //Make sure required services are constructed
            serviceProvider.GetRequiredService<Core.Config.IBotConfigContainer>().Initialize();
            serviceProvider.GetRequiredService<Core.View.IConsoleOutput>();
        }
    }
}
