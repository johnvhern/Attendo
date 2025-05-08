using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Attendo.Screens
{
    public partial class Dashboard : Form
    {
        public Dashboard()
        {
            InitializeComponent();
        }

        private void Dashboard_Load(object sender, EventArgs e)
        {
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
        }
    }
}
