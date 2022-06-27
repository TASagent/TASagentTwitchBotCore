using TASagentTwitchBot.Core.Web;

namespace TASagentTwitchBot.Plugin.TTTAS.Web;

public static class TTTASExtensions
{
    public static IMvcBuilder AddTTTASAssembly(this IMvcBuilder builder) =>
        builder.AddApplicationPart(typeof(TTTASExtensions).Assembly);

    public static IServiceCollection RegisterTTTASServices(this IServiceCollection services)
    {
        //Construct or load TTTASConfiguration
        return services.AddTASSingleton<TTTASConfiguration>(TTTASConfiguration.GetConfig())
            .AddTASSingleton<TTTASProvider>()
            .AddTASSingleton<TTTASRenderer>()
            .AddTASSingleton<TTTASHandler>()
            .AddTASSingleton<TTTASCommandSystem>()
            .AddTASSingleton<TTTASRedemptionHandler>();
    }

    public static IEndpointRouteBuilder RegisterTTTASEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<Hubs.TTTASHub>("/Hubs/TTTAS");
        return endpoints;
    }
}
