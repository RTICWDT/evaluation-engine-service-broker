// -----------------------------------------------------------------------
// <copyright file="StudyIdsMessageProcessor.cs" company="RTI">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace ServiceBroker.MessageProcessors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using NLog;
    using System.Diagnostics;
    using System.Data.SqlClient;
    using System.Configuration;
    using System.Security.Cryptography;
    using Npgsql;
    using System.Transactions;
    using Newtonsoft.Json;
    using Messages;

    /// <summary>
    /// Proccesses a message by converting Student IDs into Study IDs.
    /// </summary>
    public class StudyIdsMessageProcessor
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void ProcessMessage(byte[] message)
        {
            var stringMessage = System.Text.Encoding.Default.GetString(message);
            var jobGUID = new Guid(stringMessage);
            var studentIds = GetStudentIds(jobGUID);
            var hashedIds = HashStudentIds(studentIds);
            var studyIds = GetStudyIds(hashedIds);

            if (studyIds.Count > 0)
            {
                using(var transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    DeleteStudentIds(jobGUID);
                    InsertStudyIdsInMessageTable(studyIds, jobGUID);
                    transactionScope.Complete();
                }   
            }

            logger.Info("Message Received: {0}", stringMessage); 
            Trace.WriteLine("StudyIdsMessageProcessor Recieved Message");

            // Now update the Job Status to READY (5)
            UpdateJobStatus(jobGUID);
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
            Trace.WriteLine("StudyIdsMessageProcessor Recieved Failed Message");
            return;
        }

        /// <summary>
        /// Gets the student ids.
        /// </summary>
        /// <param name="jobGUID">The job GUID.</param>
        /// <returns>A list of student ids.</returns>
        private static List<string> GetStudentIds(Guid jobGUID)
        {
            var output = new List<string>();
            var connectionString = ConfigurationManager.ConnectionStrings["communicationsConnectionString"].ToString();
            var sqlStatement = @"SELECT StudentId FROM JobStudentIds WHERE JobGUID = @jobGUID;";
            using (var sqlConnection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(sqlStatement, sqlConnection))
            {
                sqlConnection.Open();
                command.Parameters.AddWithValue("@jobGUID", jobGUID.ToString());
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    int studentIdOrdinal = reader.GetOrdinal("StudentId");
                    while (reader.Read())
                    {
                        output.Add(reader.GetString(studentIdOrdinal));
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Hashes the student ids.
        /// </summary>
        /// <param name="studentIds">The student ids.</param>
        /// <returns>A list with hashed student ids.</returns>
        private static List<string> HashStudentIds(List<string> studentIds)
        {
            var output = new List<string>();
            foreach (var id in studentIds)
            {
                output.Add(HashID(id));
            }

            return output;
        }

        /// <summary>
        /// Gets the study ids.
        /// </summary>
        /// <param name="hashedStudentIds">The hashed student ids.</param>
        /// <returns></returns>
        private static List<string> GetStudyIds(List<string> hashedStudentIds)
        {
            var output = new List<string>();
            if (hashedStudentIds.Count == 0)
            {
                return output;
            }

            // It's okay not to check for SQL injections because the inputs have been hashed.
            var sqlStatement = new StringBuilder(@"select ""StudyId"" from ""Crosswalk"" where ""HashedId"" in (");
            for (int i = 0; i < hashedStudentIds.Count - 1; i++)
            {
                sqlStatement.Append("'");
                sqlStatement.Append(hashedStudentIds[i]);
                sqlStatement.Append("', ");
            }

            sqlStatement.Append("'");
            sqlStatement.Append(hashedStudentIds.Last());
            sqlStatement.Append("');");

            var connectionString = ConfigurationManager.ConnectionStrings["crosswalk"].ToString();
            using (NpgsqlConnection connections = new NpgsqlConnection(connectionString))
            {
                connections.Open();
                using (NpgsqlCommand command = new NpgsqlCommand(sqlStatement.ToString(), connections))
                {
                    command.Prepare();
                    using (NpgsqlDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            output.Add(dr["StudyId"].ToString());
                        }
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Deletes the student ids.
        /// </summary>
        /// <param name="jobGUID">The job GUID.</param>
        private static void DeleteStudentIds(Guid jobGUID)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["communicationsConnectionString"].ToString();
            var sqlStatement = @"DELETE FROM JobStudentIds WHERE JobGUID = @jobGUID;";
            using (var transactionScope = new TransactionScope())
            {
                using (var sqlConnection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(sqlStatement, sqlConnection))
                {
                    sqlConnection.Open();
                    command.Parameters.AddWithValue("@jobGUID", jobGUID.ToString());
                    command.ExecuteNonQuery();
                }

                transactionScope.Complete();
            }
        }

        /// <summary>
        /// Inserts the study ids in message table.
        /// </summary>
        /// <param name="studyIds">The study ids.</param>
        private static void InsertStudyIdsInMessageTable(List<string> studyIds, Guid jobGUID)
        {
            if (studyIds.Count == 0)
            {
                return; 
            }

            var jobId = jobGUID.ToString();
            var sqlStatement = new StringBuilder("INSERT INTO JobStudyIds (JobGUID, StudyId) ");
            for (int numIds = 0; numIds < studyIds.Count - 1; numIds++)
            {
                sqlStatement.Append("SELECT '");
                sqlStatement.Append(jobId);
                sqlStatement.Append("', '");
                sqlStatement.Append(studyIds[numIds]);
                sqlStatement.Append("' UNION ALL ");
            }

            sqlStatement.Append("SELECT '");
            sqlStatement.Append(jobId);
            sqlStatement.Append("', '");
            sqlStatement.Append(studyIds.Last());
            sqlStatement.Append("'");

            using (var transactionScope = new TransactionScope())
            {
                var connectionString = ConfigurationManager.ConnectionStrings["communicationsConnectionString"].ToString();
                using (var sqlConnection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(sqlStatement.ToString(), sqlConnection))
                {
                    sqlConnection.Open();
                    command.ExecuteNonQuery();
                }

                transactionScope.Complete();
            }
        }

        /// <summary>
        /// Hash a string.
        /// </summary>
        /// <param name="id">The string to hash.</param>
        /// <returns>The hashed string.</returns>
        private static string HashID(string id)
        {
            HMACSHA512 hashAlg = new HMACSHA512(Convert.FromBase64String(ConfigurationManager.AppSettings["Key64"]));

            byte[] text = Encoding.UTF8.GetBytes(id);
            byte[] hash = hashAlg.ComputeHash(text);

            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Updates the job status to READY.
        /// </summary>
        /// <param name="jobGUID">The job GUID.</param>
        private static void UpdateJobStatus(Guid jobGUID)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["communicationsConnectionString"].ToString();
            var sqlStatement = @"UPDATE Jobs SET Status = 5 WHERE JobGUID = @jobGUID;";
            using (var transactionScope = new TransactionScope())
            {
                using (var sqlConnection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(sqlStatement, sqlConnection))
                {
                    sqlConnection.Open();
                    command.Parameters.AddWithValue("@jobGUID", jobGUID.ToString());
                    command.ExecuteNonQuery();
                }

                transactionScope.Complete();
            }
        }
    }
}
