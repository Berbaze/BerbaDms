using System.Text;
using Tesseract;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing;
using PdfPig = UglyToad.PdfPig;
using Pdfium = PdfiumViewer.PdfDocument;

namespace DmsProjeckt.Data
{
    public class PdfOcrUtil
    {
        public static async Task<string> ExtractTextAsync(Stream pdfStream)
        {
            pdfStream.Position = 0;
            var sb = new StringBuilder();

            // 🧾 1. Lecture de texte via PdfPig
            using (var doc = PdfPig.PdfDocument.Open(pdfStream))
            {
                foreach (var page in doc.GetPages())
                {
                    if (!string.IsNullOrWhiteSpace(page.Text))
                        sb.AppendLine(page.Text);
                }
            }

            if (sb.Length > 50)
                return sb.ToString();

            // 📂 2. Préparation OCR
            pdfStream.Position = 0;
            var tessPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

            if (!Directory.Exists(tessPath))
                throw new DirectoryNotFoundException($"❌ Dossier tessdata introuvable à {tessPath}");

            // 📖 Choix de langue
            string lang = File.Exists(Path.Combine(tessPath, "deu.traineddata")) ? "deu" :
                          File.Exists(Path.Combine(tessPath, "eng.traineddata")) ? "eng" :
                          throw new FileNotFoundException("❌ Aucun fichier de langue .traineddata trouvé dans tessdata");

            using var engine = new TesseractEngine(tessPath, lang, EngineMode.Default);
            using var pdfDoc = Pdfium.Load(pdfStream);

            for (int i = 0; i < pdfDoc.PageCount; i++)
            {
                using var img = pdfDoc.Render(i, 400, 400, true);
                using var ms = new MemoryStream();
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;

                using var pix = Pix.LoadFromMemory(ms.ToArray());
                using var page = engine.Process(pix);
                var text = page.GetText();
                sb.AppendLine(text);
            }

            return sb.ToString();
        }
    }
}
