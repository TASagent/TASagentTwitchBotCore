using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.Config
{
    public interface IExternalWebAccessConfiguration
    {
        Task<string> GetExternalAddress();
        Task<string> GetExternalWebSubAddress();
        string GetLocalAddress();
    }

    public class ExternalWebAccessConfiguration : IExternalWebAccessConfiguration
    {
        private string myIPAddress = null;

        public async Task<string> GetExternalAddress()
        {
            if (string.IsNullOrEmpty(myIPAddress))
            {
                myIPAddress = await GetIPAddress();
            }

            return $"http://{myIPAddress}:5000";
        }

        public async Task<string> GetExternalWebSubAddress()
        {
            if (string.IsNullOrEmpty(myIPAddress))
            {
                myIPAddress = await GetIPAddress();
            }

            return $"http://{myIPAddress}:{Web.Middleware.WebSubHandlerMiddleware.WEBSUB_PORT}";
        }

        public string GetLocalAddress() => $"http://localhost:5000";

        private static async Task<string> GetIPAddress()
        {
            string address = "";
            const string ADDRESS_LABEL = "Address: ";
            WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
            using (WebResponse response = request.GetResponse())
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                address = await stream.ReadToEndAsync();
            }

            int first = address.IndexOf(ADDRESS_LABEL) + ADDRESS_LABEL.Length;
            int last = address.LastIndexOf("</body>");
            address = address[first..last];

            return address;
        }
    }
}
