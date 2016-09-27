// -----------------------------------------------------------------------
// <copyright file="QueueListenerConfiguration.cs" company="RTI">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace ServiceBroker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Data.SqlClient;

    /// <summary>
    /// A class modeling the configuration of the queues the service will poll.
    /// </summary>
    public class QueueListenerConfiguration
    {
            /// <summary>
            /// Gets or sets the name of the queue.
            /// </summary>
            /// <value>
            /// The name of the queue.
            /// </value>
            public string QueueName { get; set; }

            /// <summary>
            /// Gets or sets the threads.
            /// </summary>
            /// <value>
            /// The threads.
            /// </value>
            public int Threads { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether [enlist message processor].
            /// </summary>
            /// <value>
            /// <c>true</c> if [enlist message processor]; otherwise, <c>false</c>.
            /// </value>
            public bool EnlistMessageProcessor { get; set; }

            /// <summary>
            /// Gets or sets the message processor.
            /// </summary>
            /// <value>
            /// The message processor.
            /// </value>
            public Action<byte[]> MessageProcessor { get; set; }

            /// <summary>
            /// Gets or sets the failed message processor.
            /// </summary>
            /// <value>
            /// The failed message processor.
            /// </value>
            public Action<byte[], SqlConnection, Exception> FailedMessageProcessor { get; set; }

            /// <summary>
            /// Gets or sets the connection string.
            /// </summary>
            /// <value>
            /// The connection string.
            /// </value>
            public string ConnectionString { get; set; }
    }
}
