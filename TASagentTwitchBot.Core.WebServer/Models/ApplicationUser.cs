using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace TASagentTwitchBot.Core.WebServer.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string TwitchBroadcasterName { get; set; }
        public string TwitchBroadcasterId { get; set; }

        public string SubscriptionSecret { get; set; }

        public int MonthlyTTSUsage { get; set; }
        public int MonthlyTTSLimit { get; set; }

        public virtual HashSet<SubscriptionData> Subscriptions { get; set; }

    }

    public class SubscriptionData
    {
        public int Id { get; set; }

        public int SubscriberId { get; set; }
        public ApplicationUser Subscriber { get; set; }

        public string SubscriptionId { get; set; }
        public string SubscriptionType { get; set; }
    }
}
