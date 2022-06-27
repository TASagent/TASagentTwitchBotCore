using TASagentTwitchBot.Core.Web;

namespace TASagentTwitchBot.Plugin.Quotes.Web;

public static class QuotesExtensions
{
    public static IMvcBuilder AddQuotesAssembly(this IMvcBuilder builder) =>
        builder.AddApplicationPart(typeof(QuotesExtensions).Assembly);

    public static IServiceCollection RegisterQuotesServices(this IServiceCollection services) =>
        services.AddTASSingleton<QuoteSystem>();
}
