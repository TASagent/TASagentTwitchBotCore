using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.WebServer.Models
{
    public class UserRolesViewModel
    {
        public string UserId { get; set; }
        public string TwitchBroadcasterId { get; set; }
        public string TwitchBroadcasterName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public IEnumerable<string> Roles { get; set; }
        public int MonthlyTTSCharactersUsed { get; set; }
        public int MonthlyTTSCharacterLimit { get; set; }
    }
}
