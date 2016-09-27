// -----------------------------------------------------------------------
// <copyright file="QueueListenerConfigurationFactory.cs" company="RTI">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace ServiceBroker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Configuration;
    using MessageProcessors;

    /// <summary>
    /// A class in charge of generating a list of QueueListenerConfigurations.
    /// </summary>
    internal static class QueueListenerConfigurationFactory
    {
        /// <summary>
        /// Gets the queue listener configurations.
        /// </summary>
        /// <returns></returns>
        internal static List<QueueListenerConfiguration> GetQueueListenerConfigurations()
        {
            var output = new List<QueueListenerConfiguration>();
            var config = new QueueListenerConfiguration();

            var processor = ConfigurationManager.AppSettings["processor"].ToString();

            if (processor.Equals("ClientNotification"))
            {
                config.ConnectionString = ConfigurationManager.ConnectionStrings["clientNotificationConnectionString"].ToString();
                config.QueueName = ConfigurationManager.AppSettings["clientNotificationQueueName"].ToString();
                config.Threads = 1;
                config.EnlistMessageProcessor = false;
                config.MessageProcessor = ClientNotificationMessageProcessor.ProcessMessage;
                config.FailedMessageProcessor = ClientNotificationMessageProcessor.SaveFailedMessage;
            }
            else
            {
                config.ConnectionString = ConfigurationManager.ConnectionStrings["getStudyIdsConnectionString"].ToString();
                config.QueueName = ConfigurationManager.AppSettings["getStudyIdsQueueName"].ToString();
                config.Threads = 1;
                config.EnlistMessageProcessor = false;
                config.MessageProcessor = StudyIdsMessageProcessor.ProcessMessage;
                config.FailedMessageProcessor = StudyIdsMessageProcessor.SaveFailedMessage;
            }
            
            output.Add(config);
            return output;
        }
    }
}
