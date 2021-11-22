using Microsoft.AspNetCore.Identity;

namespace TASagentTwitchBot.Core.WebServer.Models;

public class ApplicationUser : IdentityUser
{
    public string TwitchBroadcasterName { get; set; } = null!;
    public string TwitchBroadcasterId { get; set; } = null!;

    public string SubscriptionSecret { get; set; } = null!;

    public int MonthlyTTSUsage { get; set; }
    public int MonthlyTTSLimit { get; set; }

    public virtual HashSet<SubscriptionData> Subscriptions { get; set; } = null!;

}

public class SubscriptionData
{
    public int Id { get; set; }

    public int SubscriberId { get; set; }
    public ApplicationUser Subscriber { get; set; } = null!;

    public string SubscriptionId { get; set; } = null!;
    public string SubscriptionType { get; set; } = null!;
}
