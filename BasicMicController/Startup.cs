using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BasicMicController
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            //services.AddSignalR(o => o.EnableDetailedErrors = true);
            services.AddSignalR();

            services
                .AddSingleton<BasicMicApplication>()
                .AddSingleton<TASagentTwitchBot.Core.ErrorHandler>()
                .AddSingleton<TASagentTwitchBot.Core.ApplicationManagement>()
                .AddSingleton<TASagentTwitchBot.Core.Notifications.NotificationServer>()
                .AddSingleton<TASagentTwitchBot.Core.Audio.MidiKeyboardHandler>();

            services
                .AddSingleton<TASagentTwitchBot.Core.Config.IBotConfigContainer, TASagentTwitchBot.Core.Config.BotConfigContainer>()
                .AddSingleton<TASagentTwitchBot.Core.Notifications.IActivityDispatcher, TASagentTwitchBot.Core.Notifications.ActivityDispatcher>()
                .AddSingleton<TASagentTwitchBot.Core.ICommunication, TASagentTwitchBot.Core.CommunicationHandler>()
                .AddSingleton<TASagentTwitchBot.Core.Audio.IMicrophoneHandler, TASagentTwitchBot.Core.Audio.MicrophoneHandler>()
                .AddSingleton<TASagentTwitchBot.Core.IMessageAccumulator, TASagentTwitchBot.Core.MessageAccumulator>()
                .AddSingleton<TASagentTwitchBot.Core.View.IConsoleOutput, TASagentTwitchBot.Core.View.BasicView>()
                .AddSingleton<TASagentTwitchBot.Core.TTS.ITTSRenderer, TASagentTwitchBot.Core.TTS.TTSRenderer>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            
            app.UseAuthorization();

            app.UseStaticFiles();

            app.UseMiddleware<TASagentTwitchBot.Core.Web.Middleware.AuthCheckerMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<TASagentTwitchBot.Core.Web.Hubs.OverlayHub>("/Hubs/Overlay");
                endpoints.MapHub<TASagentTwitchBot.Core.Web.Hubs.TTSMarqueeHub>("/Hubs/TTSMarquee");
                endpoints.MapHub<TASagentTwitchBot.Core.Web.Hubs.MonitorHub>("/Hubs/Monitor");
            });

            app.ApplicationServices.GetRequiredService<TASagentTwitchBot.Core.Config.IBotConfigContainer>().Initialize();
            app.ApplicationServices.GetRequiredService<TASagentTwitchBot.Core.View.IConsoleOutput>();
        }
    }
}
