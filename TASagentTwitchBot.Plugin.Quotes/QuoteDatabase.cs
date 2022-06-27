using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TASagentTwitchBot.Core.Database;

namespace TASagentTwitchBot.Plugin.Quotes;

//Create the database 
[Core.AutoRegister]
public interface IQuoteDatabaseContext
{
    DbSet<Quote> Quotes { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}

public class Quote
{
    public int QuoteId { get; set; }

    public string QuoteText { get; set; } = null!;
    public string Speaker { get; set; } = null!;
    public DateTime CreateTime { get; set; }

    public int CreatorId { get; set; }
    public User Creator { get; set; } = null!;

    public bool IsFakeNews { get; set; }
    public string? FakeNewsExplanation { get; set; }
}
