using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Image;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using iText.Layout.Properties;
using System.Data.SqlClient;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using QRCoder;
using System.Data.Common;
using iText.Kernel.Geom;
using Rectangle = System.Drawing.Rectangle;
using Path = System.IO.Path;

namespace Attendo.Screens
{
    public partial class Preview : Form
    {
        private string dbConnection = "Data Source=localhost\\sqlexpress;Initial Catalog=Attendo;Integrated Security=True;";
        private string studentID;
        private string studentName;
        private string course;
        private System.Drawing.Image qrImage;
        private List<(string ID, string Name, string Course)> selectedStudents;
        private List<Bitmap> qrCards = new List<Bitmap>(); // for print and preview
        private int currentPage = 0;

        public Preview(List<(string ID, string Name, string Course)> students)
        {
            InitializeComponent();

            selectedStudents = students;
            GenerateQRCards();

            currentPage = 0; // Reset page count
            printDocument1.PrintPage += PrintDocument1_PrintPage;
            printPreviewControl1.Document = printDocument1;
            printPreviewControl1.InvalidatePreview();


        }

        private void GenerateQRCards()
        {
            qrCards.Clear();

            foreach (var student in selectedStudents)
            {
                string content = $"{student.ID}|{student.Name}|{student.Course}";
                using (var qrGen = new QRCodeGenerator())
                using (var qrData = qrGen.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q))
                using (var qrCode = new QRCode(qrData))
                {
                    Bitmap qr = qrCode.GetGraphic(20);
                    Bitmap card = new Bitmap(350, 200); // example ID card size

                    using (Graphics g = Graphics.FromImage(card))
                    {
                        g.Clear(Color.White);
                        g.DrawImage(qr, new Rectangle(10, 20, 100, 100));
                        g.DrawString($"ID: {student.ID}", new Font("Arial", 10), Brushes.Black, new PointF(120, 30));
                        g.DrawString($"Name: {student.Name}", new Font("Arial", 10), Brushes.Black, new PointF(120, 60));
                        g.DrawString($"Course: {student.Course}", new Font("Arial", 10), Brushes.Black, new PointF(120, 90));
                    }

                    qrCards.Add(card);
                }
            }
        }

        private void PrintDocument1_PrintPage(object sender, PrintPageEventArgs e)
        {
            int cardsPerRow = 2;
            int cardsPerColumn = 2;
            int cardWidth = 350;
            int cardHeight = 500;
            int spacingX = 30;
            int spacingY = 30;

            int startX = 50;
            int startY = 50;

            int cardsThisPage = 0;
            Graphics g = e.Graphics;

            for (int row = 0; row < cardsPerColumn; row++)
            {
                for (int col = 0; col < cardsPerRow; col++)
                {
                    if (currentPage >= selectedStudents.Count)
                    {
                        e.HasMorePages = false;
                        return;
                    }

                    var student = selectedStudents[currentPage];
                    Rectangle cardBounds = new Rectangle(
                        startX + col * (cardWidth + spacingX),
                        startY + row * (cardHeight + spacingY),
                        cardWidth,
                        cardHeight
                    );

                    DrawIDCard(g, cardBounds, student.ID, student.Name, student.Course);

                    currentPage++;
                    cardsThisPage++;
                }
            }

            e.HasMorePages = (currentPage < selectedStudents.Count);
        }

