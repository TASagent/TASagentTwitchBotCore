using Microsoft.EntityFrameworkCore;

namespace TASagentTwitchBot.Core.Database;

[AutoRegister]
public interface IUserHelper
{
    Task<User> GetBroadcaster();
    Task<IReadOnlyList<User>> GetModUsers();

    Task<User?> GetUserByTwitchLogin(string twitchLogin, bool create = true);
    Task<User?> GetUserByTwitchId(string twitchId, bool create = true);
}

public class UserHelper : IUserHelper
{
    private readonly Config.BotConfiguration botConfig;
    private readonly API.Twitch.HelixHelper helixHelper;
    private readonly IServiceScopeFactory scopeFactory;

    public UserHelper(
        Config.BotConfiguration botConfig,
        API.Twitch.HelixHelper helixHelper,
        IServiceScopeFactory scopeFactory)
    {
        this.botConfig = botConfig;
        this.helixHelper = helixHelper;
        this.scopeFactory = scopeFactory;
    }

    public async Task<User> GetBroadcaster()
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        return await db.Users.FirstAsync(x => x.TwitchUserId == botConfig.BroadcasterId);
    }

    public async Task<IReadOnlyList<User>> GetModUsers()
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        return await db.Users.Where(x => x.AuthorizationLevel == Commands.AuthorizationLevel.Moderator).ToListAsync();
    }

    public async Task<User?> GetUserByTwitchId(string twitchId, bool create = true)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        User? user = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserId == twitchId);

        if (user is not null || !create)
        {
            return user;
        }

        API.Twitch.TwitchUsers.Datum? userData = await helixHelper.GetUserById(twitchId);

        if (userData is not null)
        {
            user = new User()
            {
                TwitchUserId = twitchId,
                TwitchUserName = string.IsNullOrWhiteSpace(userData.DisplayName) ? userData.Login : userData.DisplayName,
                AuthorizationLevel = Commands.AuthorizationLevel.None
            };

            db.Users.Add(user);

            await db.SaveChangesAsync();

            return user;
        }

        return null;
    }

    public async Task<User?> GetUserByTwitchLogin(string twitchLogin, bool create = true)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<BaseDatabaseContext>();

        User? user = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserName.ToLower() == twitchLogin.ToLower());

        if (user is not null || !create)
        {
            return user;
        }

        API.Twitch.TwitchUsers.Datum? userData = await helixHelper.GetUserByLogin(twitchLogin);

        if (userData is not null)
        {
            User? idMatchUser = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserId == userData.ID);

            if (idMatchUser is not null)
            {
                //The twitch login name in the database requires an update
                idMatchUser.TwitchUserName = string.IsNullOrWhiteSpace(userData.DisplayName) ? userData.Login : userData.DisplayName;
                user = idMatchUser;
            }
            else
            {
                user = new User()
                {
                    TwitchUserId = userData.ID,
                    TwitchUserName = string.IsNullOrWhiteSpace(userData.DisplayName) ? userData.Login : userData.DisplayName,
                    AuthorizationLevel = Commands.AuthorizationLevel.None
                };

                db.Users.Add(user);
            }


            await db.SaveChangesAsync();

            return user;
        }

        return null;
    }
}
