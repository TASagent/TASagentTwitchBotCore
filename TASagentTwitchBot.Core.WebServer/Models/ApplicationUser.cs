using Microsoft.AspNetCore.Identity;

namespace TASagentTwitchBot.Core.WebServer.Models;

public class ApplicationUser : IdentityUser
{
    public string? TwitchBroadcasterName { get; set; }
    public string? TwitchBroadcasterId { get; set; }

    public string SubscriptionSecret { get; set; } = null!;

    public int MonthlyTTSUsage { get; set; }
    public int MonthlyTTSLimit { get; set; }
}
