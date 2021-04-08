using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core
{
    public class ApplicationManagement
    {
        private readonly TaskCompletionSource exitTrigger = new TaskCompletionSource();

        public ApplicationManagement()
        {
            
        }

        public Task WaitForEndAsync() => exitTrigger.Task;

        public void TriggerExit() => exitTrigger.TrySetResult();
    }
}
