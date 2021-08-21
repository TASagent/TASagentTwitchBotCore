using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TASagentTwitchBot.Core.Chat;

namespace TASagentTwitchBot.Core.Database
{
    //Create the database 
    public abstract class BaseDatabaseContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Quote> Quotes { get; set; }
        public DbSet<CustomTextCommand> CustomTextCommands { get; set; }
        public DbSet<BanRule> BanRules { get; set; }
        public DbSet<BannedUser> BannedUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseSqlite($"Data Source={BGC.IO.DataManagement.PathForDataFile("Config", "data.sqlite")}");
    }

    public class User
    {
        public int UserId { get; set; }

        public string TwitchUserName { get; set; }
        public string TwitchUserId { get; set; }

        public DateTime? FirstSeen { get; set; }
        public DateTime? FirstFollowed { get; set; }

        public TTS.TTSVoice TTSVoicePreference { get; set; }
        public TTS.TTSPitch TTSPitchPreference { get; set; }
        public TTS.TTSSpeed TTSSpeedPreference { get; set; }
        public string TTSEffectsChain { get; set; }
        public DateTime? LastSuccessfulTTS { get; set; }

        public Commands.AuthorizationLevel AuthorizationLevel { get; set; }
        public List<Quote> QuotesCreated { get; } = new List<Quote>();

        public string Color { get; set; }
    }

    public class CustomTextCommand
    {
        public int CustomTextCommandId { get; set; }

        public string Command { get; set; }
        public string Text { get; set; }
        public bool Enabled { get; set; }
    }

    public class BanRule
    {
        public int BanRuleId { get; set; }

        public string RegexRule { get; set; }

        public int TimeoutSeconds { get; set; }
        public int TimeoutCooldown { get; set; }

        public TextContentType TextContentType { get; set; }
        public bool ShowMessage { get; set; }

        public bool UseTimeout { get; set; }
    }

    public class BannedUser
    {
        public int BannedUserId { get; set; }

        public int RuleId { get; set; }
        public BanRule Rule { get; set; }
        public DateTime BannedOn { get; set; }
        public string Username { get; set; }
    }


    public class Quote
    {
        public int QuoteId { get; set; }

        public string QuoteText { get; set; }
        public string Speaker { get; set; }
        public DateTime CreateTime { get; set; }

        public int CreatorId { get; set; }
        public User Creator { get; set; }

        public bool IsFakeNews { get; set; }
        public string FakeNewsExplanation { get; set; }
    }
}
