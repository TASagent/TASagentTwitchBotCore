using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TASagentTwitchBot.Core.API.Twitch;

namespace TASagentTwitchBot.Core.WebSub
{
    public interface IFollowSubscriber : IWebSubSubscriber
    {
        Task NotifyFollower(string id, string name);
    }

    public class FollowSubscriber : IFollowSubscriber, IWebSubSubscriber
    {
        private readonly Config.BotConfiguration botConfig;
        private readonly Config.IExternalWebAccessConfiguration webAccessConfig;
        private readonly ICommunication communication;
        private readonly Notifications.IFollowerHandler followerHandler;
        private readonly HelixHelper helixHelper;

        private readonly IServiceScopeFactory scopeFactory;

        private string externalURL = null;
        private string subURL = null;

        public FollowSubscriber(
            Config.IBotConfigContainer botConfigContainer,
            Config.IExternalWebAccessConfiguration webAccessConfiguration,
            ICommunication communication,
            Notifications.IFollowerHandler followerHandler,
            HelixHelper helixHelper,
            IServiceScopeFactory scopeFactory)
        {
            botConfig = botConfigContainer.BotConfig;
            webAccessConfig = webAccessConfiguration;
            this.communication = communication;
            this.followerHandler = followerHandler;
            this.helixHelper = helixHelper;

            this.scopeFactory = scopeFactory;
        }

        public async Task Subscribe(WebSubHandler webSubHandler)
        {
            string externalAddress = await webAccessConfig.GetExternalWebSubAddress();

            externalURL = $"{externalAddress}/TASagentBotAPI/WebSub/Followers";

            subURL = $"https://api.twitch.tv/helix/users/follows?first=1&to_id={botConfig.BroadcasterId}";

            bool success = await helixHelper.WebhookSubscribe(
                callback: externalURL,
                mode: "subscribe",
                topic: subURL,
                lease: 48 * 60 * 60,
                secret: webSubHandler.CreateSecretForRoute("/TASagentBotAPI/WebSub/Followers"));

            if (!success)
            {
                communication.SendErrorMessage("Failed to subscribe to Follows. Aborting.");

                externalURL = null;
                subURL = null;
            }
        }

        public async Task NotifyFollower(string id, string name)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            Database.BaseDatabaseContext db = scope.ServiceProvider.GetRequiredService<Database.BaseDatabaseContext>();
            Database.User follower = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserId == id);

            if (follower == null)
            {
                follower = new Database.User()
                {
                    TwitchUserName = name,
                    TwitchUserId = id,
                    FirstSeen = DateTime.Now,
                    FirstFollowed = DateTime.Now,
                    AuthorizationLevel = Commands.AuthorizationLevel.None
                };

                await db.Users.AddAsync(follower);
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

        public async Task Unsubscribe(WebSubHandler webSubHandler)
        {
            if (externalURL is null || subURL is null)
            {
                return;
            }

            TaskCompletionSource taskCompletionSource = new TaskCompletionSource();
            webSubHandler.NotifyPendingClosure("/TASagentBotAPI/WebSub/Followers", taskCompletionSource);

            //Try unsubscribing
            await helixHelper.WebhookSubscribe(
                callback: externalURL,
                mode: "unsubscribe",
                topic: subURL,
                lease: 0,
                secret: "");

            //Clear values
            externalURL = null;
            subURL = null;

            await taskCompletionSource.Task;
        }
    }
}
