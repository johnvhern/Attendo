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
using System.Media;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.TextFormatting;

namespace Attendo.Screens
{
    public partial class Dashboard : Form
    {
        private int repeatedKeyCount = 0;
        private char lastKeyChar;
        private string dbConnection = "Data Source=localhost\\sqlexpress;Initial Catalog=Attendo;Integrated Security=True;";
        public Dashboard()
        {
            InitializeComponent();
        }

        private void Dashboard_Load(object sender, EventArgs e)
        {
            lbldateTime.Text = "Today is " + DateTime.Now.ToString("dddd, MMMM dd, yyyy hh:mm tt");
            SessionManager sessionManager = new SessionManager();
            DataTable activeSession = sessionManager.GetActiveSession();
            LoadScannedStudents();

            if (activeSession.Rows.Count > 0)
            {
                // Assuming your label is named lblActiveSession
                string sessionName = activeSession.Rows[0]["SessionName"].ToString();
                string startTime = activeSession.Rows[0]["StartTime"].ToString();
                string cutOffTime = activeSession.Rows[0]["CutoffTime"].ToString();
                label2.Text = $"Active Session: {sessionName} (Started: {startTime}) (End: {cutOffTime})";
            }
            else
            {
                label2.Text = "No active session.";
            }

            txtScanInput.TabStop = false;
            txtScanInput.Focus();
        }

        private void PlaySound(string filename)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", filename);
                using (SoundPlayer player = new SoundPlayer(path))
                {
                    player.Play(); // or player.PlaySync() if you want it to block
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sound error: " + ex.Message);
            }
        }

        private void LoadScannedStudents()
        {
            int sessionID = GetActiveSessionID();
            if (sessionID == -1)
            {
                MessageBox.Show("No active session.");
                return;
            }

            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = @"
            SELECT 
                s.student_id AS [Student ID],
                s.student_name AS [Name],
                s.course AS [Course],
                a.scan_time AS [Scan Time],
                a.status AS [Status]
            FROM tblAttendance a
            JOIN tblStudents s ON a.student_id = s.id
            WHERE a.session_id = @sessionID
            ORDER BY a.scan_time DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@sessionID", sessionID);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    dgvAttendanceLog.DataSource = table; // your DataGridView name
                }

