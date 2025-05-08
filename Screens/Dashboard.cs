using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.TextFormatting;

namespace Attendo.Screens
{
    public partial class Dashboard : Form
    {
        private string dbConnection = "Data Source=localhost\\sqlexpress;Initial Catalog=Attendo;Integrated Security=True;";
        public Dashboard()
        {
            InitializeComponent();
        }

        private void Dashboard_Load(object sender, EventArgs e)
        {
            lbldateTime.Text = "TODAY IS " + DateTime.Now.ToString("dddd, MMMM dd, yyyy hh:mm tt");
            SessionManager sessionManager = new SessionManager();
            DataTable activeSession = sessionManager.GetActiveSession();

            if (activeSession.Rows.Count > 0)
            {
                // Assuming your label is named lblActiveSession
                string sessionName = activeSession.Rows[0]["SessionName"].ToString();
                string startTime = activeSession.Rows[0]["StartTime"].ToString();
                label2.Text = $"Active Session: {sessionName} (Started: {startTime})";
            }
            else
            {
                label2.Text = "No active session.";
            }

            txtScanInput.TabStop = false;
            txtScanInput.Focus();
        }

        private void ProcessScan(string scannedData)
        {
            try
            {
                string[] parts = scannedData.Split('|');
                if (parts.Length != 3)
                {
                    MessageBox.Show("Invalid QR format.");
                    return;
                }

                string studentID = parts[0];
                string name = parts[1];
                string course = parts[2];

                // Look up in database
                using (SqlConnection conn = new SqlConnection(dbConnection))
                {
                    conn.Open();
                    string query = "SELECT id, student_name, course, photopath FROM tblStudents WHERE student_id = @studentID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@studentID", studentID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int studentDbID = reader.GetInt32(0);
                                string studentName = reader.GetString(1);
                                string studentCourse = reader.GetString(2);
                                string photoPath = reader.GetString(3);

                                // Display info on dashboard
                                lblstudentNumber.Text = studentID;
                                lblstudentName.Text = studentName;
                                lblstudentCourse.Text = studentCourse;
                                picID.Image = Image.FromFile(photoPath);

                                // Check attendance
                                RecordAttendance(studentDbID);
                            }
                            else
                            {
                                MessageBox.Show("Student not found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void RecordAttendance(int studentDbID)
        {
            int sessionId = GetActiveSessionID();

            if (sessionId == -1)
            {
                MessageBox.Show("No active session found.");
                return;
            }

            if (HasAlreadyScanned(studentDbID.ToString(), sessionId))
            {
                MessageBox.Show("Attendance already recorded for this session.");
                return;
            }

            DateTime now = DateTime.Now;
            DateTime cutoff = GetSessionCutoffTime(sessionId);
            string status = (now <= cutoff) ? "IN" : "LATE";

            InsertAttendance(studentDbID.ToString(), sessionId, now, status);

            // Optional: Display on UI
            lbldateTime.Text = "TODAY IS " + now.ToString("dddd, MMMM dd, yyyy hh:mm tt");
            lblstudentStatus.Text = status;
            lblstudentStatus.BackColor = (status == "LATE") ? Color.OrangeRed : Color.Green;
        }

        private void InsertAttendance(string studentID, int sessionId, DateTime scanTime, string status)
        {
            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = "INSERT INTO tblAttendance (student_id, session_id, scan_time, status) VALUES (@sid, @sess, @time, @status)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    cmd.Parameters.AddWithValue("@sess", sessionId);
                    cmd.Parameters.AddWithValue("@time", scanTime);
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private bool HasAlreadyScanned(string studentID, int sessionId)
        {
            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM tblAttendance WHERE student_id = @studentID AND session_id = @sessionId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@studentID", studentID);
                    cmd.Parameters.AddWithValue("@sessionId", sessionId);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        private DateTime GetSessionCutoffTime(int sessionId)
        {
            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = "SELECT cutofftime FROM tblSessions WHERE sessionid = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", sessionId);
                    return Convert.ToDateTime(cmd.ExecuteScalar());
                }
            }
        }

        private int GetActiveSessionID()
        {
            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = "SELECT TOP 1 sessionid FROM tblSessions WHERE isactive = 1";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : -1;
                }
            }
        }

        private void txtScanInput_TextChanged(object sender, EventArgs e)
        {
            scannTimer.Stop();
            scannTimer.Start(); // restart timer on each change
        }

        private void scannTimer_Tick(object sender, EventArgs e)
        {
            scannTimer.Stop();
            string scannedData = txtScanInput.Text.Trim();
            if (!string.IsNullOrEmpty(scannedData))
            {
                ProcessScan(scannedData);
                txtScanInput.Clear();
                txtScanInput.Focus();
            }
        }
    }
}
