
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using ProtoBuf;
using ChitChat.Events;
using Dapper;

namespace CC_Server
{
    public class DatabaseManager
    {
        //Define the single database file
        private Dictionary<string, string> tables;
        private string DATABASE_FILE = "ChitChatDB.db";

        // Only a single connection will exist to this database
        private SQLiteConnection connection;

        #region
        // Default constructor
        public DatabaseManager() { }

        public DatabaseManager(string databasePath = "CCDB.sqlite")
        {
            DATABASE_FILE = databasePath;
        }
        #endregion

        /* Constructor with option to make database
         * Creates a new instance of a DatabaseManager and if specified
         * also creates the database file.
         * @param makeDatabase - A boolean indicating whether or not to make the DB file
         * @return A new instance of the DatabaseManager object
         */
        public DatabaseManager(bool makeDatabase, string databasePath = "ChitChatDB.db")
        {
            DATABASE_FILE = databasePath;
            if (makeDatabase)
            {
                // Let's see if the database file exists first
                if (!(DatabaseExists()))
                {
                    if (!(CreateDatabase()))
                    {
                        System.Windows.Forms.MessageBox.Show("Cannot create database file!");
                        return;
                    }
                }
                // The database is now in existence!
            }
        }

        /* OpenConnection method
         * Opens a connection to the database (if not already open) and
         * returns a boolean indicative of the success. Also handles cases where 
         * timing may have been an issue by prompting the user with an option to
         * retry the operation.
         * @return A boolean indicating whether or not the connection has been opened
         */
        public bool OpenConnection()
        {
            // If the connection already exists, we will just let it be
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
                return true;

            try
            {
                connection = new SQLiteConnection("Data Source=" + DATABASE_FILE);
                connection.Open();
                return true;
            }
            catch (Exception e)
            {
                // Provide the user with a chance to retry on error.
                if (System.Windows.Forms.MessageBox.Show("Connection to Database Failed.\n" + e.Message,
                    "Connection Failed", System.Windows.Forms.MessageBoxButtons.RetryCancel,
                    System.Windows.Forms.MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Retry)
                {
                    return OpenConnection();
                }
                else
                    return false;
            }
        }

        /* Tables Are Verified Method
        * Gets a list of tables that currently exist within the
        * database file and compares them with the table design that 
        * exists in the table dictionary. This method checks to 
        * make sure that the two match (in table names only).
        * @return A boolean indicating whether or not the table names 
        *         in the database match those defined in the table dictionary
        */
        public bool TablesAreVerified()
        {
            if (OpenConnection())
            {
                // The connection is opened and can be used!
                try
                {
                    SQLiteCommand command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type=\"table\"", connection);
                    List<string> tableNames = new List<string>();
                    foreach (string key in this.tables.Keys)
                        tableNames.Add(key);

                    SQLiteDataReader results = command.ExecuteReader();
                    while (results.Read())
                    {
                        string tableName = results.GetValue(0).ToString();
                        if (tableNames.Contains(tableName))
                            tableNames.Remove(tableName);
                    }
                    results.Close();
                    if (tableNames.Count > 0)
                        return false;
                    else
                        return true;
                }
                catch (Exception e)
                {

                    Console.WriteLine("Some error occurred while verifying the integrity of the tables.\n{0}", e.Message.ToString());
                    return false;
                }
            }
            else
            {
                // There is no connection, we must notify!
                System.Windows.Forms.MessageBox.Show("Connection Failed to Open!");
                return false;
            }
        }

        public bool UserExistsInDatabase(string userName)
        {
            int count = 0;

            string tableName = "userCredentials";
            string fields = "userName, userPassword";
            string options = $"WHERE userName='{userName}'";

            string sqlQuery = "SELECT " + fields + " FROM " + tableName + " " + options;

            var sqlCommand = new SQLiteCommand(sqlQuery, connection);
            var dataReader = sqlCommand.ExecuteReader();

            while(dataReader.Read())
            {
                count++;
            }

            if(count != 1)
            {
                return false;
            }

            dataReader.Close();

            return true;
        }

        public int InsertNewUser(pbUserInfo newUserInfo)
        {
            if (OpenConnection())
            {
                string tableName = "userCredentials";
                string fields = "userName, userPassword";
                string values = $"'{newUserInfo.UserName}', '{newUserInfo.Password}'";
                string sqlQuery = "INSERT INTO " + tableName + " (" + fields + ") VALUES (" + values + ")";
                var sqlCommand = new SQLiteCommand(sqlQuery, connection);
                sqlCommand.ExecuteNonQuery();

                var insertID = new SQLiteCommand(@"select last_insert_rowid()", connection);

                long rowID = (long)insertID.ExecuteScalar();

                return (int)rowID;
            }

            return -1;
        }

