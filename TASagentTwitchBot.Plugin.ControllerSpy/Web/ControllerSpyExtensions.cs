using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TASagentTwitchBot.Plugin.ControllerSpy.Web
{
    public static class ControllerSpyExtensions
    {
        public static IMvcBuilder AddControllerSpyControllerAssembly(this IMvcBuilder builder)
        {
            return builder.AddApplicationPart(typeof(ControllerSpyExtensions).Assembly);
        }

        public static IServiceCollection RegisterControllerSpyServices(this IServiceCollection services)
        {
            return services.AddSingleton<IControllerManager, ControllerManager>();
        }

        public static IEndpointRouteBuilder RegisterControllerSpyEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<Hubs.ControllerSpyHub>("/Hubs/ControllerSpy");

            return endpoints;
        }
    }
}
