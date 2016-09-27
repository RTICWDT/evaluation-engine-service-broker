// -----------------------------------------------------------------------
// <copyright file="ClientNotificationMessageProcessor.cs" company="RTI">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace ServiceBroker.MessageProcessors
{
    using System;
    using System.Configuration;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Net.Configuration;
    using System.Net.Mail;
    using Messages;
    using Newtonsoft.Json;
    using NLog;

    /// <summary>
    /// Processes a message by sending an email to the user who ran an Evaluation Engine report.
    /// </summary>
    public class ClientNotificationMessageProcessor
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Processes the message by sending the user an email.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void ProcessMessage(byte[] message)
        {
            var stringMessage = System.Text.Encoding.Default.GetString(message);
            var classMessage = JsonConvert.DeserializeObject<ClientNotificationMessage>(stringMessage);

            var messageDetails = GetMessageDetails(classMessage.JobGUID);
            var userEmailAddress = messageDetails.ClientEmailAddress;
            var messageToUser = GetMessageToUser(classMessage.JobStatus, messageDetails);

            if (!string.IsNullOrEmpty(userEmailAddress) && !string.IsNullOrEmpty(messageToUser))
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var section = config.GetSection("system.net/mailSettings/smtp") as SmtpSection;
                var emailMessage = new MailMessage();
                emailMessage.Body = messageToUser;
                emailMessage.Subject = ServiceBroker.Properties.Resources.ReportCompletedSubject; 
                emailMessage.To.Add(userEmailAddress);
                using (var client = new SmtpClient())
                {
                    try
                    {
                        emailMessage.From = new MailAddress(section.From);
                        client.Send(emailMessage);
                    }
                    catch (Exception ex)
                    {
                        logger.Info("Error: {0}", ex.Message); 
                    }
                    
                }
            }

            logger.Info("Message Received: {0}", stringMessage); 
            Trace.WriteLine("ClientNotificationMessageProcessor Recieved Message");
        }

        /// <summary>
        /// Saves the failed message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="sqlConnection">The SQL connection.</param>
        /// <param name="errorInfo">The error info.</param>
        public static void SaveFailedMessage(byte[] message, SqlConnection sqlConnection, Exception errorInfo)
        {
            var stringMessage = System.Text.Encoding.Default.GetString(message);
            logger.Error("Failed Message: {0}.  Error: {1}", stringMessage, errorInfo.Message);
            Trace.WriteLine("ClientNotificationMessageProcessor Recieved Failed Message");
            return;
        }

        /// <summary>
        /// Gets the message details.
        /// </summary>
        /// <param name="jobGUID">The job GUID.</param>
        /// <returns>A MessageDetails struct.</returns>
        private static MessageDetails GetMessageDetails(Guid jobGUID)
        {
            var details = new MessageDetails();
            var connectionString = ConfigurationManager.ConnectionStrings["webAppConnectionString"].ToString();
            var sqlStatement = @"SELECT TOP 1 Id, UserEmail FROM Analyses WHERE JobGUID = @jobGUID;";
            using (var sqlConnection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(sqlStatement, sqlConnection))
            {
                sqlConnection.Open();
                command.Parameters.AddWithValue("@jobGUID", jobGUID.ToString());
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    int userNameOrdinal = reader.GetOrdinal("UserEmail");
                    int idOrdinal = reader.GetOrdinal("Id");
                    details.ClientEmailAddress = reader.GetString(userNameOrdinal);
                    details.ReportId = reader.GetInt32(idOrdinal);
                }
            }

            return details;
        }

        /// <summary>
        /// Gets the message to user.
        /// </summary>
        /// <param name="jobStatus">The job status.</param>
        /// <param name="details">The details.</param>
        /// <returns>If it's a valid job status, an appropriate message is returned; otherwise, an empty string.</returns>
        private static string GetMessageToUser(int jobStatus, MessageDetails details)
        {
            switch (jobStatus)
            {
                case 3:
                    return string.Format(ServiceBroker.Properties.Resources.ReportCompletedSuccessfullyBody, GetReportUrl(details.ReportId));
                case 4:
                    return string.Format(ServiceBroker.Properties.Resources.ReportCompletedErrorBody, GetReportUrl(details.ReportId));
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets the report URL.
        /// </summary>
        /// <param name="reportId">The report id.</param>
        /// <returns>Returns a string representing the URL where the user can find the generated report.</returns>
        private static string GetReportUrl(int reportId)
        {
            return ConfigurationManager.AppSettings["reportBaseUrl"].ToString() + reportId;
        }

        /// <summary>
        /// A container for information necessary to notify client.
        /// </summary>
        private struct MessageDetails
        {
            /// <summary>
            /// Gets or sets the client email address.
            /// </summary>
            /// <value>
            /// The client email address.
            /// </value>
            public string ClientEmailAddress { get; set; }

            /// <summary>
            /// Gets or sets the report id.
            /// </summary>
            /// <value>
            /// The report id.
            /// </value>
            public int ReportId { get; set; }
        }
    }
}
