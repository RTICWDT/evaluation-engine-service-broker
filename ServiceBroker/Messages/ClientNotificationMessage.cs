// -----------------------------------------------------------------------
// <copyright file="ClientNotificationMessage.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace ServiceBroker.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A class defining what the client notification message should look like.
    /// </summary>
    public class ClientNotificationMessage
    {
        /// <summary>
        /// Gets or sets the job GUID.
        /// </summary>
        /// <value>
        /// The job GUID.
        /// </value>
        public Guid JobGUID { get; set; }

        /// <summary>
        /// Gets or sets the job status.
        /// </summary>
        /// <value>
        /// The job status.
        /// </value>
        public int JobStatus { get; set; }
    }
}
