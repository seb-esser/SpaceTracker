using System;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Diagnostics;

namespace SpaceTracker
{
    public class SQLiteConnector
    {
        private string _path = @"Data Source=C:\sqlite_tmp\RoomWallWindow_sample.db;Version=3;New=False;Compress=True;";
        private bool _blockExecution = true; 
        public SQLiteConnector()
        {

        }

        /// <summary>
        /// simple method to send queries to the database
        /// </summary>
        /// <param name="query"></param>
        public void runSQLQuery(string query)
        {
            if (_blockExecution)
            {
                return;
            }
            try
            {
                SQLiteConnection sqlite_conn = new SQLiteConnection(_path);
                string stringQuery = query;

                sqlite_conn.Open();//Open the SqliteConnection
                var SqliteCmd = new SQLiteCommand();//Initialize the SqliteCommand
                SqliteCmd = sqlite_conn.CreateCommand();//Create the SqliteCommand
                SqliteCmd.CommandText = stringQuery;//Assigning the query to CommandText
                SqliteCmd.ExecuteNonQuery();//Execute the SqliteCommand
                sqlite_conn.Close();//Close the SqliteConnection
                return; 
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return;
            }
            
        }
    }
}