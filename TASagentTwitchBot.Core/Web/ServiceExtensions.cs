namespace TASagentTwitchBot.Core.Web;

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

    /// <summary>
    /// Registers <typeparamref name="TService"/> and adds automatic redirects for several optional interfaces
    /// </summary>
    public static IServiceCollection AddTASSingleton<TService>(this IServiceCollection services) where TService : class
    {
        services.AddSingleton<TService>();

        Type serviceType = typeof(TService);

        if (serviceType.GetInterfaces().Contains(typeof(Scripting.IScriptedComponent)))
        {
            services.AddSingleton<Scripting.IScriptedComponent>(x => (Scripting.IScriptedComponent)x.GetRequiredService<TService>());
        }

        if (serviceType.GetInterfaces().Contains(typeof(Commands.ICommandContainer)))
        {
            services.AddSingleton<Commands.ICommandContainer>(x => (Commands.ICommandContainer)x.GetRequiredService<TService>());
        }

        return services;
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
