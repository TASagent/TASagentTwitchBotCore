using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.WebServer.Models
{
    public class ManageUserRolesViewModel
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool Selected { get; set; }
    }
}
