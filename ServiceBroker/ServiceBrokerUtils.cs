// -----------------------------------------------------------------------
// <copyright file="ServiceBrokerUtils.cs" company="RTI">
// TODO: Update copyright text.
// see: http://code.msdn.microsoft.com/Service-Broker-Message-e81c4316#content
// </copyright>
// -----------------------------------------------------------------------

namespace ServiceBroker
{
    using System;
    using System.Data;
    using System.Data.SqlClient;

    /// <summary>
    /// A class in charge of retrieving messages from a queue.
    /// </summary>
    internal class ServiceBrokerUtils
    {
        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="con">The con.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>If it's not the end of the conversation, the message; null, otherwise.</returns>
        public static byte[] GetMessage(string queueName, SqlConnection con, TimeSpan timeout)
        {
            using (SqlDataReader reader = GetMessageBatch(queueName, con, timeout, 1))
            {
                if (reader == null || !reader.HasRows)
                {
                    return null;
                }

                reader.Read();
                var conversationHandle = reader.GetGuid(reader.GetOrdinal("conversation_handle"));
                var messageType = reader.GetString(reader.GetOrdinal("message_type_name"));
                if (messageType.Equals("http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog"))
                {
                    EndConversation(conversationHandle, con);
                    return null;
                }

                var body = reader.GetSqlBinary(reader.GetOrdinal("message_body"));
                return body.Value;
            }
        }

        /// <summary>
        /// Ends the conversation.
        /// </summary>
        /// <param name="conversationHandle">The conversation handle.</param>
        /// <param name="sqlConnection">The con.</param>
        internal static void EndConversation(Guid conversationHandle, SqlConnection sqlConnection)
        {
            try
            {
                var sqlStatement = "END CONVERSATION @ConversationHandle;";
                using (var command = new SqlCommand(sqlStatement, sqlConnection))
                {
                    var conversation = command.Parameters.Add("@ConversationHandle", SqlDbType.UniqueIdentifier);
                    conversation.Value = conversationHandle;
                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Gets the message batch.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="sqlConnection">The SQL connection.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="maxMessages">The max messages.</param>
        /// <returns>A SqlDataReader.</returns>
        private static SqlDataReader GetMessageBatch(string queueName, SqlConnection sqlConnection, TimeSpan timeout, int maxMessages)
        {
            var sqlStatement = string.Format(@"waitfor(RECEIVE top (@count) conversation_handle,service_name,message_type_name,message_body,message_sequence_number FROM [{0}]), timeout @timeout", queueName);

            var command = new SqlCommand(sqlStatement, sqlConnection);

            var numberOfMessages = command.Parameters.Add("@count", SqlDbType.Int);
            numberOfMessages.Value = maxMessages;

            var paramTimeout = command.Parameters.Add("@timeout", SqlDbType.Int);

            if (timeout == TimeSpan.MaxValue)
            {
                paramTimeout.Value = -1;
            }
            else
            {
                paramTimeout.Value = (int)timeout.TotalMilliseconds;
            }

            // honor the RECIEVE timeout, whatever it is.
            command.CommandTimeout = 0;

            return command.ExecuteReader();
        }
    }
}
