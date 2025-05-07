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
using System.Text.RegularExpressions;


namespace Attendo.Screens
{
    public partial class Students : Form
    {
        OpenFileDialog openFileDialog;
        private string dbConnection = "Data Source=localhost\\sqlexpress;Initial Catalog=Attendo;Integrated Security=True;";
        int selectedBoarderID;
        public Students()
        {
            InitializeComponent();
        }

        private void loadAllBoarders()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(dbConnection))
                {
                    conn.Open();
                    string loadBoarders = "SELECT id, course, student_id, student_name FROM tblStudents";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(loadBoarders, conn))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        // Assuming you have a DataGridView named dataGridView1
                        dgvStudents.DataSource = dt;
                        // Optionally, you can set the column headers
                        dgvStudents.Columns["id"].Visible = false;
                        dgvStudents.Columns["course"].HeaderText = "Course";
                        dgvStudents.Columns["student_id"].HeaderText = "Student ID";
                        dgvStudents.Columns["student_name"].HeaderText = "Name";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        public static class Validator
        {
            public static bool IsNameValid(string name) =>
                !string.IsNullOrWhiteSpace(name) && Regex.IsMatch(name, @"^[A-Za-z\s\-]+$");
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtCourse.Text))
            {
                MessageBox.Show("Course is required.", "Invalid Entry", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCourse.Focus();
                return;
            }

            if (string.IsNullOrEmpty(txtStudentID.Text))
            {
                MessageBox.Show("Student ID is required.", "Invalid Entry", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCourse.Focus();
                return;
            }

            if (!Validator.IsNameValid(txtName.Text))
            {
                MessageBox.Show("Name is required.", "Invalid Entry", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtCourse.Focus();
                return;
            }

            string course = txtCourse.Text;
            string studentID = txtStudentID.Text;
            string name = txtName.Text;


        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Select Picture";
                dialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    picID.Image = Image.FromFile(dialog.FileName);
                }
            }
        }

        private void dgvStudents_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0)
                {
                    DataGridViewRow row = dgvStudents.Rows[e.RowIndex];
                    selectedBoarderID = Convert.ToInt32(row.Cells["id"].Value);
                    string name = row.Cells["student_name"].Value.ToString();
                    string course = row.Cells["course"].Value.ToString();
                    string lastName = row.Cells["student_id"].Value.ToString();
                    txtCourse.Text = course;
                    txtStudentID.Text = lastName;
                    txtName.Text = name;
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
