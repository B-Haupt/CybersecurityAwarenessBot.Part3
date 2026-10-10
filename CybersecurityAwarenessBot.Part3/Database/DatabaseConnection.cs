using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CybersecurityAwarenessBot.Database
{
    /// <summary>
    /// This class creates the connection to the MySQL database that stores the user's tasks.
    /// It also turns the MySQL errors into messages that the user can understand.
    /// </summary>
    internal class DatabaseConnection
    {

        /// <summary>
        /// This is the connection string
        /// </summary>
        private const string ConnectionString = "Server=localhost;Port=3306;Database=cybersecurity_bot;User ID=cyberbot_app;Password=CyberAware#2026;Connection Timeout=5;";

        /// <summary>
        /// This creates an new, unopened connection.
        /// </summary>
        /// <returns>A connection to the task database</returns>
        public static MySqlConnection Create() {
            return new MySqlConnection(ConnectionString);
        }

        /// <summary>
        ///  This method tries to open a connection, without freezing the window while it waits.
        /// </summary>
        /// <returns> Null if the connection worked or message if there is an error</returns>
        public static async Task<string?> TestConnectionAsync() {
            try
            {
                using MySqlConnection connection = Create();
                await connection.OpenAsync();
                return null;
            }
            catch (MySqlException ex) 
            {
                return DescribeError(ex);
            }
        }

        /// <summary>
        /// Turns a MySQL error into a message that says what to do about it. The numbers are MySQL's own error codes.
        /// </summary>
        /// <param name="ex">The error MySQL reported</param>
        /// <returns>A message suitable for showing to the user</returns>
        public static string DescribeError(MySqlException ex) 
        {
            return ex.Number switch
            {
                // 1042: nothing answered on localhost:3306, so the MySQL service isn't running
                1042 => "Couldn't reach the task database. Please check that the MySQL service is running, then try again.",

                // 1045: the login was rejected, usually because schema.sql hasn't been run on this computer yet
                1045 => "The task database refused the login. Please run Database/schema.sql in MySQL Workbench to set it up.",

                // 1049: the cybersecurity_bot database doesn't exist yet
                1049 => "The task database hasn't been created yet. Please run Database/schema.sql in MySQL Workbench.",

                _ => "Something went wrong with the task database: " + ex.Message
            };
        }


    }
}
