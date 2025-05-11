using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Attendo.Models
{
    public partial class SidePanel : UserControl
    {
        private MainScreen mainForm;
        private Button activeButton;
        public SidePanel(MainScreen mainForm)
        {
            InitializeComponent();
            this.mainForm = mainForm; 
            ColorActiveButton(btnDashboard);
        }

        private void ColorActiveButton(Button button)
        {
            if (activeButton != null)
            {
                activeButton.BackColor = Color.FromArgb(10, 17, 28);
            }

            activeButton = button;
            activeButton.BackColor = Color.FromArgb(128,245, 245, 245);
        }

        private void btnSessions_Click(object sender, EventArgs e)
        {
            mainForm.OpenForm(new Screens.Sessions());
            ColorActiveButton((Button)sender);
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            mainForm.OpenForm(new Screens.Dashboard());
            ColorActiveButton((Button)sender);
        }

        private void btnStudents_Click(object sender, EventArgs e)
        {
            mainForm.OpenForm(new Screens.Students());
            ColorActiveButton((Button)sender);
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
            mainForm.OpenForm(new Screens.Reports());
            ColorActiveButton((Button)sender);
        }

        private void btnQR_Click(object sender, EventArgs e)
        {
            mainForm.OpenForm(new Screens.QRGenerator());
            ColorActiveButton((Button)sender);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to logout?", "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (dialogResult == DialogResult.Yes)
            {
                MainScreen mainScreen = (MainScreen)this.ParentForm;
                mainScreen.Hide();
                Screens.Login loginForm = new Screens.Login();
                loginForm.Show();

                loginForm.FormClosed += (s, args) => mainScreen.Close(); // Close main screen when login form is closed
            }
        }
    }
}