                dgvAttendanceLog.Columns["Scan Time"].DefaultCellStyle.Format = "MMM dd, yyyy hh:mm tt";
                dgvAttendanceLog.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvAttendanceLog.DefaultCellStyle.SelectionBackColor = dgvAttendanceLog.DefaultCellStyle.BackColor;
                dgvAttendanceLog.DefaultCellStyle.SelectionForeColor = dgvAttendanceLog.DefaultCellStyle.ForeColor;
            }
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
                                LoadScannedStudents(); 
                            }
                            else
                            {
                                PlaySound("error.wav");
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
                PlaySound("error.wav");
                MessageBox.Show("No active session found.");
                return;
            }

            if (HasAlreadyScanned(studentDbID.ToString(), sessionId))
            {
                PlaySound("error.wav");
                MessageBox.Show("Attendance already recorded for this session.");
                return;
            }

            DateTime now = DateTime.Now;
            DateTime cutoff = GetSessionCutoffTime(sessionId);
            string status = (now <= cutoff) ? "IN" : "LATE";

            PlaySound(status == "IN" ? "success.wav" : "late.wav");
            InsertAttendance(studentDbID.ToString(), sessionId, now, status);
            
            // Optional: Display on UI
            lbldateTime.Text = "Today is " + now.ToString("dddd, MMMM dd, yyyy hh:mm tt");
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

                    LoadScannedStudents(); // Refresh the DataGridView
                    LoadRecentScannedStudents(); // Refresh the recent scans
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

        private void dgvAttendanceLog_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvAttendanceLog.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
            {
                string status = e.Value.ToString();

                if (status.Equals("LATE", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Color.Red;
                    e.CellStyle.ForeColor = Color.White;
                }
                else if (status.Equals("IN", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Color.Green;
                    e.CellStyle.ForeColor = Color.White;
                }
            }
        }

        private void dgvAttendanceLog_SelectionChanged(object sender, EventArgs e)
        {
            dgvAttendanceLog.ClearSelection();
        }

        private void btnResetInput_Click(object sender, EventArgs e)
        {
            txtScanInput.Text = "";
            txtScanInput.Focus();  // Re-focus the scanner input

            // Play a short beep as feedback
            System.Media.SystemSounds.Beep.Play();

            MessageBox.Show("Scanner input has been reset. Ready to scan again.");
        }

        private void txtScanInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == lastKeyChar)
            {
                repeatedKeyCount++;
                if (repeatedKeyCount > 10) // too many repeated characters
                {
                    btnResetInput.PerformClick(); // auto reset
                }
            }
            else
            {
                lastKeyChar = e.KeyChar;
                repeatedKeyCount = 1;
            }
        }

        private void LoadRecentScannedStudents()
        {
            flowRecentScans.Controls.Clear();

            int activeSessionId = GetActiveSessionID();
            if (activeSessionId == -1)
            {
                MessageBox.Show("No active session found.");
                return;
            }

            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = @"
            SELECT TOP 3 
                s.student_id, s.student_name, s.course, s.photopath,
                a.scan_time, a.status
            FROM tblAttendance a
            INNER JOIN tblStudents s ON a.student_id = s.id
            WHERE a.session_id = @sessionId
            ORDER BY a.scan_time DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@sessionId", activeSessionId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string studentId = reader["student_id"].ToString();
                            string name = reader["student_name"].ToString();
                            string course = reader["course"].ToString();
                            DateTime scanTime = Convert.ToDateTime(reader["scan_time"]);
                            string status = reader["status"].ToString();
                            string photoPath = reader["photopath"].ToString();

                            Panel card = CreateStudentCard(studentId, name, course, scanTime, status, photoPath);
                            flowRecentScans.Controls.Add(card);
                        }
                    }
                }
            }
        }


        private Panel CreateStudentCard(string studentId, string name, string course, DateTime scanTime, string status, string photoPath)
        {
            Panel card = new Panel
            {
                Width = flowRecentScans.Width - 10,
                Height = 130,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5),
                BackColor = Color.White
            };

            // PictureBox
            PictureBox pic = new PictureBox
            {
                Width = 100,
                Height = 100,
                Left = 10,
                Top = (card.Height - 100) / 2,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };
            if (File.Exists(photoPath))
                pic.Image = Image.FromFile(photoPath);

            // Base left for labels
            int labelLeft = 120;
            int labelWidth = card.Width - labelLeft - 10;
            int topSpacing = 15;

            Label lblName = new Label
            {
                Text = $"Name: {name}",
                Left = labelLeft,
                Top = topSpacing,
                Width = labelWidth,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            Label lblCourse = new Label
            {
                Text = $"Course: {course}",
                Left = labelLeft,
                Top = lblName.Bottom + 5,
                Width = labelWidth
            };

            Label lblTime = new Label
            {
                Text = $"Time: {scanTime.ToString("g")}",
                Left = labelLeft,
                Top = lblCourse.Bottom + 5,
                Width = labelWidth
            };

            Label lblStatus = new Label
            {
                Text = $"Status: {status}",
                Left = labelLeft,
                Top = lblTime.Bottom + 5,
                Width = labelWidth,
                ForeColor = status == "IN" ? Color.Green : Color.OrangeRed
            };

            // Add to panel
            card.Controls.Add(pic);
            card.Controls.Add(lblName);
            card.Controls.Add(lblCourse);
            card.Controls.Add(lblTime);
            card.Controls.Add(lblStatus);

            return card;

        }


    }
}
