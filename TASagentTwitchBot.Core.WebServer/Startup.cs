using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

using TASagentTwitchBot.Core.WebServer.Database;
using TASagentTwitchBot.Core.WebServer.Models;
using TASagentTwitchBot.Core.WebServer.Tokens;

namespace TASagentTwitchBot.Core.WebServer;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }
    public void ConfigureServices(IServiceCollection services)
    {
        //Initialize DataManagement
        BGC.IO.DataManagement.Initialize("TASagentBotWebServer");

        Config.WebServerConfig configFile = Config.WebServerConfig.GetConfig();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configFile.DBConnectionString));

        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultUI()
            .AddDefaultTokenProviders();
        services.AddControllersWithViews();
        services.AddRazorPages();

        services.AddSignalR();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddScheme<TokenAuthenticationOptions, TokenAuthenticationHandler>(TokenAuthenticationHandler.SCHEME_NAME, o => { })
            .AddCookie()
            .AddTwitch(options =>
            {
                options.ClientId = configFile.TwitchClientId;
                options.ClientSecret = configFile.TwitchClientSecret;

                options.SaveTokens = true;

                options.Scope.Add("bits:read");
                options.Scope.Add("channel:read:subscriptions");
                options.Scope.Add("channel:read:redemptions");
                options.Scope.Add("channel:read:polls");
                options.Scope.Add("channel:read:predictions");
                options.Scope.Add("channel:read:hype_train");
                options.Scope.Add("channel:read:goals");
            });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownProxies.Add(IPAddress.Parse("192.168.254.254"));

        });

        services.AddSingleton(configFile);

        services
            .AddSingleton<ApplicationManagement>()
            .AddSingleton<ICommunication, WebServerCommunicationHandler>()
            .AddSingleton<EventSub.IServerEventSubHandler, EventSub.ServerEventSubHandler>();

        services
            .AddSingleton<API.Twitch.HelixServerHelper>()
            .AddSingleton<API.Twitch.HelixEventSubHelper>()
            .AddSingleton<API.Twitch.AppAccessTokenManager>();

        services.AddSingleton<TTS.IServerTTSRenderer, TTS.ServerTTSRenderer>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseForwardedHeaders();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        UseCoreLibraryAssets(app, env);

        app.UseRouting();

        app.UseMiddleware<Web.Middleware.RewindRequiredMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseWebSockets();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            endpoints.MapRazorPages();

            endpoints.MapHub<Web.Hubs.BotEventSubHub>("/Hubs/BotEventSubHub");
            endpoints.MapHub<Web.Hubs.BotTTSHub>("/Hubs/BotTTSHub");
        });
    }

    private void UseCoreLibraryAssets(
        IApplicationBuilder app,
        IWebHostEnvironment env)
    {
        string wwwRootPath;

        if (env.IsDevelopment())
        {
            //Navigate relative to the current path in Development
            wwwRootPath = Path.Combine(
                Directory.GetParent(env.ContentRootPath)!.FullName,
                "TASagentTwitchBot.Core",
                "wwwroot",
                "Assets");
        }
        else
        {
            //Look in published "_content" directory
            wwwRootPath = Path.Combine(env.WebRootPath, "_content", "TASagentTwitchBot.Core", "Assets");
        }

        PhysicalFileProvider fileProvider = new PhysicalFileProvider(wwwRootPath);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            RequestPath = "/Assets"
        });
    }
}
