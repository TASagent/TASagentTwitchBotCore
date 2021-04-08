using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;


namespace BasicMicController
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Initialize DataManagement
            BGC.IO.DataManagement.Initialize("TASagentBot");

            IConfigurationRoot config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();


            //netsh http add urlacl url="http://+:5000/" user=everyone
            IWebHost host = new WebHostBuilder()
                .UseConfiguration(config)
                .UseKestrel()
                .UseUrls("http://0.0.0.0:5000")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseWebRoot(BGC.IO.DataManagement.PathForDataDirectory("wwwroot"))
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.StartAsync().Wait();

            BasicMicApplication application = host.Services.GetService(typeof(BasicMicApplication)) as BasicMicApplication;
            application.RunAsync().Wait();

            host.StopAsync().Wait();
        }
    }
}
