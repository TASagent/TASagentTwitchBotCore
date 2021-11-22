namespace TASagentTwitchBot.Core.WebServer.Models;

public class UserRolesViewModel
{
    public string UserId { get; set; } = null!;
    public string TwitchBroadcasterId { get; set; } = null!;
    public string TwitchBroadcasterName { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public IEnumerable<string> Roles { get; set; } = null!;
    public int MonthlyTTSCharactersUsed { get; set; }
    public int MonthlyTTSCharacterLimit { get; set; }
}