        private void DrawIDCard(Graphics g, Rectangle bounds, string studentID, string studentName, string course)
        {
            string photoRelativePath = ""; // this comes from the database
            using (SqlConnection conn = new SqlConnection(dbConnection))
            {
                conn.Open();
                string query = "SELECT photopath FROM tblStudents WHERE student_id = @id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", studentID);
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        photoRelativePath = result.ToString();
                    }
                }
            }

            string schoolName = "Garcia College of Technology";

            string photoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, photoRelativePath);
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "favicon.png");
            string qrPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QR", studentID + ".png");

            Font fontTitle = new Font("Arial", 12, FontStyle.Bold);
            Font fontLabel = new Font("Arial", 10, FontStyle.Regular);
            Font fontInfo = new Font("Arial", 10, FontStyle.Bold);
            Brush brush = Brushes.Black;

            int cardX = bounds.X;
            int cardY = bounds.Y;
            int cardWidth = bounds.Width;
            int cardHeight = bounds.Height;

            int centerX = cardX + cardWidth / 2;
            float contentWidth = 250f;
            float contentLeft = cardX + (cardWidth - contentWidth) / 2;
            StringFormat centerFormat = new StringFormat { Alignment = StringAlignment.Center };

            g.FillRectangle(Brushes.White, bounds);
            using (Pen borderPen = new Pen(Color.Black, 2)) // Change color and thickness as needed
            {
                g.DrawRectangle(borderPen, bounds);
            }


            if (File.Exists(logoPath))
            {
                System.Drawing.Image logo = System.Drawing.Image.FromFile(logoPath);
                g.DrawImage(logo, centerX - 25, cardY + 10, 50, 50);
            }

            g.DrawString(schoolName, fontTitle, brush, new RectangleF(cardX, cardY + 70, cardWidth, 30), centerFormat);

            if (File.Exists(photoPath))
            {
                System.Drawing.Image photo = System.Drawing.Image.FromFile(photoPath);

                int photoWidth = 120;
                int photoHeight = 120;
                int photoX = centerX - (photoWidth / 2);
                int photoY = cardY + 110;

                // Draw the photo
                g.DrawImage(photo, photoX, photoY, photoWidth, photoHeight);

                // Draw a black border
                Pen borderPen = new Pen(Color.Black, 1); // 2px border
                g.DrawRectangle(borderPen, photoX, photoY, photoWidth, photoHeight);
            }

            int textY = cardY + 240;

            g.DrawString("Student ID:", fontLabel, brush, new RectangleF(contentLeft, textY, contentWidth, 20), centerFormat);
            g.DrawString(studentID, fontInfo, brush, new RectangleF(contentLeft, textY + 15, contentWidth, 20), centerFormat);

            g.DrawString("Name:", fontLabel, brush, new RectangleF(contentLeft, textY + 45, contentWidth, 20), centerFormat);
            g.DrawString(studentName, fontInfo, brush, new RectangleF(contentLeft, textY + 60, contentWidth, 20), centerFormat);

            g.DrawString("Course:", fontLabel, brush, new RectangleF(contentLeft, textY + 90, contentWidth, 20), centerFormat);
            g.DrawString(course, fontInfo, brush, new RectangleF(contentLeft, textY + 105, contentWidth, 20), centerFormat);

            if (File.Exists(qrPath))
            {
                System.Drawing.Image qr = System.Drawing.Image.FromFile(qrPath);
                g.DrawImage(qr, centerX - 40, cardY + 380, 80, 80);
            }
        }


        private void btnPrint_Click(object sender, EventArgs e)
        {
            PrintDialog dlg = new PrintDialog();
            dlg.Document = printDocument1;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                printDocument1.Print();
            }
        }

        private void btnSavePDF_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PDF File|*.pdf",
                Title = "Save All Student IDs",
                FileName = "Student_IDs.pdf"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                using (var writer = new PdfWriter(saveDialog.FileName))
                using (var pdf = new PdfDocument(writer))
                using (var doc = new Document(pdf))
                {
                    int cardsPerRow = 2;
                    int cardsPerCol = 2;
                    int cardWidth = 350;
                    int cardHeight = 500;

                    int index = 0;
                    while (index < selectedStudents.Count)
                    {
                        using (Bitmap pageBitmap = new Bitmap(850, 1100)) // Letter size in pixels at 96 DPI
                        using (Graphics g = Graphics.FromImage(pageBitmap))
                        {
                            g.Clear(Color.White);
                            int startX = 30;
                            int startY = 30;
                            int spacingX = 30;
                            int spacingY = 30;

                            for (int row = 0; row < cardsPerCol; row++)
                            {
                                for (int col = 0; col < cardsPerRow; col++)
                                {
                                    if (index >= selectedStudents.Count) break;

                                    var student = selectedStudents[index];
                                    Rectangle cardBounds = new Rectangle(
                                        startX + col * (cardWidth + spacingX),
                                        startY + row * (cardHeight + spacingY),
                                        cardWidth,
                                        cardHeight
                                    );

                                    DrawIDCard(g, cardBounds, student.ID, student.Name, student.Course);
                                    index++;
                                }
                            }

                            using (MemoryStream ms = new MemoryStream())
                            {
                                pageBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                var imgData = ImageDataFactory.Create(ms.ToArray());
                                var pdfImg = new iText.Layout.Element.Image(imgData)
                                    .ScaleToFit(PageSize.LETTER.GetWidth(), PageSize.LETTER.GetHeight());

                                doc.Add(pdfImg);
                                if (index < selectedStudents.Count)
                                    doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                            }
                        }
                    }
                }

                MessageBox.Show("PDF saved successfully.");
            }
        }

    }
}
