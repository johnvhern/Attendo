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
    public partial class Login : Form
    {
        private string dbConnection = "Data Source=localhost\\sqlexpress;Initial Catalog=Attendo;Integrated Security=True;";
        public Login()
        {
            InitializeComponent();
            txtPassword.UseSystemPasswordChar = true;
            this.KeyPreview = true;
        }

        private void showPassCheckBox_CheckedChanged_1(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !showPassCheckBox.Checked;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.", "Invalid Entry", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = "SELECT password FROM tblUsers WHERE username = @username";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        string storedHash = result.ToString();
                        if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                        {
                            MessageBox.Show("Login successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            MainScreen mainScreen = new MainScreen();
                            mainScreen.Show();
                            this.Hide();
                            mainScreen.FormClosed += (s, args) => this.Close(); // Close login form when main screen is closed


                        }
                        else
                        {
                            MessageBox.Show("Incorrect password.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Username not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void Login_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnLogin.PerformClick();
            }
        }
    }
}
