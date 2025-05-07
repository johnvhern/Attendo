using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Layout;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Attendo.Screens
{
    public partial class IDPreviewForm : Form
    {
        private Bitmap idBitmap;
        private PrintDocument printDocument;
        public IDPreviewForm(string studentID, string name, string course)
        {
            InitializeComponent();
            Width = 600;
            Height = 800;

            var pictureBox = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 600,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            Controls.Add(pictureBox);

            var btnSaveImage = new Button { Text = "Save as Image", Dock = DockStyle.Top };
            var btnSavePDF = new Button { Text = "Save as PDF", Dock = DockStyle.Top };
            var btnPrint = new Button { Text = "Print", Dock = DockStyle.Top };

            Controls.Add(btnPrint);
            Controls.Add(btnSavePDF);
            Controls.Add(btnSaveImage);

            Rectangle bounds = new Rectangle(0, 0, 300, 500);
            idBitmap = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(idBitmap))
            {
                DrawIDCard(g, bounds, studentID, name, course);
            }

            pictureBox.Image = idBitmap;

            btnSaveImage.Click += (s, e) => SaveAsImage();
            btnSavePDF.Click += (s, e) => SaveAsPDF();
            btnPrint.Click += (s, e) => PrintID();

            printDocument = new PrintDocument();
            printDocument.PrintPage += (s, e) =>
            {
                e.Graphics.DrawImage(idBitmap, new Point(100, 100));
            };
        }

        private void SaveAsImage()
        {
            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "PNG Image|*.png" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    idBitmap.Save(sfd.FileName);
                    MessageBox.Show("Image saved.");
                }
            }
        }

        private void SaveAsPDF()
        {
            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF File|*.pdf" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (var fs = new FileStream(sfd.FileName, FileMode.Create))
                    using (var writer = new PdfWriter(fs))
                    using (var pdf = new PdfDocument(writer))
                    using (var doc = new Document(pdf))
                    {
                        using (var ms = new MemoryStream())
                        {
                            idBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            var imageData = ImageDataFactory.Create(ms.ToArray());
                            var pdfImg = new iText.Layout.Element.Image(imageData);
                            doc.Add(pdfImg);
                        }
                    }
                    MessageBox.Show("PDF saved.");
                }
            }
        }

        private void PrintID()
        {
            PrintDialog pd = new PrintDialog { Document = printDocument };
            if (pd.ShowDialog() == DialogResult.OK)
            {
                printDocument.Print();
            }
        }

        private void DrawIDCard(Graphics g, Rectangle bounds, string studentID, string studentName, string course)
        {
            string schoolName = "Garcia College of Technology";

            string photoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos", studentID + ".jpg");
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
            g.DrawRectangle(Pens.Black, bounds);

            if (File.Exists(logoPath))
            {
                Image logo = Image.FromFile(logoPath);
                g.DrawImage(logo, centerX - 25, cardY + 10, 50, 50);
            }

            g.DrawString(schoolName, fontTitle, brush, new RectangleF(cardX, cardY + 70, cardWidth, 30), centerFormat);

            if (File.Exists(photoPath))
            {
                Image photo = Image.FromFile(photoPath);
                g.DrawImage(photo, centerX - 60, cardY + 110, 120, 120);
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
                Image qr = Image.FromFile(qrPath);
                g.DrawImage(qr, centerX - 40, cardY + 380, 80, 80);
            }
        }
    }
}
