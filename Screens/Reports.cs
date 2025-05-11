using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Attendo.Screens
{
    public partial class Reports : Form
    {
        private string sessionDate = "";
        private PrintDocument printDocument = new PrintDocument();
        private DataTable printTable;
        private int currentRow = 0;
        private int rowHeight = 30;
        private Font titleFont = new Font("Arial", 9, FontStyle.Bold);
        private Font printFont = new Font("Arial", 9);
        private Font headerFont = new Font("Arial", 9, FontStyle.Bold);
        private string selectedSession = "";
        private string selectedCourse = "";
        private string sessionCutOffTime = "";
        private string dbConnection = "Data Source=localhost\\sqlexpress;Initial Catalog=Attendo;Integrated Security=True;";
        public Reports()
        {
            InitializeComponent();
        }

        private void Reports_Load(object sender, EventArgs e)
        {
            txtSearchbox.Text = "Search...";
            txtSearchbox.ForeColor = Color.Gray;
        }

        private void dateTimeReport_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                cbSessions.Items.Clear(); // Clear previous items
                cbCourse.Items.Clear(); // Clear previous items

                string selectedDate = dateTimeReport.Value.ToString("yyyy-MM-dd");

                using (SqlConnection conn = new SqlConnection(dbConnection))
                {
                    conn.Open();
                    string loadSession = "SELECT sessionname FROM tblSessions WHERE CONVERT(date, cutofftime) = @date";

                    using (SqlCommand cmd = new SqlCommand(loadSession, conn))
                    {
                        cmd.Parameters.AddWithValue("@date", selectedDate);
                        SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string sessionName = reader["sessionname"].ToString();
                                cbSessions.Items.Add(sessionName);
                            }
                        }
                        else
                        {
                            MessageBox.Show("No sessions found for the selected date.", "No Sessions", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while selecting the date: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cbSessions_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                cbCourse.Items.Clear(); // Clear previous items
                using (SqlConnection conn = new SqlConnection(dbConnection))
                {
                    conn.Open();
                    string loadCourse = "SELECT DISTINCT s.course FROM tblStudents s " +
                        "JOIN tblAttendance a ON s.id = a.student_id " +
                        "JOIN tblSessions se ON a.session_id = se.sessionid " +
                        "WHERE se.sessionname = @sessionname";

                    using (SqlCommand cmd = new SqlCommand(loadCourse, conn))
                    {
                        cmd.Parameters.AddWithValue("@sessionname", cbSessions.SelectedItem.ToString());
                        SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string courseName = reader["course"].ToString();
                                cbCourse.Items.Add(courseName);
                            }
                        }
                        else
                        {
                            MessageBox.Show("No courses found for the selected session.", "No Courses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while selecting the session: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (cbSessions.SelectedIndex == -1 || cbCourse.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a session and a course.", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                using (SqlConnection conn = new SqlConnection(dbConnection)){

                    conn.Open();
                    string loadReport = @"
                                        SELECT 
                                            s.course,
                                            s.student_id,
                                            s.student_name,
                                            a.scan_time,
                                            ISNULL(a.status, 'ABSENT') AS status
                                        FROM tblStudents s
                                        LEFT JOIN tblAttendance a 
                                            ON s.id = a.student_id AND a.session_id = (
                                                SELECT sessionid FROM tblSessions WHERE sessionname = @sessionname
                                            )
                                        WHERE s.course = @course";


                    using (SqlCommand cmd = new SqlCommand(loadReport, conn)){

                        cmd.Parameters.AddWithValue("@sessionname", cbSessions.SelectedItem.ToString());
                        cmd.Parameters.AddWithValue("@course", cbCourse.SelectedItem.ToString());
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dataGridView1.DataSource = dt;
                        dataGridView1.Columns["course"].HeaderText = "Course";
                        dataGridView1.Columns["student_id"].HeaderText = "Student ID";
                        dataGridView1.Columns["student_name"].HeaderText = "Student Name";
                        dataGridView1.Columns["scan_time"].HeaderText = "Scan Time";
                        dataGridView1.Columns["status"].HeaderText = "Status";
                        dataGridView1.Columns["scan_time"].DefaultCellStyle.Format = "MMMM dd, yyyy hh:mm:ss tt";
                        dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                        dataGridView1.DefaultCellStyle.SelectionBackColor = dataGridView1.DefaultCellStyle.BackColor;
                        dataGridView1.DefaultCellStyle.SelectionForeColor = dataGridView1.DefaultCellStyle.ForeColor;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading the report: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "status" && e.Value != null)
            {
                string status = e.Value.ToString();

                if (status.Equals("LATE", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Color.Red;
                    e.CellStyle.ForeColor = Color.Black;
                }
                else if (status.Equals("IN", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Color.Green;
                    e.CellStyle.ForeColor = Color.Black;
                }
                else if (status.Equals("ABSENT", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = Color.Yellow;
                    e.CellStyle.ForeColor = Color.Black;
                }
            }
        }

        private void txtSearchbox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearchbox.Text))
            {
                txtSearchbox.Text = "Search...";
                txtSearchbox.ForeColor = Color.Gray;
            }
        }

        private void txtSearchbox_TextChanged(object sender, EventArgs e)
        {
            if (txtSearchbox.Text == "Search...") return;

            string filterText = txtSearchbox.Text.Trim().Replace("'", "''"); // avoid SQL issues
            (dataGridView1.DataSource as DataTable).DefaultView.RowFilter =
                $"student_name LIKE '%{filterText}%' OR student_id LIKE '%{filterText}%' OR course LIKE '%{filterText}%'";
        }

        private void txtSearchbox_Enter(object sender, EventArgs e)
        {
            if (txtSearchbox.Text == "Search...")
            {
                txtSearchbox.Text = "";
                txtSearchbox.ForeColor = Color.Black;
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            dataGridView1.ClearSelection();
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (dataGridView1.DataSource is DataTable dt)
            {
                printTable = dt;
                currentRow = 0;
                selectedSession = cbSessions.SelectedItem?.ToString() ?? "";
                selectedCourse = cbCourse.SelectedItem?.ToString() ?? "";

                using (SqlConnection conn = new SqlConnection(dbConnection))
                {
                    conn.Open();
                    string query = "SELECT cutofftime FROM tblSessions WHERE sessionname = @sessionname";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@sessionname", selectedSession);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            sessionDate = Convert.ToDateTime(result).ToString("MMMM dd, yyyy");
                            sessionCutOffTime = Convert.ToDateTime(result).ToString("hh:mm:ss tt");
                        }
                        else
                        {
                            sessionDate = "Unknown Date";
                        }
                    }
                }

                // Create the PrintDocument
                printDocument = new PrintDocument();
                printDocument.DefaultPageSettings.Landscape = true;
                printDocument.PrintPage += PrintDocument_PrintPage;

                // Show the PrintPreviewDialog in your custom form with PrintPreviewControl
                ReportPreview reportPreviewForm = new ReportPreview(printDocument,printTable,selectedSession,selectedCourse,sessionDate, sessionCutOffTime);

                reportPreviewForm.ShowPrintPreview();
            }
            else
            {
                MessageBox.Show("No data to print.");
            }
        }



        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            int marginLeft = e.MarginBounds.Left;
            int marginTop = e.MarginBounds.Top;
            int offsetY = 50;

            int[] columnWidths = new int[printTable.Columns.Count];
            int totalWidth = e.MarginBounds.Width;

            // Basic column width logic (divide evenly)
            for (int i = 0; i < printTable.Columns.Count; i++)
            {
                columnWidths[i] = totalWidth / printTable.Columns.Count;
            }

            // Print header information
            string title = $"Attendance Report";
            string subtitle = $"Session: {selectedSession}    Course: {selectedCourse}";
            string date = $"Date: {sessionDate}     Cut Off Time: {sessionCutOffTime}";

            e.Graphics.DrawString(title, new Font("Arial", 16, FontStyle.Bold), Brushes.Black, marginLeft, offsetY);
            offsetY += 35;

            e.Graphics.DrawString(subtitle, titleFont, Brushes.Black, marginLeft, offsetY);
            offsetY += 25;

            e.Graphics.DrawString(date, titleFont, Brushes.Black, marginLeft, offsetY);
            offsetY += 40;

            StringFormat headerFormat = new StringFormat();
            headerFormat.Alignment = StringAlignment.Center;
            headerFormat.LineAlignment = StringAlignment.Center;

            // Column headers
            int x = marginLeft;
            for (int i = 0; i < printTable.Columns.Count; i++)
            {
                string headerText;

                switch (printTable.Columns[i].ColumnName)
                {
                    case "course":
                        headerText = "Course";
                        break;
                    case "student_id":
                        headerText = "Student ID";
                        break;
                    case "student_name":
                        headerText = "Student Name";
                        break;
                    case "scan_time":
                        headerText = "Scan Time";
                        break;
                    case "status":
                        headerText = "Status";
                        break;
                    default:
                        headerText = printTable.Columns[i].ColumnName;
                        break;
                }

                // Draw the header cell with the custom or default text
                e.Graphics.FillRectangle(Brushes.LightGray, x, offsetY, columnWidths[i], rowHeight);
                e.Graphics.DrawRectangle(Pens.Black, x, offsetY, columnWidths[i], rowHeight);
                e.Graphics.DrawString(headerText, headerFont, Brushes.Black, new RectangleF(x, offsetY, columnWidths[i], rowHeight), headerFormat);

                x += columnWidths[i];
            }
            offsetY += rowHeight;

            // Data rows
            while (currentRow < printTable.Rows.Count)
            {
                x = marginLeft;
                for (int i = 0; i < printTable.Columns.Count; i++)
                {
                    string value = printTable.Rows[currentRow][i]?.ToString() ?? "";

                    Rectangle cellRect = new Rectangle(x, offsetY, columnWidths[i], rowHeight);

                    // Check if current column is "Status"
                    if (printTable.Columns[i].ColumnName == "status")
                    {
                        string status = value.ToUpper();
                        Brush backgroundBrush = Brushes.White;
                        Brush textBrush = Brushes.Black;

                        if (status == "IN")
                        {
                            backgroundBrush = Brushes.Green;
                            textBrush = Brushes.Black;
                        }
                        else if (status == "LATE")
                        {
                            backgroundBrush = Brushes.Red;
                            textBrush = Brushes.Black;
                        }
                        else if (status == "ABSENT")
                        {
                            backgroundBrush = Brushes.Yellow;
                            textBrush = Brushes.Black;
                        }

                            // Fill background and draw text
                            e.Graphics.FillRectangle(backgroundBrush, cellRect);
                        e.Graphics.DrawRectangle(Pens.Black, cellRect);
                        e.Graphics.DrawString(value, printFont, textBrush, cellRect);
                    }
                    else
                    {
                        // Default drawing for other cells
                        e.Graphics.DrawRectangle(Pens.Black, cellRect);
                        e.Graphics.DrawString(value, printFont, Brushes.Black, cellRect);
                    }

                    x += columnWidths[i];
                }

                offsetY += rowHeight;
                currentRow++;

                if (offsetY + rowHeight > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }
            }

            // Reset after finishing
            currentRow = 0;
            e.HasMorePages = false;
            printDocument.PrintPage -= PrintDocument_PrintPage; // clean up handler
        }
    }
}
