using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;


using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Web;
using TASagentTwitchBot.Core.WebServer;
using TASagentTwitchBot.Core.WebServer.Config;
using TASagentTwitchBot.Core.WebServer.Database;
using TASagentTwitchBot.Core.WebServer.EventSub;
using TASagentTwitchBot.Core.WebServer.Models;
using TASagentTwitchBot.Core.WebServer.Tokens;
using TASagentTwitchBot.Core.WebServer.API.Twitch;
using TASagentTwitchBot.Core.WebServer.TTS;
using TASagentTwitchBot.Core.WebServer.Web.Middleware;
using TASagentTwitchBot.Core.WebServer.Web.Hubs;
using TASagentTwitchBot.Core.WebServer.Web;

//Initialize DataManagement
BGC.IO.DataManagement.Initialize("TASagentBotWebServer");

WebServerConfig configFile = WebServerConfig.GetConfig();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .UseUrls("http://0.0.0.0:5003");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configFile.DBConnectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddSignalR();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
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

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Add(IPAddress.Parse("192.168.254.254"));

});

builder.Services.AddTASSingleton(configFile);

builder.Services
    .AddTASSingleton<WebServerCommunicationHandler>()
    .AddTASSingleton<ServerEventSubHandler>();

builder.Services
    .AddTASSingleton<HelixServerHelper>()
    .AddTASSingleton<HelixEventSubHelper>()
    .AddTASSingleton<AppAccessTokenManager>();

builder.Services
    .AddTASSingleton<ServerTTSRenderer>()
    .AddTASSingleton<TASagentTwitchBot.Plugin.TTS.AmazonTTS.AmazonTTSLocalSystem>()
    .AddTASSingleton<TASagentTwitchBot.Plugin.TTS.AzureTTS.AzureTTSLocalSystem>()
    .AddTASSingleton<TASagentTwitchBot.Plugin.TTS.GoogleTTS.GoogleTTSLocalSystem>();

builder.Services
    .AddTASSingleton<DataForwardingConnectionManager>()
    .AddTASSingleton<ServerDataForwardingHandler>();

using WebApplication app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
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

UseCoreLibraryAssets(app);

app.UseRouting();

app.UseMiddleware<RewindRequiredMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.MapHub<BotEventSubHub>("/Hubs/BotEventSubHub");
app.MapHub<BotTTSHub>("/Hubs/BotTTSHub");
app.MapHub<BotDataForwardingHub>("/Hubs/BotDataForwardingHub");

//Prepare Database
using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider services = scope.ServiceProvider;
    ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();
    try
    {
        ApplicationDbContext context = services.GetRequiredService<ApplicationDbContext>();
        UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await ContextSeed.SeedRolesAsync(roleManager);
        await ContextSeed.SeedSuperAdminAsync(userManager);
    }
    catch (Exception ex)
    {
        ILogger<Program> logger = loggerFactory.CreateLogger<Program>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

//Run
app.Run();


static void UseCoreLibraryAssets(WebApplication app)
{
    string wwwRootPath;

    if (app.Environment.IsDevelopment())
    {
//#warning DOTNET CORE 6 FIX
        //Behavior of Directory.GetParent(x) seems to have changed in DotNetCore 6.0.
        //Now Directory.GetParent("/path/to/dir/") returns "/path/to/dir" when it used to return "/path/to"
        string path = Directory.GetParent(app.Environment.ContentRootPath)!.FullName;
        //path = Directory.GetParent(path)!.FullName;

        //Navigate relative to the current path in Development
        wwwRootPath = Path.Combine(
            path,
            "TASagentTwitchBot.Core",
            "wwwroot",
            "Assets");
    }
    else
    {
        //Look in published "_content" directory
        wwwRootPath = Path.Combine(app.Environment.WebRootPath, "_content", "TASagentTwitchBot.Core", "Assets");
    }

    PhysicalFileProvider fileProvider = new PhysicalFileProvider(wwwRootPath);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider,
        RequestPath = "/Assets"
    });
}
