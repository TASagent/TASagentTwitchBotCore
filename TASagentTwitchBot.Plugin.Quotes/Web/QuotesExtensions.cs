namespace TASagentTwitchBot.Plugin.Quotes.Web;

public static class QuotesExtensions
{
    public static IMvcBuilder AddQuotesAssembly(this IMvcBuilder builder)
    {
        return builder.AddApplicationPart(typeof(QuotesExtensions).Assembly);
    }

    public static IServiceCollection RegisterQuotesServices(this IServiceCollection services)
    {
        return services.AddSingleton<Core.Commands.ICommandContainer, QuoteSystem>();
    }

    public static IServiceCollection RegisterQuoteDatabase<TDatabaseType>(this IServiceCollection services)
        where TDatabaseType : Microsoft.EntityFrameworkCore.DbContext, IQuoteDatabaseContext
    {
        return services.AddScoped<IQuoteDatabaseContext>(x => x.GetRequiredService<TDatabaseType>());
    }
}
