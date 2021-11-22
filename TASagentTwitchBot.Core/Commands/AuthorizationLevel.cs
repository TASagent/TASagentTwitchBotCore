namespace TASagentTwitchBot.Core.Commands;

public enum AuthorizationLevel
{
    Restricted = 0,
    None,
    Elevated,
    Moderator,
    Admin
}
