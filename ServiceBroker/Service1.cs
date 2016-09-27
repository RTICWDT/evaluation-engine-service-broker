// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Service1.cs" company="RTI">
//   Copyright (c) MPR Inc. All rights reserved.
//   see: http://code.msdn.microsoft.com/Service-Broker-Message-e81c4316#content
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ServiceBroker
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.ServiceProcess;
    using System.Threading;
    using System.Transactions;

    /// <summary>
    /// A service that queries to database to see what reports have been completed.
    /// </summary>
    public partial class EEServiceBroker : ServiceBase
    {
        /// <summary>
        /// A list of queue settings
        /// </summary>
        private static List<QueueListenerConfiguration> queueSettings = new List<QueueListenerConfiguration>();

        /// <summary>
        /// A list of listeners
        /// </summary>
        private static List<Thread> listeners = new List<Thread>();

        /// <summary>
        /// The stopping
        /// </summary>
        private static bool stopping = false;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EEServiceBroker"/> class.
        /// </summary>
        public EEServiceBroker()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Checks for messages; if a message is found, then it is processed.
        /// </summary>
        /// <param name="queueListenerConfig">The queue listener config.</param>
        public static void ListenerThreadProc(object queueListenerConfig)
        {
            QueueListenerConfiguration config = (QueueListenerConfiguration)queueListenerConfig;
            while (!stopping)
            {
                TransactionOptions to = new TransactionOptions();
                to.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
                to.Timeout = TimeSpan.MaxValue;

                CommittableTransaction tran = new CommittableTransaction(to);

                // try to get a message; if one is found, process it.
                try
                {
                    using (var con = new SqlConnection(config.ConnectionString))
                    {
                        // get message
                        con.Open();
                        con.EnlistTransaction(tran);
                        var timeOutInSeconds = 10.0;
                        byte[] message = ServiceBrokerUtils.GetMessage(config.QueueName, con, TimeSpan.FromSeconds(timeOutInSeconds));
                        if (message == null)
                        {
                            tran.Commit();
                            con.Close();
                            continue;
                        }

                        // process message
                        try
                        {
                            if (config.EnlistMessageProcessor)
                            {
                                using (var ts = new TransactionScope(tran))
                                {
                                    config.MessageProcessor(message);
                                    ts.Complete();
                                }
                            }
                            else
                            {
                                config.MessageProcessor(message);
                            }
                        }
                        catch (SqlException ex)
                        {
                            config.FailedMessageProcessor(message, con, ex);
                        }

                        // the message processing succeeded or the FailedMessageProcessor ran so commit the RECEIVE
                        tran.Commit();
                        con.Close();
                    }
                }
                catch (SqlException ex)
                {
                    Trace.WriteLine("Unexpected Exception in Thread Proc for " + config.QueueName + ".  Thread Proc is exiting: " + ex.Message);
                    tran.Rollback();
                    tran.Dispose();
                    return;
                }
            }
        }

        /// <summary>
        /// Starts the service.
        /// </summary>
        public void StartService(string[] args)
        {
            this.OnStart(args);
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        public void StopService()
        {
            this.OnStop();
        }
        
        /// <summary>
        /// Executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            queueSettings = QueueListenerConfigurationFactory.GetQueueListenerConfigurations();
            foreach (var q in queueSettings)
            {
                for (int i = 0; i < q.Threads; i++)
                {
                    Thread listenerThread = new Thread(ListenerThreadProc);
                    listenerThread.Name = "Listener Thread " + i.ToString() + " for " + q.QueueName;
                    listenerThread.IsBackground = false;
                    listeners.Add(listenerThread);

                    listenerThread.Start(q);
                    Trace.WriteLine("Started thread " + listenerThread.Name);
                }
            }
        }

        /// <summary>
        /// Executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
            stopping = true;
            foreach (var t in listeners)
            {
                t.Join(20 + 1000);
            }
        }
    }
}
