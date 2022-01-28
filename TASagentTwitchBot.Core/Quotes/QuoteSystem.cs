using BGC.Collections.Generic;

using TASagentTwitchBot.Core.Database;
using TASagentTwitchBot.Core.Commands;

namespace TASagentTwitchBot.Core.Quotes;

public class QuoteSystem : ICommandContainer
{
    //Subsystems
    private readonly Config.BotConfiguration botConfig;
    private readonly ICommunication communication;

    //Data
    private readonly IServiceScopeFactory scopeFactory;

    private readonly DepletableBag<string> fakeNewsBag = new DepletableBag<string>()
        {
            "dubious, at best",
            "fake news",
            "profoundly unreliable",
            "wildly misrepresentative",
            "a gratuitous fiction",
            "a grotesque mockery of fact",
            "hilariously untrue",
            "fundamentally misleading",
            "absurdly manipulated",
            "simply incorrect",
            "entirely wrong"
        };

    private readonly Random randomizer;

    public QuoteSystem(
        Config.BotConfiguration botConfig,
        ICommunication communication,
        IServiceScopeFactory scopeFactory)
    {
        this.botConfig = botConfig;

        this.communication = communication;
        this.scopeFactory = scopeFactory;

        randomizer = new Random();

        fakeNewsBag.AutoRefill = true;
    }

    public void RegisterCommands(ICommandRegistrar commandRegistrar)
    {
        commandRegistrar.RegisterGlobalCommand("quote", QuoteCommandHandler);

        commandRegistrar.RegisterGlobalCommand("addquote", AddQuoteCommandHandler);
        commandRegistrar.RegisterScopedCommand("quote", "add", AddQuoteCommandHandler);
        commandRegistrar.RegisterScopedCommand("add", "quote", AddQuoteCommandHandler);

        commandRegistrar.RegisterGlobalCommand("removequote", RemoveQuoteCommandHandler);
        commandRegistrar.RegisterScopedCommand("quote", "remove", RemoveQuoteCommandHandler);
        commandRegistrar.RegisterScopedCommand("remove", "quote", RemoveQuoteCommandHandler);

        commandRegistrar.RegisterGlobalCommand("getquote", GetQuoteCommandHandler);
        commandRegistrar.RegisterScopedCommand("quote", "get", GetQuoteCommandHandler);
        commandRegistrar.RegisterScopedCommand("get", "quote", GetQuoteCommandHandler);

        commandRegistrar.RegisterScopedCommand("set", "quote", HandleQuoteSetRequest);
        commandRegistrar.RegisterScopedCommand("quote", "set", HandleQuoteSetRequest);

        commandRegistrar.RegisterHelpCommand("quote", QuoteHelpHandler);
    }

    public IEnumerable<string> GetPublicCommands()
    {
        yield return "quote";
    }

