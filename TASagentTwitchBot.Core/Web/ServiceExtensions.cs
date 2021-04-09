using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace TASagentTwitchBot.Core.Web
{
    public static class ServiceExtensions
    {
        public static IServiceCollection UnregisterInterfaceAll<T>(this IServiceCollection services)
        {
            foreach (ServiceDescriptor descriptor in services.Where(x => x.ServiceType == typeof(T)).ToList())
            {
                services.Remove(descriptor);
            }

            return services;
        }

        public static IServiceCollection UnregisterImplementation<T>(this IServiceCollection services)
        {
            foreach (ServiceDescriptor descriptor in services.Where(x => x.ImplementationType == typeof(T)).ToList())
            {
                services.Remove(descriptor);
            }

            return services;
        }

        public static IMvcBuilder GetMvcBuilder(this IServiceCollection services) =>
            services.AddMvc(x => x.EnableEndpointRouting = false);

        public static IMvcBuilder RegisterControllersWithoutFeatures(
            this IServiceCollection services,
            params string[] disabledFeatures) =>
            services.GetMvcBuilder()
                .ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new ConditionalControllerFeatureProvider(disabledFeatures)));

        public static IMvcBuilder RegisterControllersWithoutFeatures(
            this IMvcBuilder builder,
            params string[] disabledFeatures) =>
            builder.ConfigureApplicationPartManager(
                x => x.FeatureProviders.Add(new ConditionalControllerFeatureProvider(disabledFeatures)));

        public static IApplicationBuilder MapCustomizedControllers(
            this IApplicationBuilder app)
        {
            return app.UseMvc(x => x.MapRoute("default", "{controller}/{action=Index}"));
        }
    }
}
