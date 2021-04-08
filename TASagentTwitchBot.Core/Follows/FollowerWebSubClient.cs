using System;
using System.Collections.Specialized;
using System.IO;
using System.Text.Json;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using TASagentTwitchBot.Core.API.Twitch;
using TASagentTwitchBot.Core.Database;

namespace TASagentTwitchBot.Core.Follows
{
    public class FollowerWebSubClient : IDisposable
    {
        private readonly HelixHelper helixHelper;
        private readonly Config.BotConfiguration botConfig;
        private readonly ICommunication communication;
        private readonly Notifications.IFollowerHandler followerHandler;

        private readonly BaseDatabaseContext db;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly CountdownEvent readers = new CountdownEvent(1);

        private string myIPAddress = null;
        private string externalURL = "";
        private readonly HttpListener httpListener;
        private bool disposedValue = false;
        private bool connected = false;

        public FollowerWebSubClient(
            Config.IBotConfigContainer botConfigContainer,
            ICommunication communication,
            Notifications.IFollowerHandler followerHandler,
            HelixHelper helixHelper,
            BaseDatabaseContext db)
        {
            botConfig = botConfigContainer.BotConfig;
            this.helixHelper = helixHelper;
            this.communication = communication;
            this.followerHandler = followerHandler;

            this.db = db;

            httpListener = new HttpListener();
        }

        public virtual async Task Connect()
        {
            if (string.IsNullOrEmpty(myIPAddress))
            {
                myIPAddress = await GetIPAddress();
            }

            externalURL = $"http://{myIPAddress}:9005/Followers/";

            httpListener.Prefixes.Clear();
            httpListener.Prefixes.Add("http://+:9005/Followers/");
            httpListener.Start();

            if (!httpListener.IsListening)
            {
                communication.SendErrorMessage("Listener not listening. Aborting.");
                await Task.Delay(2000);
                throw new Exception("Listener not listening. Aborting.");
            }

            bool success = await helixHelper.WebhookSubscribe(
                callback: externalURL,
                mode: "subscribe",
                topic: $"https://api.twitch.tv/helix/users/follows?first=1&to_id={botConfig.BroadcasterId}",
                lease: 48 * 60 * 60,
                secret: "");

            if (!success)
            {
                communication.SendErrorMessage("Failed to subscribe to Follows. Aborting.");
                await Task.Delay(2000);
                throw new Exception("Failed to subscribe to Follows. Aborting.");
            }

            HttpListenerContext context = await httpListener.GetContextAsync();
            HttpListenerRequest request = context.Request;

            NameValueCollection queryString = request.QueryString;

            string hubChallenge = queryString.Get("hub.challenge");

            if (string.IsNullOrEmpty(hubChallenge))
            {
                communication.SendErrorMessage($"Failed to subscribe to Follows: {queryString.Get("hub.reason")}. Aborting.");
                await Task.Delay(2000);
                throw new Exception($"Failed to subscribe to Follows: {queryString.Get("hub.reason")}. Aborting.");
            }

            using HttpListenerResponse response = context.Response;

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(hubChallenge);

            response.StatusCode = 200;
            response.ContentType = "text/plain";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            response.Close();

            connected = true;

            ListenForMessages();
        }

        protected virtual async void ListenForMessages()
        {
            try
            {
                readers.AddCount();

                while (true)
                {
                    HttpListenerContext context = await httpListener.GetContextAsync().WithCancellation(cancellationTokenSource.Token);

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        break;
                    }

                    HttpListenerRequest request = context.Request;

                    string text;
                    using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        text = reader.ReadToEnd();
                    }

                    if (string.IsNullOrEmpty(text))
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    TwitchFollows follows = JsonSerializer.Deserialize<TwitchFollows>(text);
                    TwitchFollows.Datum newFollower = follows.Data[0];

                    User follower = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserId == newFollower.FromID);

                    if (follower == null)
                    {
                        follower = new User()
                        {
                            TwitchUserName = newFollower.FromName,
                            TwitchUserId = newFollower.FromID,
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

                    HttpListenerResponse response = context.Response;
                    response.StatusCode = 200;
                    response.Close();
                }
            }
            catch (TaskCanceledException)
            {
                //Swallow
            }
            catch (ThreadAbortException)
            {
                //Swallow
            }
            catch (ObjectDisposedException)
            {
                //Swallow
            }
            finally
            {
                readers.Signal();
            }
        }

        private static async Task<string> GetIPAddress()
        {
            string address = "";
            const string ADDRESS_LABEL = "Address: ";
            WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
            using (WebResponse response = request.GetResponse())
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                address = await stream.ReadToEndAsync();
            }

            int first = address.IndexOf(ADDRESS_LABEL) + ADDRESS_LABEL.Length;
            int last = address.LastIndexOf("</body>");
            address = address[first..last];

            return address;
        }

        private async Task TryDisconnect()
        {
            if (!connected)
            {
                return;
            }

            await helixHelper.WebhookSubscribe(
                callback: externalURL,
                mode: "unsubscribe",
                topic: $"https://api.twitch.tv/helix/users/follows?first=1&to_id={botConfig.BroadcasterId}",
                lease: 0,
                secret: "");

            connected = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    TryDisconnect().Wait();

                    cancellationTokenSource.Cancel();

                    readers.Signal();
                    readers.Wait();
                    readers.Dispose();

                    httpListener.Abort();

                    cancellationTokenSource.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
