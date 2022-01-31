using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TASagentTwitchBot.Plugin.Quotes.Web;

[ApiController]
[Route("/TASagentBotAPI/[controller]")]
public class QuotesController : ControllerBase
{
    private readonly IQuoteDatabaseContext db;

    public QuotesController(IQuoteDatabaseContext db)
    {
        this.db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuoteDTO>>> GetQuotes()
    {
        return await db.Quotes
            .Include(x => x.Creator)
            .Select(x => QuoteToDTO(x))
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<QuoteDTO>> GetQuote(
        int id)
    {
        Quote? quote = await db.Quotes.FindAsync(id);

        if (quote is null)
        {
            return NotFound();
        }

        return QuoteToDTO(quote);
    }

    private static QuoteDTO QuoteToDTO(Quote quote) =>
        new QuoteDTO(
            quote.QuoteId,
            quote.QuoteText,
            quote.Speaker,
            quote.Creator.TwitchUserName,
            quote.CreateTime,
            quote.IsFakeNews);
}

public record QuoteDTO(int Id, string QuoteText, string Speaker, string Creator, DateTime CreateTime, bool FakeNews);
