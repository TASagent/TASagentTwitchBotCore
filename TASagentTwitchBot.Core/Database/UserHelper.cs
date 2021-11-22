using Microsoft.EntityFrameworkCore;

namespace TASagentTwitchBot.Core.Database;

public interface IUserHelper
{
    Task<User?> GetUserByTwitchLogin(string twitchLogin, bool create = true);
    Task<User?> GetUserByTwitchId(string twitchId, bool create = true);
}

public class UserHelper : IUserHelper
{
    private readonly API.Twitch.HelixHelper helixHelper;
    private readonly IServiceScopeFactory scopeFactory;

    public UserHelper(
        API.Twitch.HelixHelper helixHelper,
        IServiceScopeFactory scopeFactory)
    {
        this.helixHelper = helixHelper;
        this.scopeFactory = scopeFactory;
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
