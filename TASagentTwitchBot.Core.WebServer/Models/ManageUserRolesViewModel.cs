﻿namespace TASagentTwitchBot.Core.WebServer.Models;

public class ManageUserRolesViewModel
{
    public string RoleId { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public bool Selected { get; set; }
}
