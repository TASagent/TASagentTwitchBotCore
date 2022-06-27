using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace TASagentTwitchBot.Core.Web;

public static partial class ServiceExtensions
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

    /// <summary>
    /// Registers <typeparamref name="TService"/> and adds automatic redirects for several optional interfaces
    /// </summary>
    public static IServiceCollection AddTASSingleton<TService>(this IServiceCollection services) where TService : class
    {
        services.AddSingleton<TService>();

        Type serviceType = typeof(TService);

        foreach (Type serviceInterface in serviceType.GetBaseTypesAndInterfaces())
        {
            if (serviceInterface.GetCustomAttribute<AutoRegisterAttribute>() is not null)
            {
                services.AddSingleton(serviceInterface, x => x.GetRequiredService<TService>());
            }
        }

        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TService"/> and adds automatic redirects for several optional interfaces
    /// </summary>
    public static IServiceCollection AddTASSingleton<TService>(this IServiceCollection services, TService implementationInstance) where TService : class
    {
        services.AddSingleton<TService>(implementationInstance);

        Type serviceType = typeof(TService);

        foreach (Type serviceInterface in serviceType.GetBaseTypesAndInterfaces())
        {
            if (serviceInterface.GetCustomAttribute<AutoRegisterAttribute>() is not null)
            {
                services.AddSingleton(serviceInterface, implementationInstance);
            }
        }

        return services;
    }

    public static IServiceCollection AddTASDbContext<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? optionsAction = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(optionsAction, contextLifetime, optionsLifetime);

        Type databaseType = typeof(TContext);

        foreach (Type serviceInterface in databaseType.GetBaseTypesAndInterfaces())
        {
            if (serviceInterface.GetCustomAttribute<AutoRegisterAttribute>() is not null)
            {
                services.AddScoped(serviceInterface, x => x.GetRequiredService<TContext>());
            }
        }

        return services;
    }


    private static IEnumerable<Type> GetBaseTypesAndInterfaces(this Type type)
    {
        if (type.BaseType is null || type.BaseType == typeof(object))
        {
            return type.GetInterfaces();
        }

        return type.BaseType.GetBaseTypesAndInterfaces()
            .Prepend(type.BaseType)
            .Concat(type.GetInterfaces())
            .Distinct();
    }

    /// <summary>
    /// Registers <typeparamref name="TService"/> to fetch the existing registered scoped <typeparamref name="TImplementation"/> service class.
    /// </summary>
    public static IServiceCollection AddScopedRedirect<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService =>
        services.AddScoped<TService>(x => x.GetRequiredService<TImplementation>());

    /// <summary>
    /// Registers <typeparamref name="TService"/> to fetch the existing registered singleton <typeparamref name="TImplementation"/> service class.
    /// </summary>
    public static IServiceCollection AddSingletonRedirect<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService =>
        services.AddSingleton<TService>(x => x.GetRequiredService<TImplementation>());

}
