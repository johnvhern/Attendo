using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendo
{
    public class SessionManager
    {
        private string dbConnection = "Data Source=localhost\\sqlexpress;Initial Catalog=Attendo;Integrated Security=True;";
        // Create a new session
        public void CreateSession(string sessionName, DateTime startTime, DateTime cutoffTime)
        {
            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = "INSERT INTO tblSessions (sessionname, starttime, cutofftime, isactive) VALUES (@sessionName, @startTime, @cutoffTime, 0)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@sessionName", sessionName);
                    cmd.Parameters.AddWithValue("@startTime", startTime);
                    cmd.Parameters.AddWithValue("@cutoffTime", cutoffTime);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Edit an existing session
        public void EditSession(int sessionId, string sessionName, DateTime startTime, DateTime cutoffTime)
        {
            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = "UPDATE tblSessions SET sessionname = @sessionName, starttime= @startTime, cutofftime = @cutoffTime WHERE sessionid = @sessionId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@sessionName", sessionName);
                    cmd.Parameters.AddWithValue("@startTime", startTime);
                    cmd.Parameters.AddWithValue("@cutoffTime", cutoffTime);
                    cmd.Parameters.AddWithValue("@sessionId", sessionId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Close (deactivate) the current active session
        public void CloseSession(int sessionId)
        {
            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = "UPDATE tblSessions SET isactive = 0 WHERE sessionid = @sessionId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@sessionId", sessionId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Get the current active session
        public DataTable GetActiveSession()
        {
            DataTable sessionTable = new DataTable();
            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = "SELECT * FROM tblSessions WHERE isactive = 1";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                {
                    adapter.Fill(sessionTable);
                }
            }
            return sessionTable;
        }

        public void StartSession(int sessionId)
        {
            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();

                // Set all sessions to inactive
                string deactivateAll = "UPDATE tblSessions SET isactive = 0";
                using (SqlCommand cmdDeactivate = new SqlCommand(deactivateAll, conn))
                {
                    cmdDeactivate.ExecuteNonQuery();
                }

                // Activate the selected session
                string activateSession = "UPDATE tblSessions SET isactive = 1, starttime = @now WHERE sessionid = @id";
                using (SqlCommand cmdActivate = new SqlCommand(activateSession, conn))
                {
                    cmdActivate.Parameters.AddWithValue("@id", sessionId);
                    cmdActivate.Parameters.AddWithValue("@now", DateTime.Now);
                    cmdActivate.ExecuteNonQuery();
                }
            }
        }

        public void DeleteSession(int sessionId)
        {
            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = "DELETE FROM tblSessions WHERE sessionid = @sessionId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@sessionId", sessionId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
