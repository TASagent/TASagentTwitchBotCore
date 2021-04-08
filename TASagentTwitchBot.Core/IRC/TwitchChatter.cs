using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using TASagentTwitchBot.Core.Database;

namespace TASagentTwitchBot.Core.IRC
{
    public record TwitchChatter
    {
        public User User { get; init; }
        public DateTime? CreatedAt { get; init; }
        public string Badges { get; init; }
        public string Message { get; init; }
        public string MessageId { get; init; }
        public bool Whisper { get; init; }
        public int Bits { get; init; }

        public static async Task<TwitchChatter> FromIRCMessage(
            IRCMessage message,
            ICommunication communication,
            BaseDatabaseContext db)
        {
            if (message.ircCommand != IRCCommand.PrivMsg && message.ircCommand != IRCCommand.Whisper)
            {
                return null;
            }

            User user = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserId == message.tags["user-id"]);

            string displayName = message.tags["display-name"];

            if (string.IsNullOrWhiteSpace(displayName) || displayName == "1")
            {
                displayName = message.user;
            }

            string color = message.tags.ContainsKey("color") ? message.tags["color"] : null;

            if (user == null)
            {
                Commands.AuthorizationLevel authLevel = Commands.AuthorizationLevel.None;

                if (message.tags["badges"].Contains("broadcaster"))
                {
                    authLevel = Commands.AuthorizationLevel.Admin;
                }
                else if (message.tags["mod"] == "1")
                {
                    authLevel = Commands.AuthorizationLevel.Moderator;
                }

                user = new User()
                {
                    TwitchUserId = message.tags["user-id"],
                    TwitchUserName = displayName,
                    AuthorizationLevel = authLevel,
                    FirstSeen = DateTime.Now,
                    Color = color
                };

                await db.Users.AddAsync(user);
                await db.SaveChangesAsync();
            }
            else
            {
                //User Found
                bool saveChanges = false;

                if (!user.FirstSeen.HasValue)
                {
                    user.FirstSeen = DateTime.Now;
                    saveChanges = true;
                }

                if (displayName != user.TwitchUserName)
                {
                    communication.SendDebugMessage($"Updating username from {user.TwitchUserName} to {displayName}");
                    user.TwitchUserName = displayName;
                    saveChanges = true;
                }

                if (color != user.Color)
                {
                    user.Color = color;
                    saveChanges = true;
                }

                if (saveChanges)
                {
                    await db.SaveChangesAsync();
                }
            }

            int bits = message.tags.ContainsKey("bits") ? int.Parse(message.tags["bits"]) : 0;

            if (message.ircCommand == IRCCommand.Whisper)
            {
                return new TwitchChatter()
                {
                    User = user,
                    CreatedAt = DateTime.Now,
                    Badges = message.tags["badges"],
                    Message = message.message,
                    MessageId = null,
                    Whisper = true,
                    Bits = bits
                };
            }
            else
            {
                return new TwitchChatter()
                {
                    User = user,
                    CreatedAt = DateTime.Now,
                    Badges = message.tags["badges"],
                    Message = message.message,
                    MessageId = message.tags["id"],
                    Whisper = false,
                    Bits = bits
                };
            }
        }

        public string ToLogString() => Whisper ? $"[{CreatedAt:G}] {User.TwitchUserName} WHISPER: {Message}" : $"[{CreatedAt:G}] {User.TwitchUserName}: {Message}";
    }
}
