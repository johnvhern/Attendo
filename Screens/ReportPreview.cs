using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using PrintDialog = System.Windows.Forms.PrintDialog;

namespace Attendo.Screens
{
    public partial class ReportPreview : Form
    {
        private DataTable printTable;
        private string sessionName, courseName, sessionDate, sessionCutOffTime;
        private PrintPreviewControl printPreviewControl;
        private PrintDocument printDocument;
        PrintDialog printDialog = new PrintDialog();


        public ReportPreview(PrintDocument doc, DataTable table, string session, string course, string date, string cutofftime)
        {
            InitializeComponent();
            printDocument = doc;
            printTable = table;
            sessionName = session;
            courseName = course;
            sessionDate = date;
            sessionCutOffTime = cutofftime;

            printPreviewControl = new PrintPreviewControl
            {
                Dock = DockStyle.Fill,
                Document = printDocument
            };

            panel2.Controls.Add(printPreviewControl);
        }

        private void ReportPreview_Load(object sender, EventArgs e)
        {
        }

        public void ShowPrintPreview()
        {
            // Show the PrintPreviewDialog inside the custom form
            this.ShowDialog();
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            printDialog.Document = printDocument;
            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                printDocument.Print();
            }

        }

        private void btnSavePDF_Click(object sender, EventArgs e)
        {
            ExportToPdf(printTable, sessionName, courseName, sessionDate);
        }

        private void ExportToPdf(DataTable printTable, string sessionName, string courseName, string sessionDate)
        {
            string fileName = $"AttendanceReport_{sessionName}_{courseName}_{sessionDate.Replace(" ", "_").Replace(",", "")}.pdf";

            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.FileName = fileName;
                saveDialog.Filter = "PDF Files (*.pdf)|*.pdf";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var writer = new PdfWriter(saveDialog.FileName))
                    {
                        using (var pdf = new PdfDocument(writer))
                        {
                            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());
                            document.SetMargins(20, 20, 20, 20);

                            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                            PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                            document.Add(new Paragraph("Attendance Report")
                                .SetFont(boldFont)
                                .SetFontSize(16)
                                .SetTextAlignment(TextAlignment.LEFT));

                            document.Add(new Paragraph($"Session: {sessionName}    Course: {courseName}")
                                .SetFont(font)
                                .SetFontSize(9));

                            document.Add(new Paragraph($"Date: {sessionDate}    Cut Off Time: {sessionCutOffTime}")
                                .SetFont(font)
                                .SetFontSize(9)
                                .SetMarginBottom(20));

                            Table table = new Table(printTable.Columns.Count).UseAllAvailableWidth();

                            foreach (DataColumn col in printTable.Columns)
                            {
                                string headerText = col.ColumnName;
                                if (col.ColumnName == "course") headerText = "Course";
                                else if (col.ColumnName == "student_id") headerText = "Student ID";
                                else if (col.ColumnName == "student_name") headerText = "Student Name";
                                else if (col.ColumnName == "status") headerText = "Status";
                                else if (col.ColumnName == "scan_time") headerText = "Scan Time";
                                // Add more as needed

                                table.AddHeaderCell(new Cell()
                                        .Add(new Paragraph(headerText).SetTextAlignment(TextAlignment.CENTER))
                                        .SetFont(boldFont)
                                        .SetFontSize(10)
                                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                            }


                            foreach (DataRow row in printTable.Rows)
                            {
                                for (int i = 0; i < printTable.Columns.Count; i++)
                                {
                                    string text = row[i]?.ToString() ?? "";
                                    var cell = new Cell().Add(new Paragraph(text));

                                    if (printTable.Columns[i].ColumnName.Equals("Status", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (text == "IN")
                                        {
                                            cell.SetBackgroundColor(ColorConstants.GREEN)
                                                .SetFontColor(ColorConstants.BLACK);
                                        }
                                        else if (text == "LATE" || text == "ABSENT")
                                        {
                                            cell.SetBackgroundColor(ColorConstants.RED)
                                                .SetFontColor(ColorConstants.WHITE);
                                        }
                                    }
                                    table.SetFontSize(9);
                                    table.AddCell(cell);
                                }
                            }

                            document.Add(table);
                            document.Close();
                        }
                    }

                    MessageBox.Show("PDF saved successfully!", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

    }
}
