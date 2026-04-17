using TASagentTwitchBot.Core.Web;

namespace TASagentTwitchBot.Plugin.Vestaboard.Web;

public static class VestaboardExtensions
{
    //public static IMvcBuilder AddVestaboardAssembly(this IMvcBuilder builder) =>
    //    builder.AddApplicationPart(typeof(VestaboardExtensions).Assembly);

    public static IServiceCollection RegisterVestaboardServices(this IServiceCollection services)
    {
        //Construct or load TTTASConfiguration
        return services.AddTASSingleton<VestaboardConfiguration>(VestaboardConfiguration.GetConfig())
            .AddTASSingleton<VestaboardManager>()
            .AddTASSingleton<VestaboardCommandSystem>()
            //.AddTASSingleton<VestaboardRedemptionHandler>()
            ;
    }
}
