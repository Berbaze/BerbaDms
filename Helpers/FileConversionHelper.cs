using DmsProjeckt.Data;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using System.IO;

namespace DmsProjeckt.Helpers
{
    public static class FileConversionHelper
    {
        /// <summary>
        /// Konvertiert jede Datei (docx, png, jpg, txt, etc.) in ein PDF
        /// </summary>
        public static MemoryStream ConvertToPdf(string fileName, byte[] fileBytes)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            // 📄 Direkt PDF → nichts zu tun
            if (ext == ".pdf")
                return new MemoryStream(fileBytes);

            // 🖼️ Bilder (jpg/png → PDF)
            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
            {
                var ms = new MemoryStream(); // ⚠️ PAS DE using

                using (var doc = new PdfDocument())
                {
                    var page = doc.AddPage();

                    using (var image = SixLabors.ImageSharp.Image.Load(fileBytes))
                    {
                        page.Width = image.Width;
                        page.Height = image.Height;
                    }

                    using (var gfx = XGraphics.FromPdfPage(page))
                    using (var img = XImage.FromStream(() => new MemoryStream(fileBytes)))
                    {
                        gfx.DrawImage(img, 0, 0, page.Width, page.Height);
                    }

                    doc.Save(ms, false);
                }

                ms.Position = 0;
                return ms;
            }

            // 📑 Texte simples → PDF
            if (ext == ".txt")
            {
                var text = System.Text.Encoding.UTF8.GetString(fileBytes);
                var ms = new MemoryStream(); // ⚠️ PAS DE using

                using (var doc = new PdfDocument())
                {
                    var page = doc.AddPage();
                    using (var gfx = XGraphics.FromPdfPage(page))
                    {
                        gfx.DrawString(text, new XFont("Arial", 12), XBrushes.Black,
                            new XRect(20, 20, page.Width - 40, page.Height - 40), XStringFormats.TopLeft);
                    }

                    doc.Save(ms, false);
                }

                ms.Position = 0;
                return ms;
            }

            // ❌ Pas encore géré (docx, xlsx, etc.)
            throw new NotSupportedException($"Konvertierung von '{ext}' nach PDF nicht implementiert.");
        }
    }
}
