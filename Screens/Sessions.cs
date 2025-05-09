using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Attendo.Screens
{
    public partial class Sessions : Form
    {
        private string dbConnection = "Data Source=localhost\\sqlexpress;Initial Catalog=Attendo;Integrated Security=True;";
        public Sessions()
        {
            InitializeComponent();
        }
        private void LoadSessions()
        {
            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = "SELECT sessionid, sessionname, starttime, cutofftime, CASE WHEN isactive = 1 THEN 'Yes' ELSE 'No' END AS isactive FROM tblSessions";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable sessionTable = new DataTable();
                    adapter.Fill(sessionTable);
                    dgvSessions.DataSource = sessionTable;
                    dgvSessions.Columns["sessionid"].Visible = false;
                    dgvSessions.Columns["sessionname"].HeaderText = "Session Name";
                    dgvSessions.Columns["starttime"].HeaderText = "Start Time";
                    dgvSessions.Columns["cutofftime"].HeaderText = "Cutoff Time";
                    dgvSessions.Columns["isactive"].HeaderText = "Active";

                    dgvSessions.Columns["starttime"].DefaultCellStyle.Format = "MMM dd, yyyy hh:mm:ss tt";
                    dgvSessions.Columns["cutofftime"].DefaultCellStyle.Format = "MMM dd, yyyy hh:mm:ss tt";

                }
            }
        }

        private void Sessions_Load(object sender, EventArgs e)
        {
            txtSearchBox.Text = "Search...";
            txtSearchBox.ForeColor = Color.Gray;
            startTimeDT.Format = DateTimePickerFormat.Custom;
            startTimeDT.CustomFormat = "MMMM dd, yyyy hh:mm tt";
            cutoffDT.Format = DateTimePickerFormat.Custom;
            cutoffDT.CustomFormat = "MMMM dd, yyyy hh:mm tt";
            LoadSessions();
            dgvSessions.CellFormatting += dgvSessions_CellFormatting;
            dgvSessions.ClearSelection();
        }

        private void dgvSessions_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            
            if (dgvSessions.Columns[e.ColumnIndex].Name == "isactive" && e.Value != null)
            {
                string status = e.Value.ToString();

                if (status.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Color.Green;
                    e.CellStyle.ForeColor = Color.White;
                }
                else if (status.Equals("No", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Color.Red;
                    e.CellStyle.ForeColor = Color.White;
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSessionName.Text))
            {
                MessageBox.Show("Please enter a session name.");
                return;
            }

            string sessionName = txtSessionName.Text;
            DateTime startTime = startTimeDT.Value;
            DateTime cutoffTime = cutoffDT.Value;

            SessionManager sessionManager = new SessionManager();
            sessionManager.CreateSession(sessionName, startTime, cutoffTime);

            MessageBox.Show("Session created successfully!");
            LoadSessions();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            // Get the current active session
            SessionManager sessionManager = new SessionManager();
            DataTable activeSession = sessionManager.GetActiveSession();

            if (activeSession.Rows.Count > 0)
            {
                int sessionId = Convert.ToInt32(activeSession.Rows[0]["sessionid"]);
                sessionManager.CloseSession(sessionId);
                MessageBox.Show("Session closed successfully!");
            }
            else
            {
                MessageBox.Show("No active session to close.");
            }
            LoadSessions();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            SessionManager sessionManager = new SessionManager();

            if (dgvSessions.SelectedRows.Count > 0)
            {
                int sessionId = Convert.ToInt32(dgvSessions.SelectedRows[0].Cells["sessionid"].Value);
                sessionManager.StartSession(sessionId);
                MessageBox.Show("Session is now active.");
            }
            else
            {
                MessageBox.Show("Please select a session to start.");
            }
            LoadSessions();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            string sessionName = txtSessionName.Text;
            DateTime startTime = startTimeDT.Value;
            DateTime cutoffTime = cutoffDT.Value;

            SessionManager sessionManager = new SessionManager();
            if (dgvSessions.SelectedRows.Count > 0)
            {
                int sessionId = Convert.ToInt32(dgvSessions.SelectedRows[0].Cells["sessionid"].Value);
                sessionManager.EditSession(sessionId, sessionName, startTime, cutoffTime);
                MessageBox.Show("Session successfully updated");
            }
            else
            {
                MessageBox.Show("Please select a session to edit.");
            }
            LoadSessions();
        }

        private void dgvSessions_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvSessions.SelectedRows.Count > 0)
            {
                txtSessionName.Text = dgvSessions.SelectedRows[0].Cells["sessionname"].Value.ToString();
                startTimeDT.Value = Convert.ToDateTime(dgvSessions.SelectedRows[0].Cells["starttime"].Value);
                cutoffDT.Value = Convert.ToDateTime(dgvSessions.SelectedRows[0].Cells["cutofftime"].Value);
            }
        }

        private void txtSearchBox_Enter(object sender, EventArgs e)
        {
            if (txtSearchBox.Text == "Search...")
            {
                txtSearchBox.Text = "";
                txtSearchBox.ForeColor = Color.Black;
            }
        }

        private void txtSearchBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearchBox.Text))
            {
                txtSearchBox.Text = "Search...";
                txtSearchBox.ForeColor = Color.Gray;
            }
        }

        private void txtSearchBox_TextChanged(object sender, EventArgs e)
        {
            if (txtSearchBox.Text == "Search...") return;

            string filterText = txtSearchBox.Text.Trim().Replace("'", "''"); // avoid SQL issues
            (dgvSessions.DataSource as DataTable).DefaultView.RowFilter =
                $"sessionname LIKE '%{filterText}%'";
        }
    }
}