    private string QuoteHelpHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            return "Quote commands: !quote add <quote> [user], !quote, !quote <phrase>";
        }
        else if (remainingCommand.Length == 1 && remainingCommand[0].ToLowerInvariant() == "add")
        {
            return $"Quote Add syntax: !quote add \"Quote\" [optional username]. Quote Add example: !quote add \"Here is an example quote\" {botConfig.Broadcaster}";
        }
        else
        {
            return $"No quote subcommand found: {string.Join(' ', remainingCommand)}";
        }
    }

    private async Task QuoteCommandHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            //Get Random Quote
            await GetRandomQuote(chatter.User);
        }
        else if (remainingCommand[0].StartsWith('#') && remainingCommand[0].Length > 1 && int.TryParse(remainingCommand[0][1..], out int quoteId))
        {
            //Get a quote by ID
            await GetQuoteById(chatter, quoteId);
        }
        else
        {
            //Get a quote by search
            string searchString = string.Join(' ', remainingCommand).Trim();
            await QuoteSearch(chatter, searchString);
        }
    }

    private async Task HandleQuoteSetRequest(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Moderator)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, you are not authorized to manipulate quotes.");
            return;
        }

        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, no Quote setting specified.");
            return;
        }

        if (remainingCommand.Length > 1 && remainingCommand[0].StartsWith('#') && int.TryParse(remainingCommand[0][1..], out int quoteId))
        {
            //!set quote #<number> ...

            string quoteSetCommand = remainingCommand[1].ToLowerInvariant();

            if (quoteSetCommand == "fakenews")
            {
                await MarkFakeNews(
                    chatter: chatter,
                    quoteId: quoteId,
                    commandText: remainingCommand.Length > 2 ? string.Join(' ', remainingCommand[2..]).Trim() : null);

                return;
            }
            else if (quoteSetCommand == "realnews")
            {
                await MarkRealNews(
                    chatter: chatter,
                    quoteId: quoteId);

                return;
            }
        }

        communication.SendPublicChatMessage(
            $"@{chatter.User.TwitchUserName}, Quote setting not recognized ({string.Join(' ', remainingCommand)}).");
    }

    private Task GetQuoteCommandHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand is not null && remainingCommand.Length == 1)
        {
            string number = remainingCommand[1];

            if (number.Length >= 2 && number[0] == '#')
            {
                number = number[1..];
            }

            if (!int.TryParse(number, out int getQuoteId))
            {
                communication.SendPublicChatMessage(
                    $"@{chatter.User.TwitchUserName}, Malformed Quote Remove command.  Expected Form: !quote #5");
                communication.SendDebugMessage($"    Original Message: {chatter.Message}");

                return Task.CompletedTask;
            }

            return GetQuoteById(chatter, getQuoteId);
        }

        return GetRandomQuote(chatter.User);
    }

    private Task AddQuoteCommandHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand is null || remainingCommand.Length == 0)
        {
            //Malformed
            communication.SendPublicChatMessage(
                $"@{chatter.User.TwitchUserName}, Malformed Quote Add command.  Expected: !quote add \"Contents\" [optional username]");
            communication.SendDebugMessage($"    Original Message: {chatter.Message}");

            return Task.CompletedTask;
        }

        //Add a quote
        string addQuoteText = string.Join(' ', remainingCommand).Trim();
        return AddQuote(chatter, addQuoteText);
    }

    private Task RemoveQuoteCommandHandler(IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand is null || remainingCommand.Length != 1)
        {
            //Malformed
            communication.SendPublicChatMessage(
                $"@{chatter.User.TwitchUserName}, Malformed Quote Remove command.  Expected Form: !Quote Remove #5");
            communication.SendDebugMessage($"    Original Message: {chatter.Message}");

            return Task.CompletedTask;
        }

        string number = remainingCommand[0];

        if (number.Length >= 2 && number[0] == '#')
        {
            number = number[1..];
        }

        if (!int.TryParse(number, out int removeQuoteId))
        {
            communication.SendPublicChatMessage(
                $"@{chatter.User.TwitchUserName}, Malformed Quote Remove command.  Expected Form: !Quote Remove #5");
            communication.SendDebugMessage($"    Original Message: {chatter.Message}");

            return Task.CompletedTask;
        }

        //Try to remove a quote
        return RemoveQuote(chatter, removeQuoteId);
    }

    private async Task MarkFakeNews(
        IRC.TwitchChatter chatter,
        int quoteId,
        string? commandText)
    {
        //Only Admin can mark quotes as fake news
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Admin)
        {
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return;
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        //Find Quote
        Quote? matchingQuote = await db.Quotes.FindAsync(quoteId);

        if (matchingQuote is null)
        {
            communication.SendPublicChatMessage(
                $"@{chatter.User.TwitchUserName}, No quote #{quoteId} found.");
            communication.SendDebugMessage($"No Quote found with Id #{quoteId}");
            communication.SendDebugMessage($"    Original Message: {chatter.Message}");

            return;
        }

        if (!string.IsNullOrEmpty(commandText) && commandText.Length > 2 && commandText.StartsWith('"') && commandText.EndsWith('"'))
        {
            //Strip off quotes
            commandText = commandText[1..^1];
        }

        matchingQuote.IsFakeNews = true;
        matchingQuote.FakeNewsExplanation = commandText;

        await db.SaveChangesAsync();

        await db.Entry(matchingQuote).Reference(x => x.Creator).LoadAsync();
        SendQuote(matchingQuote, "UPDATED AS FAKE NEWS ");
    }

    private async Task MarkRealNews(
        IRC.TwitchChatter chatter,
        int quoteId)
    {
        //Only Admin can mark quotes as real news
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Admin)
        {
            communication.SendPublicChatMessage($"I'm afraid I can't let you do that, @{chatter.User.TwitchUserName}.");
            return;
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        //Find Quote
        Quote? matchingQuote = await db.Quotes.FindAsync(quoteId);

        if (matchingQuote is null)
        {
            communication.SendPublicChatMessage(
                $"@{chatter.User.TwitchUserName}, No quote #{quoteId} found.");
            communication.SendDebugMessage($"No Quote found with Id #{quoteId}");
            communication.SendDebugMessage($"    Original Message: {chatter.Message}");

            return;
        }

        matchingQuote.IsFakeNews = false;
        matchingQuote.FakeNewsExplanation = null;

        await db.SaveChangesAsync();

        await db.Entry(matchingQuote).Reference(x => x.Creator).LoadAsync();
        SendQuote(matchingQuote, "UPDATED AS REAL NEWS ");
    }

    private async Task RemoveQuote(IRC.TwitchChatter chatter, int quoteId)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        //Look Up Quote
        Quote? matchingQuote = await db.Quotes.FindAsync(quoteId);

        if (matchingQuote is null)
        {
            //Likely authorized
            communication.SendPublicChatMessage(
                $"@{chatter.User.TwitchUserName}, No quote #{quoteId} found.");
            communication.SendDebugMessage($"No Quote found with Id #{quoteId}");
            communication.SendDebugMessage($"    Original Message: {chatter.Message}");

            return;
        }

        await db.Entry(matchingQuote).Reference(x => x.Creator).LoadAsync();

        //Recent quotes were created in the last 15 minutes
        bool recentQuote = DateTime.Now - matchingQuote.CreateTime < new TimeSpan(hours: 0, minutes: 15, seconds: 0);

        //Admins are always authorized to delete quotes
        bool actionAuthorized = chatter.User.AuthorizationLevel >= AuthorizationLevel.Admin;

        //Mods are authorized if the creator is less than a mod and the quote is recent
        actionAuthorized |= recentQuote &&
            chatter.User.AuthorizationLevel >= AuthorizationLevel.Moderator &&
            matchingQuote.Creator.AuthorizationLevel < AuthorizationLevel.Moderator;

        //Quote creators are authorized if the quote is recent
        actionAuthorized |= recentQuote && chatter.User.UserId == matchingQuote.CreatorId;

        if (!actionAuthorized)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, you are not authorized to delete this quote.");
            return;
        }

        db.Quotes.Remove(matchingQuote);
        await db.SaveChangesAsync();

        communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, quote #{quoteId} has been expunged.");
    }

    private async Task GetRandomQuote(User requestingUser)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        if (!db.Quotes.Any())
        {
            communication.SendPublicChatMessage($"@{requestingUser.TwitchUserName} Sorry, no quotes can be displayed.");
            return;
        }

        int index = randomizer.Next(0, db.Quotes.Count());

        //Return a random Quote
        Quote randomQuote = db.Quotes.Skip(index).First();

        await db.Entry(randomQuote).Reference(x => x.Creator).LoadAsync();
        SendQuote(randomQuote);
    }

    private async Task AddQuote(IRC.TwitchChatter chatter, string quoteText)
    {
        //Adding
        string quote;
        string userName = botConfig.Broadcaster;

        if (quoteText.StartsWith('"') && quoteText[1..].Contains('"'))
        {
            int endQuoteIndex = quoteText.IndexOf('"', 1);

            if (!quoteText.StartsWith('"') || endQuoteIndex == -1)
            {
                //Malformed
                communication.SendPublicChatMessage(
                    $"@{chatter.User.TwitchUserName} Malformed Quote Add command.  Expected: !quote add \"Contents\" [optional username]");
                communication.SendDebugMessage($"Bad Command: {quoteText}");
                communication.SendDebugMessage($"    Original Message: {chatter.Message}");

                return;
            }

            quote = quoteText[1..endQuoteIndex];

            if (quoteText.Length > endQuoteIndex + 1)
            {
                //Parse Username
                quoteText = quoteText[(endQuoteIndex + 1)..].Trim();

                if (quoteText.Contains(' ') || quoteText.Contains('"'))
                {
                    communication.SendPublicChatMessage(
                        $"@{chatter.User.TwitchUserName} Malformed Quote Add command.  Expected: !quote add \"Contents\" [optional username]");
                    communication.SendDebugMessage($"Bad Username: {quoteText}");
                    communication.SendDebugMessage($"    Original Message: {chatter.Message}");

                    return;
                }

                if (quoteText.StartsWith('-') || quoteText.StartsWith('@'))
                {
                    //Strip off any optional - or @
                    quoteText = quoteText[1..];
                }

                if (quoteText.Length > 0)
                {
                    userName = quoteText;
                }
            }
        }
        else
        {
            quote = quoteText;
        }

        Quote newQuote = new Quote()
        {
            QuoteText = quote,
            Speaker = userName,
            CreatorId = chatter.User.UserId,
            CreateTime = DateTime.Now
        };


        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        db.Quotes.Add(newQuote);
        await db.SaveChangesAsync();

        await db.Entry(newQuote).Reference(x => x.Creator).LoadAsync();
        SendQuote(newQuote, "ADDED ");
    }

    private async Task GetQuoteById(IRC.TwitchChatter chatter, int id)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        //Look Up Quote
        Quote? matchingQuote = await db.Quotes.FindAsync(id);

        if (matchingQuote is null)
        {
            communication.SendPublicChatMessage(
                $"@{chatter.User.TwitchUserName}, No quote #{id} found.");
            communication.SendDebugMessage($"No Quote found with Id #{id}");
            communication.SendDebugMessage($"    Original Message: {chatter.Message}");

            return;
        }

        await db.Entry(matchingQuote).Reference(x => x.Creator).LoadAsync();
        SendQuote(matchingQuote);
    }

    private async Task QuoteSearch(IRC.TwitchChatter chatter, string quoteText)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        //Clear quotation marks
        quoteText = quoteText.Replace("\"", "").ToLowerInvariant();

        IEnumerable<Quote> matchingQuotes = db.Quotes.Where(x => x.QuoteText.ToLower().Contains(quoteText));
        int matchingQuoteCount = matchingQuotes.Count();

        if (matchingQuoteCount == 0)
        {
            communication.SendPublicChatMessage(
                $"@{chatter.User.TwitchUserName}, No quote found for search \"{quoteText}\"");
            communication.SendDebugMessage($"No Quote found: {quoteText}");
            communication.SendDebugMessage($"    Original Message: {chatter.Message}");

            return;
        }

        int returnIndex = randomizer.Next(0, matchingQuoteCount);

        Quote selectedQuote = matchingQuotes.Skip(returnIndex).First();

        await db.Entry(selectedQuote).Reference(x => x.Creator).LoadAsync();
        SendQuote(selectedQuote);
    }

    private void SendQuote(Quote data, string prefix = "")
    {
        string outputQuoteText =
            $"{prefix}" +
            $"Quote {data.QuoteId}: \"{data.QuoteText}\" - {data.Speaker} " +
            $"({data.CreateTime:MMMM d, yyyy}) " +
            $"<Quotht By @{data.Creator.TwitchUserName}>";

        if (data.IsFakeNews)
        {
            //Fake News
            if (string.IsNullOrWhiteSpace(data.FakeNewsExplanation))
            {
                //No Explanation
                outputQuoteText += $" ❗[{botConfig.Broadcaster} has labeled this quote as {fakeNewsBag.PopNext()}]";
            }
            else
            {
                //Include Explanation
                outputQuoteText += $" ❗[{botConfig.Broadcaster} has labeled this quote as {fakeNewsBag.PopNext()}, stating \"{data.FakeNewsExplanation}\"]";
            }
        }

        communication.SendPublicChatMessage(outputQuoteText);
    }
}
