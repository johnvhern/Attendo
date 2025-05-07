using System;
using System.Data;
using System.Windows.Forms;
using OfficeOpenXml;
using System.IO;
using System.Data.SqlClient;
using System.Drawing;
using MetroFramework.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Security.Policy;


namespace Attendo.Screens
{
    public partial class Students : Form
    {
        OpenFileDialog openFileDialog;
        private string dbConnection = "Data Source=localhost\\sqlexpress;Initial Catalog=Attendo;Integrated Security=True;";
        private DataTable dataTable = new DataTable();
        public Students()
        {
            InitializeComponent();
            cbSearchBy.SelectedIndex = 0;

        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            using (openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel Files|*.xlsx;*.xls";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = openFileDialog.FileName;
                    PreviewExcelData(openFileDialog.FileName);
                }
            }
        }

        private void PreviewExcelData(string fileName)
        {
            try
            {
                using (var package = new ExcelPackage(new FileInfo(fileName)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var course = worksheet.Cells[8, 3].Text;

                    dataTable.Clear();
                    dataTable.Columns.Clear();

                    dataTable.Columns.Add("Course", typeof(string));
                    dataTable.Columns.Add("Student ID", typeof(string));
                    dataTable.Columns.Add("Name", typeof(string));

                    int startRow = 10;
                    for (int row = startRow; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var studentID = worksheet.Cells[row, 1].Text;
                        var studentName = worksheet.Cells[row, 3].Text;



                        if (string.IsNullOrWhiteSpace(worksheet.Cells[row, 1].Text) &&
                            string.IsNullOrWhiteSpace(worksheet.Cells[row, 2].Text) &&
                            string.IsNullOrWhiteSpace(studentID))
                        {
                            continue;
                        }

                        if (!IsDataAlreadySaved(studentID))
                        {
                           
                                dataTable.Rows.Add(
                                    course,
                                    studentID,
                                    studentName
                                );
                        }
                    }

                    if (dataTable.Rows.Count == 0)
                    {
                        MessageBox.Show("Data already saved. Please upload another excel file", "Already Saved Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        txtFilePath.Clear();
                        return;
                    }

                    dgvStudents.DataSource = dataTable;
                    dgvStudents.Tag = new { Course = course };
                    dgvStudents.ClearSelection();
                    dgvStudents.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                    HighlightMissingDetails();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void HighlightMissingDetails()
        {
            foreach (DataGridViewRow row in dgvStudents.Rows)
            {
                if (!row.IsNewRow)
                {
                    bool isDetailsEmpty =
                        (dataTable.Columns.Contains("Student ID") &&
                        string.IsNullOrWhiteSpace(row.Cells["Student ID"].Value?.ToString()) &&
                        string.IsNullOrWhiteSpace(row.Cells["Name"].Value?.ToString()));

                    if (isDetailsEmpty)
                    {
                        row.DefaultCellStyle.BackColor = Color.LightCoral;
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = Color.White;
                    }
                }
            }
        }

        private bool HasMissingClassification()
        {
            bool hasMissing = false;

            foreach (DataRow row in dataTable.Rows)
            {
                if (dataTable.Columns.Contains("Student ID"))
                {
                    bool isDetailsEmpty =
                        string.IsNullOrWhiteSpace(row["Student ID"].ToString()) &&
                        string.IsNullOrWhiteSpace(row["Name"].ToString());

                    if (isDetailsEmpty)
                    {
                        hasMissing = true;
                        break;
                    }
                }
               
            }

            return hasMissing;
        }

        private bool IsDataAlreadySaved(string studentID)
        {
            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                string query;

               
                // CHECK DATA FOR DUPLICATION OF STUDENTS
                    query = "SELECT COUNT(*) FROM tblStudents WHERE student_id = @studentid";
               

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@studentid", studentID);

                    conn.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {

                if (HasMissingClassification())
                {
                    MessageBox.Show("There is some students with missing details. Please complete the data before saving.",
                                    "Missing Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }


                if (string.IsNullOrWhiteSpace(txtFilePath.Text))
                {
                    MessageBox.Show("Please import an excel file first.");
                    return;
                }

                if (dataTable.Rows.Count == 0)
                {
                    MessageBox.Show("No data to save.", "Save Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SaveData(); // Call the existing SaveData method
                txtFilePath.Clear();
                dgvStudents.DataSource = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void SaveData()
        {
            if (dataTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to save.", "Save Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tableName = ((dynamic)dgvStudents.Tag).Course;

            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                foreach (DataRow row in dataTable.Rows)
                {
                    string checkQuery = $"SELECT COUNT(*) FROM tblStudents WHERE course = @course AND student_id = @studentid";

                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@course", ((dynamic)dgvStudents.Tag).Course);
                        checkCmd.Parameters.AddWithValue("@studentid", row["Student ID"]);

                        int count = (int)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            // If the learner exists, update their classification
                            string updateQuery = "UPDATE tblStudents SET student_id = @student_id AND student_name = @studentname AND course = @course";

                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@GradeLevel", ((dynamic)dgvStudents.Tag).Course);
                                updateCmd.Parameters.AddWithValue("@studentname", row["Name"]);
                                updateCmd.Parameters.AddWithValue("@student_id", row["Student ID"]);

                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // If the learner does not exist, insert a new record
                            string insertQuery = "INSERT INTO tblStudents (course, student_id, student_name) " +
                                  "VALUES (@course, @studentid, @studentname)";
                             

                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@course", ((dynamic)dgvStudents.Tag).Course);
                                insertCmd.Parameters.AddWithValue("@studentid", row["Student ID"]);
                                insertCmd.Parameters.AddWithValue("@studentname", row["Name"]);

                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }

            MessageBox.Show("Data saved successfully.", "Save Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //ActivityLogger logger = new ActivityLogger();

            //if (dataGridView1.Columns.Equals("RMA Classification"))
            //{
            //    logger.LogActivity(LoggedUser.Username, "Saved Learners Profile Data(Literacy_Numeracy)");
            //}
            //else
            //{
            //    logger.LogActivity(LoggedUser.Username, "Saved Learners Profile Data(Science_ERUNT)");
            //}


            dataTable.Clear();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                string column;
                string searchValue = txtSearchTerm.Text.Trim();

                if (string.IsNullOrEmpty(searchValue))
                {
                    MessageBox.Show("Please enter a search term.");
                    return;
                }

                string selected = cbSearchBy.SelectedItem?.ToString();

                switch (selected)
                {
                    case "Course":
                        column = "course";
                        break;
                    case "Name":
                        column = "student_name";
                        break;
                    default:
                        column = "student_id";
                        break;
                }

                using (SqlConnection conn = new SqlConnection(dbConnection)){
                        conn.Open();
                        string query = $"SELECT DISTINCT course, student_id, student_name FROM tblStudents WHERE {column} LIKE @SearchValue";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@SearchValue", $"%{searchValue}%");
                            DataTable dt = new DataTable();
                            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                            {
                                adapter.Fill(dt);
                            }
                            dgvStudents.DataSource = dt;
                            dgvStudents.Columns["student_id"].HeaderText = "Student ID";
                            dgvStudents.Columns["student_name"].HeaderText = "Name";
                            dgvStudents.Columns["course"].HeaderText = "Course";
                            dgvStudents.ClearSelection();
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured: {ex.Message}");
            }
        }
    }
}
