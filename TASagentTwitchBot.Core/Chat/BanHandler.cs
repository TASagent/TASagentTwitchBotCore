using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BGC.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using TASagentTwitchBot.Core.Database;
using TASagentTwitchBot.Core.IRC;

namespace TASagentTwitchBot.Core.Chat
{

    public static class BanHelpers
    {
        public static BanRuleDTO RuleToDTO(Database.BanRule banRule) =>
        new BanRuleDTO(
             banRule.RegexRule,
                banRule.TextContentType,
                banRule.TimeoutSeconds,
                banRule.TimeoutCooldown,
                banRule.ShowMessage,
                banRule.UseTimeout,
                banRule.BanRuleId
         );


        public record BanRuleDTO(string regex, TextContentType textContentType, int timeoutSeconds, int timeoutCooldown, bool showMessage, bool useTimeout, int id);
    }

    public interface IBanHandler
    {
        Task HandlePossibleUserBan(TwitchChatter message);
        bool CheckOrUpdateBanRulesExist();
        void UpdateBanRules();
        Task RemoveBanRule(BanHelpers.BanRuleDTO banRule);
        Task<BanHelpers.BanRuleDTO> AddBanRule(BanHelpers.BanRuleDTO banRule);

        List<BanRule> GetBanRules();
    }

    public enum TextContentType
    {
        Username,
        ChatMessage,
        PrivateMessage,
    }

    public class BanHandler : IBanHandler
    {

        public List<BanRule> banRules { get; private set; }
        private readonly ICommunication communication;
        private readonly IServiceScopeFactory scopeFactory;
        private DateTime lastCheckOfBanRules;
        private readonly DepletableBag<string> banMessages = new DepletableBag<string>()
        {
            "another mouse squashed",
            "winner winner chicken dinner",
            "finished them",
            "another one bites the dust",
            "gatutitous violence is overrated",
            "at least one more name won't go down in history",
            "thanks for keeping bots employed",
            "teehee",
            "fasted bot in the west",
            "*buzz* *swat* *swat*, got it",
            "please come again"
        };
        public BanHandler(ICommunication communication, IServiceScopeFactory scopeFactory)
        {

            this.communication = communication;

            this.scopeFactory = scopeFactory;
            UpdateBanRules();
            banMessages.AutoRefill = true;

        }


        public bool CheckOrUpdateBanRulesExist()
        {
            var minsSincecheck = DateTime.UtcNow - lastCheckOfBanRules;
            // update ban rules if it's been at least 2 minutes since last update
            if (minsSincecheck.TotalMinutes > 2)
            {
                UpdateBanRules();
            }
            return banRules.Count > 0;
        }

        public void UpdateBanRules()
        {
            using IServiceScope scope = scopeFactory.CreateScope();

            BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();
            banRules = db.BanRules.AsEnumerable().ToList();
            lastCheckOfBanRules = DateTime.UtcNow;

        }

        public async Task RemoveBanRule(BanHelpers.BanRuleDTO banRule)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();
            var rule = db.Find<BanRule>(banRule.id);
            if (rule != null)
            {
                db.Remove<BanRule>(rule);
                await db.SaveChangesAsync();
            }
            UpdateBanRules();
        }

        public DateTime? GetLastTimeoutOfUser(int banRuleId, string username)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();
            var last = db.BannedUsers.Select(c => c)
                .Where(x => x.RuleId == banRuleId)
                .Where(x => x.Username == username.ToLower())
                .OrderByDescending(x => x.BannedOn).First();
            if (last != null)
                return last.BannedOn;
            return null;
        }

        public async Task<BanHelpers.BanRuleDTO> AddBanRule(BanHelpers.BanRuleDTO banRule)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();
            var bR = new BanRule
            {
                RegexRule = banRule.regex,
                TimeoutSeconds = banRule.timeoutSeconds,
                TimeoutCooldown = banRule.timeoutCooldown,
                TextContentType = banRule.textContentType,
                ShowMessage = banRule.showMessage,
                UseTimeout = banRule.useTimeout,
            };
            db.Add<BanRule>(bR);
            await db.SaveChangesAsync();
            UpdateBanRules();
            return BanHelpers.RuleToDTO(bR);
        }

        public List<BanRule> GetBanRules()
        {
            return banRules;
        }

        public virtual async Task HandlePossibleUserBan(TwitchChatter chatter)
        {
            var ban = false;
            BanRule rule = null;

            foreach (var r in banRules)
            {
                string text = string.Empty;
                if (r.TextContentType == TextContentType.Username)
                    text = chatter.User.TwitchUserName.ToLower();
                else
                    text = chatter.Message;
                var m = Regex.Match(text, r.RegexRule);

                if (m.Success)
                {
                    ban = true;
                    rule = r;
                    break;
                }
            }
            if (!ban) return;

            using IServiceScope scope = scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();
            var user = new BannedUser
            {
                // normalize to lowercase for easier querying
                Username = chatter.User.TwitchUserName.ToLower(),
                RuleId = rule.BanRuleId,
                BannedOn = DateTime.UtcNow
            };

            await db.AddAsync(user);

            await db.SaveChangesAsync();
            if (rule.UseTimeout)
            {
                var last = GetLastTimeoutOfUser(rule.BanRuleId, chatter.User.TwitchUserName);
                double secondsDiff = rule.TimeoutCooldown + 1;
                if (last != null)
                {
                    var now = DateTime.UtcNow;
                    secondsDiff = (now - (DateTime)last).TotalSeconds;
                }
                if (secondsDiff <= rule.TimeoutCooldown && rule.TimeoutCooldown != -1) return;
                communication.SendPublicChatMessage(string.Format("/timeout {0} {1}", chatter.User.TwitchUserName, rule.TimeoutSeconds));

            }

            else
                communication.SendPublicChatMessage(string.Format("/ban {0}", chatter.User.TwitchUserName));

            if (rule.ShowMessage)
                communication.SendPublicChatMessage($"{banMessages.PopNext()}");

        }

    }

}
