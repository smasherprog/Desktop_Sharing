using System;
using System.Collections.Generic;

using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace WindowsService
{
    public static class Interactive
    {
        static public void Run(ServiceBase[] servicesToRun)
        {
            Console.WriteLine("Services running in interactive mode.");
            Console.WriteLine();

            MethodInfo onStartMethod = typeof(ServiceBase).GetMethod("OnStart",
                BindingFlags.Instance | BindingFlags.NonPublic);
            foreach(ServiceBase service in servicesToRun)
            {
                Console.WriteLine("Starting {0}...", service.ServiceName);
                onStartMethod.Invoke(service, new object[] { new string[] { } });
                Console.WriteLine("Started");
            }

            //MethodInfo onStopMethod = typeof(ServiceBase).GetMethod("OnStop",
            //    BindingFlags.Instance | BindingFlags.NonPublic);
            //foreach(ServiceBase service in servicesToRun)
            //{
            //    Console.WriteLine("Stopping {0}...", service.ServiceName);
            //    onStopMethod.Invoke(service, null);
            //    Console.WriteLine("Stopped");
            //}

            Console.WriteLine("All services stopped.");
            // Keep the console alive for a second to allow the user to see the message.
            Thread.Sleep(1000);
        }
    }
}