        public int InsertNewFriend(string username, string friendname)
        {
            Console.WriteLine("INSIDE INSERT NEW FRIEND");

            if(!UserExistsInDatabase(username))
            {
                Console.WriteLine("Can't add that user. He/She doesnt exist in DB");
                return -1;
            }

            if (OpenConnection())
            {
                string sqlQuery = $"INSERT INTO userFriends (userName,userFriend) VALUES('{username}','{friendname}')";

                var sqlCommand = new SQLiteCommand(sqlQuery, connection);
                sqlCommand.ExecuteNonQuery();

                var insertID = new SQLiteCommand(@"select last_insert_rowid()", connection);

                var rowID = (long)insertID.ExecuteScalar();

                return (int)rowID;
            }

            return -1;
        }

        public List<string> GetFriendsListFromDB(string username)
        {
            var friendsList = new List<string>();

            if (OpenConnection())
            {
                var sqlQuery = $"SELECT userFriend FROM userFriends WHERE userName='{username}'";

                if (!UserExistsInDatabase(username))
                {
                    return null;
                }

                friendsList = connection.Query<string>(sqlQuery).ToList<string>();
            }



            return friendsList;
        }

        public bool CheckUserLogin(string username, string password)
        {
            if (OpenConnection())
            {
                int count = 0;

                string tableName = "userCredentials";
                string fields = "userName, userPassword";
                string options = $"WHERE userName='{username}' AND userPassword='{password}'";

                string sqlQuery = "SELECT " + fields + " FROM " + tableName + " " + options;

                var sqlCommand = new SQLiteCommand(sqlQuery, connection);
                var dataReader = sqlCommand.ExecuteReader();


                if (dataReader != null)
                {
                    while (dataReader.Read())
                    {
                        count++;
                    }
                }

                if (count != 1)
                {
                    return false;
                }

                dataReader.Close();
            }

            return true;
        }

        /* Update Table Method
        * Updates the specified fields of the specified table using the specified options.
        * @param TableName - The name of the table to update
        * @param SetString - The SET clause of a SQLite update query
        * @param Options   - The options about the update query (ie WHERE ID=0)
        * @return A boolean value indicating the success of the update.
        */
        public bool UpdateTable(string TableName, string SetString, string Options)
        {
            if (OpenConnection())
            {
                // The connection is open and can be used for processing commands now
                try
                {
                    String sqlQuery = "UPDATE " + TableName + " " + SetString + " " + Options;
                    SQLiteCommand command = new SQLiteCommand(sqlQuery, connection);
                    command.ExecuteNonQuery();
                    return true;
                }
                catch (Exception e)
                {
                    if (System.Windows.Forms.MessageBox.Show("Error Updating the Tables:\n" + e.Message,
                        "Error Updating Table!", System.Windows.Forms.MessageBoxButtons.RetryCancel,
                        System.Windows.Forms.MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Retry)
                    {
                        return (UpdateTable(TableName, SetString, Options));
                    }
                    else
                        return false;
                }
            }
            else
            {
                // There was some sort of error opening the connection!
                System.Windows.Forms.MessageBox.Show("Connection Failed to Open!");
                return false;
            }
        }

        /* CloseConnection Method
        * Closes the DatabaseManager objects connection to the database. This should
        * be called whenever consecutive queries are finished to avoid memeory leaks.
        */
        public void CloseConnection()
        {
            if (connection != null && (connection.State != System.Data.ConnectionState.Closed))
                connection.Close();
        }

        /* Database Exists method
        * This method checks to see if the database file exists. It does not
        * actually verify the integrity of the database, but simply checks if the
        * database file exists.
        * @return A boolean indicating whether or not the database file exists
        */
        public bool DatabaseExists()
        {
            try
            {
                return (System.IO.File.Exists(DATABASE_FILE));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return false;
            }
        }
        /* CreateDatabase Method
        * This method creates the database file in the filesystem.
        * @return A boolean indicating the success of the creation of the database file.
        */
        public bool CreateDatabase()
        {
            try
            {
                // Create the database file
                SQLiteConnection.CreateFile(DATABASE_FILE);
                return true;
            }
            catch (Exception e)
            {
                // On error, give the user a chance to retry
                if (System.Windows.Forms.MessageBox.Show("Something went wrong while" +
                    " trying to create the database file!\n" + e.StackTrace,
                    "Error Creating Database File!", System.Windows.Forms.MessageBoxButtons.RetryCancel,
                    System.Windows.Forms.MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Retry)
                {
                    return CreateDatabase();
                }
                else
                    return false;
            }
        }
    }
}
