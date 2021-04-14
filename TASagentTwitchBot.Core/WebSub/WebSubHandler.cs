using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

using TASagentTwitchBot.Core.API.Twitch;

namespace TASagentTwitchBot.Core.WebSub
{
    public interface IWebSubSubscriber
    {
        Task Subscribe(WebSubHandler webSubHandler);
        Task Unsubscribe(WebSubHandler webSubHandler);
    }


    public class WebSubHandler : IDisposable
    {
        private const string SIGNATURE_PREFIX = "sha256=";

        private readonly ICommunication communication;
        private readonly HelixHelper helixHelper;

        private readonly IWebSubSubscriber[] webSubSubscribers;
        private readonly Dictionary<string, string> secretDictionary = new Dictionary<string, string>();
        private readonly HashSet<string> pendingConnections = new HashSet<string>();
        private readonly Dictionary<string, TaskCompletionSource> pendingClosures = new Dictionary<string, TaskCompletionSource>();

        private bool disposedValue;

        public WebSubHandler(
            ICommunication communication,
            HelixHelper helixHelper,
            IEnumerable<IWebSubSubscriber> webSubSubscribers)
        {
            this.communication = communication;
            this.helixHelper = helixHelper;

            this.webSubSubscribers = webSubSubscribers.ToArray();
        }

        public async Task Subscribe()
        {
            //First, detect and unsubsribe from lingering webhooks
            TokenRequest tokenContainer = await helixHelper.GetClientCredentialsToken();
            TwitchWebhookResponse subscriptions = await helixHelper.GetWebhookSubscriptions(tokenContainer.AccessToken);

            if (subscriptions.Data is not null)
            {
                communication.SendWarningMessage($"Unsubscribing from {subscriptions.Data.Count} existing webhooks");

                foreach (TwitchWebhookResponse.Datum datum in subscriptions.Data)
                {
                    bool success = await helixHelper.WebhookSubscribe(
                        callback: datum.Callback,
                        mode: "unsubscribe",
                        topic: datum.Topic,
                        lease: 0,
                        secret: "");

                    if (!success)
                    {
                        communication.SendWarningMessage($"Failed to unsub {datum.Callback} from: {datum.Topic}");
                    }
                }
            }


            foreach (IWebSubSubscriber subscriber in webSubSubscribers)
            {
                await subscriber.Subscribe(this);
            }
        }

        public string CreateSecretForRoute(string route)
        {
            if (secretDictionary.ContainsKey(route))
            {
                communication.SendWarningMessage($"Route \"{route}\" already exists in secrets dictionary");
                return null;
            }

            //Cloning key - we don't know what happens to it when the hasher is cleaned up
            Guid newGuid = Guid.NewGuid();
            secretDictionary.Add(route, newGuid.ToString("N"));

            pendingConnections.Add(route);

            return secretDictionary[route];
        }

        public bool VerifyConnection(string route)
        {
            return pendingConnections.Remove(route);
        }

        public void NotifyPendingClosure(string route, TaskCompletionSource taskCompletionSource)
        {
            pendingClosures.Add(route, taskCompletionSource);
        }

        public void CloseConnection(string route)
        {
            if (pendingClosures.ContainsKey(route))
            {
                TaskCompletionSource taskCompletionSource = pendingClosures[route];
                pendingClosures.Remove(route);

                taskCompletionSource.SetResult();
            }
        }

        public async Task<bool> VerifyHubMessage(string route, string signature, HttpRequest request)
        {
            if (!secretDictionary.ContainsKey(route))
            {
                communication.SendWarningMessage($"Route \"{route}\" not found in secrets dictionary");
                return false;
            }

            try
            {
                byte[] secret = Encoding.ASCII.GetBytes(secretDictionary[route]);
                using StreamReader reader = new StreamReader(
                    request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: -1,
                    leaveOpen: true);

                string body = await reader.ReadToEndAsync();
                using HMACSHA256 hasher = new HMACSHA256(secret);
                byte[] result = hasher.ComputeHash(Encoding.ASCII.GetBytes(body));
                string stringified = BitConverter.ToString(result).Replace("-", "").ToUpper();

                if (signature.StartsWith(SIGNATURE_PREFIX))
                {
                    signature = signature[SIGNATURE_PREFIX.Length..];
                }

                return signature.Equals(stringified, StringComparison.OrdinalIgnoreCase);

            }
            catch (Exception e)
            {
                communication.SendErrorMessage($"Caught exception when trying to verify hash: {e}");
                return false;
            }
            finally
            {
                request.Body.Position = 0;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //Wait up to 3 seconds to disengage tasks
                    Task.WaitAny(
                        Task.WhenAll(webSubSubscribers.Select(x => x.Unsubscribe(this))),
                        Task.Delay(3000));
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
