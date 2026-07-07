using System.Data;
using Topshelf;

namespace IntegrationPollingService

{

    internal class Program
    {

        static void Main(string[] args)

        {
            var exitCode = HostFactory.Run(x =>
            {

                x.Service<Service>(s =>
                {
                    s.ConstructUsing(service => new Service());

                    s.WhenStarted(service => service.Start());

                    s.WhenStopped(service => service.Stop());
                });


                x.RunAsLocalSystem();

                x.SetServiceName("QuoteProjectIntegrationPollingService");

                x.SetDisplayName("QuoteProjectIntegrationPollingService");

                x.SetDescription("Quote Project Service Developed by Kiteview Technologies");

            });


            int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());

            Environment.ExitCode = exitCodeValue;
        }

        public static void RunApp()
        {

            //RunApp();

        }
    }

}