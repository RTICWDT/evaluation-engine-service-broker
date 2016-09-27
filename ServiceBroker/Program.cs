// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="RTI">
//   Copyright (c) MPR Inc. All rights reserved.
//   see: http://code.msdn.microsoft.com/Service-Broker-Message-e81c4316#content
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ServiceBroker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceProcess;
    using System.Text;

    /// <summary>
    /// This is a service that:
    /// 1] checks a database to see whether a report has been completed.
    /// 2] If a report is found, then a message is sent to another queue.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        /// <param name="args">The args.</param>
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "/console")
            {
                System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
                var s = new EEServiceBroker();
                s.StartService(args);
                Console.WriteLine("Started, hit any key to stop");
                Console.ReadKey();
                s.StopService();
                return;
            }

            ServiceBase[] servicesToRun;
            servicesToRun = new ServiceBase[] { new EEServiceBroker() };

            ServiceBase.Run(servicesToRun);
        }

    }
}
