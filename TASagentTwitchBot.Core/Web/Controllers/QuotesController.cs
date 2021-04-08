using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TASagentTwitchBot.Core.Web.Controllers
{
    [ApiController]
    [Route("/TASagentBotAPI/[controller]")]
    public class QuotesController : ControllerBase
    {
        private readonly Database.BaseDatabaseContext db;

        public QuotesController(
            Database.BaseDatabaseContext db)
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
        public async Task<ActionResult<QuoteDTO>> GetQuote(int id)
        {
            Database.Quote quote = await db.Quotes.FindAsync(id);

            if (quote is null)
            {
                return NotFound();
            }

            return QuoteToDTO(quote);
        }

        private static QuoteDTO QuoteToDTO(Database.Quote quote) =>
            new QuoteDTO(
                quote.QuoteId,
                quote.QuoteText,
                quote.Speaker,
                quote.Creator.TwitchUserName,
                quote.CreateTime);
    }

    public record QuoteDTO(int Id, string QuoteText, string Speaker, string Creator, DateTime CreateTime);
}
