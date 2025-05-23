﻿using MetroFramework.Controls;
using QRCoder;
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
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Attendo.Screens
{
    public partial class QRGenerator : Form
    {
        private string dbConnection = "Data Source=localhost\\sqlexpress;Initial Catalog=Attendo;Integrated Security=True;";
        private PrintDocument printDocument;
        public QRGenerator()
        {
            InitializeComponent();
        }

        private void LoadStudents()
        {
            try
            {
                string column;
                string searchValue = txtSearchBox.Text.Trim();

                if (string.IsNullOrEmpty(searchValue))
                {
                    MessageBox.Show("Please enter a search term.");
                    return;
                }

                string selected = cbCourseFilter.SelectedItem?.ToString();

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


                using (SqlConnection conn = new SqlConnection(dbConnection))
                {
                    conn.Open();
                    string query = $"SELECT DISTINCT course, student_id, student_name FROM tblStudents WHERE {column} LIKE @SearchValue;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SearchValue", $"%{searchValue}%");
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                        dgvStudents.DataSource = dt;
                        dgvStudents.Columns["course"].HeaderText = "Course";
                        dgvStudents.Columns["student_id"].HeaderText = "Student ID";
                        dgvStudents.Columns["student_name"].HeaderText = "Name";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured: {ex.Message}");
            }
        }

        public static class QRCodeGeneratorUtil
        {
            public static void GenerateQRCode(string data, string filePath)
            {
                using (var qrGenerator = new QRCodeGenerator())
                using (var qrData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q))
                using (var qrCode = new QRCode(qrData))
                using (var qrImage = qrCode.GetGraphic(30))
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath); // or use overwrite-safe logic
                    }
                    qrImage.SetResolution(300, 300);
                    qrImage.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

                }
            }
        }
        private void btnSelectedQR_Click(object sender, EventArgs e)
        {
            if (dgvStudents.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a student.");
                return;
            }

            foreach (DataGridViewRow row in dgvStudents.SelectedRows)
            {
                
                string studentID = row.Cells["student_id"].Value.ToString();
                string name = row.Cells["student_name"].Value.ToString();
                string course = row.Cells["course"].Value.ToString();
                string qrContent = studentID + '|' + name + '|' + course;
                string path = Path.Combine("QR", studentID + ".png");

                QRCodeGeneratorUtil.GenerateQRCode(qrContent, path);
            }

            MessageBox.Show("QR code(s) generated for selected student(s).");
        }

        private void btnGenerateAllQR_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvStudents.Rows)
            {
                string studentID = row.Cells["student_id"].Value.ToString();
                string name = row.Cells["student_name"].Value.ToString();
                string course = row.Cells["course"].Value.ToString();
                string qrContent = studentID + '|' + name + '|' + course;
                string path = Path.Combine("QR", studentID + ".png");

                QRCodeGeneratorUtil.GenerateQRCode(qrContent, path);
            }

            MessageBox.Show("QR code(s) generated for all filtered students.");
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            LoadStudents();
        }

        private void btnPrintSelectedID_Click(object sender, EventArgs e)
        {
            if (dgvStudents.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select at least one student.");
                return;
            }

            var students = new List<(string ID, string Name, string Course)>();

            foreach (DataGridViewRow row in dgvStudents.SelectedRows)
            {
                string studentID = row.Cells["student_id"].Value.ToString();
                string name = row.Cells["student_name"].Value.ToString();
                string course = row.Cells["course"].Value.ToString();
                students.Add((studentID, name, course));
            }

            Preview previewForm = new Preview(students);
            previewForm.ShowDialog();
        }

    }
}
