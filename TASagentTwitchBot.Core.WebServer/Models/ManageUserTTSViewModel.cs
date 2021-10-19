using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.WebServer.Models
{
    public class ManageUserTTSViewModel
    {
        public int MonthlyTTSCharactersUsed { get; set; }
        public int MonthlyTTSCharacterLimit { get; set; }
    }
}
