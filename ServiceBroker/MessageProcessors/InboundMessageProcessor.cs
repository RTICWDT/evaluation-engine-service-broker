// -----------------------------------------------------------------------
// <copyright file="InboundMessageProcessor.cs" company="RTI">
// TODO: Update copyright text.
// see: http://code.msdn.microsoft.com/Service-Broker-Message-e81c4316#content
// </copyright>
// -----------------------------------------------------------------------

namespace ServiceBroker.MessageProcessors
{
    using System;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Processes an incoming message.
    /// </summary>
    public class InboundMessageProcessor
    {
        /// <summary>
        /// Processes the message. User for debuging purposes.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void ProcessMessage(byte[] message)
        {
            using (var writer = new StreamWriter("C:\\eeservicebroker\applicationLogs.txt", true))
            {
                writer.WriteLine(Encoding.UTF8.GetString(message, 0, message.Length));
            }

            Trace.WriteLine("InboundMessageProcessor Recieved Message");
            return;
        }

        /// <summary>
        /// Saves the failed message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="sqlConnection">The SQL connection.</param>
        /// <param name="errorInfo">The error info.</param>
        public static void SaveFailedMessage(byte[] message, SqlConnection sqlConnection, Exception errorInfo)
        {
            Trace.WriteLine("InboundMessageProcessor Recieved Failed Message");
            return;
        }
    }
}
