using System;
using System.Threading.Tasks;


namespace TASagentTwitchBot.Core.Notifications
{
    public abstract class ActivityRequest
    {
        public int Id { get; set; } = 0;

        public bool Played { get; set; } = false;

        public ActivityRequest() { }

        public abstract Task Execute();
    }
}
