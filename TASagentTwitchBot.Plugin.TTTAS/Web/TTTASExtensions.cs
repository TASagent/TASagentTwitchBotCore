namespace TASagentTwitchBot.Plugin.TTTAS.Web;

public static class TTTASExtensions
{
    public static IMvcBuilder AddTTTASAssembly(this IMvcBuilder builder)
    {
        return builder.AddApplicationPart(typeof(TTTASExtensions).Assembly);
    }

    public static IServiceCollection RegisterTTTASServices(this IServiceCollection services)
    {
        //Construct or load TTTASConfiguration
        services.AddSingleton<TTTASConfiguration>(TTTASConfiguration.GetConfig());

        return services
            .AddSingleton<ITTTASProvider, TTTASProvider>()
            .AddSingleton<ITTTASRenderer, TTTASRenderer>()
            .AddSingleton<ITTTASHandler, TTTASHandler>()
            .AddSingleton<Core.Commands.ICommandContainer, TTTASCommandSystem>()
            .AddSingleton<Core.PubSub.IRedemptionContainer, TTTASRedemptionHandler>();
    }

    public static IEndpointRouteBuilder RegisterTTTASEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<Hubs.TTTASHub>("/Hubs/TTTAS");

        return endpoints;
    }
}
