﻿using Microsoft.EntityFrameworkCore;

using TASagentTwitchBot.Core.Database;

namespace TASagentTwitchBot.Core.IRC;

public record TwitchChatter
{
    public User User { get; init; } = null!;
    public DateTime? CreatedAt { get; init; }
    public string Badges { get; init; } = null!;
    public string Message { get; init; } = null!;
    public string MessageId { get; init; } = null!;
    public bool Whisper { get; init; }
    public int Bits { get; init; }
    public IReadOnlyList<Emote> Emotes { get; init; } = null!;

    public static async Task<TwitchChatter?> FromIRCMessage(
        IRCMessage message,
        ICommunication communication,
        IServiceScopeFactory scopeFactory)
    {
        if (message.ircCommand != IRCCommand.PrivMsg && message.ircCommand != IRCCommand.Whisper)
        {
            return null;
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        if (message.tags is null)
        {
            communication.SendDebugMessage($"Unable to parse IRC message with no tags: {message}");
            return null;
        }

        User? user = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserId == message.tags["user-id"]);

        string? displayName = message.tags["display-name"];

        if (string.IsNullOrWhiteSpace(displayName) || displayName == "1")
        {
            displayName = message.user;
        }

        string? color = message.tags.GetValueOrDefault("color");

        if (user is null)
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

            db.Users.Add(user);
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

            if (message.tags["mod"] == "1" && user.AuthorizationLevel < Commands.AuthorizationLevel.Moderator)
            {
                communication.SendDebugMessage($"Updating user {user.TwitchUserName} Authorization Level to Moderator");
                user.AuthorizationLevel = Commands.AuthorizationLevel.Moderator;
                saveChanges = true;
            }

            if (saveChanges)
            {
                await db.SaveChangesAsync();
            }
        }

        int bits = 0;
        if (message.tags.TryGetValue("bits", out string? bitString))
        {
            bits = int.Parse(bitString);
        }

        List<Emote> emotes = new List<Emote>();

        if (message.tags.TryGetValue("emotes", out string? emoteString) && !string.IsNullOrEmpty(emoteString))
        {
            //Tags includes emotes
            foreach (string emoteSubString in emoteString.Split('/'))
            {
                //ForEach unique emote
                string? code = null;
                int splitIndex = emoteSubString.IndexOf(':');
                string id = emoteSubString[0..splitIndex];
                string url = $"http://static-cdn.jtvnw.net/emoticons/v1/{id}/2.0";
                foreach (string indexSet in emoteSubString[(splitIndex + 1)..].Split(','))
                {
                    //ForEach instance of each emote
                    int rangeSplit = indexSet.IndexOf('-');
                    int startIndex = int.Parse(indexSet[0..rangeSplit]);
                    int endIndex = int.Parse(indexSet[(rangeSplit + 1)..]);

                    if (code is null)
                    {
                        //Extract the emote code from the message
                        code = message.message[startIndex..(endIndex + 1)];
                    }

                    emotes.Add(new Emote(code, url, startIndex, endIndex));
                }
            }
        }

        //Sort emotes in order of appearance
        emotes.Sort(OrderEmotes);

        if (message.ircCommand == IRCCommand.Whisper)
        {
            return new TwitchChatter()
            {
                User = user,
                CreatedAt = DateTime.Now,
                Badges = message.tags["badges"],
                Message = message.message,
                MessageId = "",
                Whisper = true,
                Bits = bits,
                Emotes = emotes
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
                Bits = bits,
                Emotes = emotes
            };
        }
    }

    public string ToLogString() => Whisper ? $"[{CreatedAt:G}] {User.TwitchUserName} WHISPER: {Message}" : $"[{CreatedAt:G}] {User.TwitchUserName}: {Message}";

    public record Emote(string Code, string URL, int StartIndex, int EndIndex);

    private static int OrderEmotes(Emote lhs, Emote rhs) => lhs.StartIndex.CompareTo(rhs.StartIndex);
}
