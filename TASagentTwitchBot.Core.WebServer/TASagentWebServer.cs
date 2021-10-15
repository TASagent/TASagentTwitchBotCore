using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.WebServer
{
    public class TASagentWebServer
    {
        private readonly ICommunication communication;
        private readonly ApplicationManagement applicationManagement;
        //private readonly ErrorHandler errorHandler;


        public TASagentWebServer(
            ICommunication communication,
            ApplicationManagement applicationManagement
            //, ErrorHandler errorHandler
            )
        {
            this.communication = communication;
            this.applicationManagement = applicationManagement;
            //this.errorHandler = errorHandler;
        }


        public async Task RunAsync()
        {
            communication.SendDebugMessage("*** Starting Up ***");

            try
            {
                await applicationManagement.WaitForEndAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                //errorHandler.LogSystemException(ex);
            }

        }
    }

}
