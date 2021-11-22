using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace TASagentTwitchBot.Core.EventSub;

public class FollowSubscriber : IEventSubSubscriber
{
    private readonly ICommunication communication;
    private readonly Notifications.IFollowerHandler followerHandler;

    private readonly IServiceScopeFactory scopeFactory;

    public FollowSubscriber(
        ICommunication communication,
        Notifications.IFollowerHandler followerHandler,
        IServiceScopeFactory scopeFactory)
    {
        this.communication = communication;
        this.followerHandler = followerHandler;

        this.scopeFactory = scopeFactory;
    }

    public void RegisterHandlers(Dictionary<string, EventHandler> handlers)
    {
        handlers.Add("channel.follow", HandleFollowEvent);
    }

    public async Task HandleFollowEvent(JsonElement twitchEvent)
    {
        string name = twitchEvent.GetProperty("user_name").GetString()!;
        string id = twitchEvent.GetProperty("user_id").GetString()!;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id))
        {
            communication.SendWarningMessage($"Received bad Follower event: {twitchEvent.GetString()}");
            return;
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        Database.BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<Database.BaseDatabaseContext>();
        Database.User? follower = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserId == id);

        if (follower is null)
        {
            follower = new Database.User()
            {
                TwitchUserName = name,
                TwitchUserId = id,
                FirstSeen = DateTime.Now,
                FirstFollowed = DateTime.Now,
                AuthorizationLevel = Commands.AuthorizationLevel.None
            };

            db.Users.Add(follower);
            await db.SaveChangesAsync();
        }
        else
        {
            bool changesMade = false;

            if (!follower.FirstSeen.HasValue)
            {
                follower.FirstSeen = DateTime.Now;
                changesMade = true;
            }

            if (!follower.FirstFollowed.HasValue)
            {
                follower.FirstFollowed = DateTime.Now;
                changesMade = true;
            }

            if (changesMade)
            {
                await db.SaveChangesAsync();
            }
        }

        followerHandler.HandleFollower(follower, true);
    }
}
