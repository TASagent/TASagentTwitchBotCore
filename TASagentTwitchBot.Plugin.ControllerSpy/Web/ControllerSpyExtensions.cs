using TASagentTwitchBot.Core.Web;

namespace TASagentTwitchBot.Plugin.ControllerSpy.Web;

public static class ControllerSpyExtensions
{
    public static IMvcBuilder AddControllerSpyControllerAssembly(this IMvcBuilder builder) =>
        builder.AddApplicationPart(typeof(ControllerSpyExtensions).Assembly);

    public static IServiceCollection RegisterControllerSpyServices(this IServiceCollection services) =>
        services.AddTASSingleton<ControllerManager>();

    public static IEndpointRouteBuilder RegisterControllerSpyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<Hubs.ControllerSpyHub>("/Hubs/ControllerSpy");
        return endpoints;
    }
}
