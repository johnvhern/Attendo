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
using System.Diagnostics;
using QRCoder;


namespace Attendo.Screens
{
    public partial class Students : Form
    {
        private string dbConnection = "Data Source=localhost\\sqlexpress;Initial Catalog=Attendo;Integrated Security=True;";
        int selectedStudentID;
        string selectedImagePath = "";
        public Students()
        {
            InitializeComponent();
            loadAllBoarders();
        }

        private void loadAllBoarders()
        {
            try
            {
                string query = "SELECT id, course, student_id, student_name, photopath FROM tblStudents";

                using (SqlConnection conn = new SqlConnection(dbConnection))
                {
                    conn.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dgvStudents.DataSource = dt;
                    dgvStudents.Columns["id"].Visible = false;
                    dgvStudents.Columns["course"].HeaderText = "Course";
                    dgvStudents.Columns["student_id"].HeaderText = "Student ID";
                    dgvStudents.Columns["student_name"].HeaderText = "Name";
                    dgvStudents.Columns["photopath"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        public Bitmap GenerateQRCode(string content)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            return qrCodeImage;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
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

                if (string.IsNullOrEmpty(txtName.Text))
                {
                    MessageBox.Show("Name is required.", "Invalid Entry", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtCourse.Focus();
                    return;
                }

                if (picID.Image.Equals(null))
                {
                    MessageBox.Show("Please upload a photo.", "Invalid Entry", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string course = txtCourse.Text.Trim();
                string studentID = txtStudentID.Text.Trim();
                string name = txtName.Text.Trim();

                // Ensure Photos folder exists
                string photoDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos");
                if (!Directory.Exists(photoDir)) Directory.CreateDirectory(photoDir);

                string newPhotoPath = Path.Combine(photoDir, studentID + Path.GetExtension(selectedImagePath));

                File.Copy(selectedImagePath, newPhotoPath, true);

                using (SqlConnection con = new SqlConnection(dbConnection))
                {
                    string query = "INSERT INTO tblStudents (course, student_id, student_name, photopath) VALUES (@course, @studentid, @name, @photo)";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@studentid", studentID);
                    cmd.Parameters.AddWithValue("@course", course);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@photo", newPhotoPath);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }

                string qrContent = studentID + "|" + name + "|" + course;
                Bitmap qrBitmap = GenerateQRCode(qrContent);

                // Save QR to disk
                string qrPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QR", studentID + ".png");
                if (!Directory.Exists(Path.GetDirectoryName(qrPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(qrPath));
                }
                qrBitmap.Save(qrPath, System.Drawing.Imaging.ImageFormat.Png);


                loadAllBoarders();
                MessageBox.Show("Student added successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);

            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Select Picture";
                dialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selectedImagePath = dialog.FileName;
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
                    selectedStudentID = Convert.ToInt32(row.Cells["id"].Value);
                    string name = row.Cells["student_name"].Value.ToString();
                    string course = row.Cells["course"].Value.ToString();
                    string lastName = row.Cells["student_id"].Value.ToString();
                    txtCourse.Text = course;
                    txtStudentID.Text = lastName;
                    txtName.Text = name;
                    picID.Image = Image.FromFile(row.Cells["photopath"].Value.ToString());
                    picQR.Image = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QR", lastName + ".png"));
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void Students_Load(object sender, EventArgs e)
        {
            loadAllBoarders();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            dgvStudents.ClearSelection();
            txtCourse.Clear();
            txtStudentID.Clear();
            txtName.Clear();
            picID.Image = null;
            selectedImagePath = "";
            selectedStudentID = -1;
            picQR.Image = null;
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            string studentID = txtStudentID.Text.Trim();
            string studentName = txtName.Text.Trim();
            string course = txtCourse.Text.Trim();

            var preview = new IDPreviewForm(studentID, studentName, course);
            preview.ShowDialog();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedStudentID >= 0)
                {
                    string course = txtCourse.Text;
                    string studentID = txtStudentID.Text;
                    string name = txtName.Text;

                    string photoDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos");
                    if (!Directory.Exists(photoDir)) Directory.CreateDirectory(photoDir);

                    string newPhotoPath = Path.Combine(photoDir, studentID + Path.GetExtension(selectedImagePath));

                    File.Copy(selectedImagePath, newPhotoPath, true);

                    using (SqlConnection conn = new SqlConnection(dbConnection))
                    {
                        conn.Open();
                        string updateBoarder = "UPDATE tblStudents SET course = @course, student_id = @studentid, student_name = @name, photopath = @photopath WHERE id = @id";
                        using (SqlCommand cmd = new SqlCommand(updateBoarder, conn))
                        {
                            cmd.Parameters.AddWithValue("@course", course);
                            cmd.Parameters.AddWithValue("@studentid", studentID);
                            cmd.Parameters.AddWithValue("@name", name);
                            cmd.Parameters.AddWithValue("@id", selectedStudentID);
                            cmd.Parameters.AddWithValue("@photopath", newPhotoPath);


                            int rowsAffected = cmd.ExecuteNonQuery();
                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Student updated successfully.");
                                loadAllBoarders();
                                btnClear_Click(sender, e);
                            }
                            else
                            {
                                MessageBox.Show("Failed to update Student.");
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("No student selected for update.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
