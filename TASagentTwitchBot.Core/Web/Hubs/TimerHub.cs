using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.Web.Hubs
{
    public class TimerHub : Hub
    {
        private readonly Timer.ITimerManager timerManager;

        public TimerHub(
            Timer.ITimerManager timerManager)
        {
            this.timerManager = timerManager;
        }

        public async Task RequestState()
        {
            await Clients.Caller.SendAsync("SetState", timerManager.GetTimerState());
        }
    }
}
