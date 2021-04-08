using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace TASagentTwitchBot.Core.Database
{
    public interface IUserHelper
    {
        Task<User> GetUserByTwitchLogin(string twitchLogin);
        Task<User> GetUserByTwitchId(string twitchId);
    }

    public class UserHelper : IUserHelper
    {
        private readonly API.Twitch.HelixHelper helixHelper;
        private readonly BaseDatabaseContext db;

        public UserHelper(
            API.Twitch.HelixHelper helixHelper,
            BaseDatabaseContext db)
        {
            this.helixHelper = helixHelper;
            this.db = db;
        }

        public async Task<User> GetUserByTwitchId(string twitchId)
        {
            User user = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserId == twitchId);

            if (user is not null)
            {
                return user;
            }

            API.Twitch.TwitchUsers.Datum userData = await helixHelper.GetUserById(twitchId);

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

        public async Task<User> GetUserByTwitchLogin(string twitchLogin)
        {
            User user = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserName.ToLower() == twitchLogin.ToLower());

            if (user is not null)
            {
                return user;
            }

            API.Twitch.TwitchUsers.Datum userData = await helixHelper.GetUserByLogin(twitchLogin);

            if (userData is not null)
            {
                User idMatchUser = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserId == userData.ID);

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
}
