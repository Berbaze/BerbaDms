
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DmsProjeckt.Service
{
    public class PdfSplitter
    {
        public static void SplitPdf(string inputPath, string outputDir, int pagesPerChunk = 50)
        {
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            using var inputDocument = PdfSharp.Pdf.IO.PdfReader.Open(inputPath, PdfDocumentOpenMode.Import);
            int totalPages = inputDocument.PageCount;

            for (int i = 0; i < totalPages; i += pagesPerChunk)
            {
                using var outputDocument = new PdfSharp.Pdf.PdfDocument();
                for (int j = i; j < i + pagesPerChunk && j < totalPages; j++)
                {
                    outputDocument.AddPage(inputDocument.Pages[j]);
                }

                string chunkPath = Path.Combine(outputDir, $"chunk_{i + 1}-{Math.Min(i + pagesPerChunk, totalPages)}.pdf");
                outputDocument.Save(chunkPath);
            }
        }
        public static List<string> SplitPdfChunks(string inputPath, string outputDir, int pagesPerChunk = 50)
        {
            var chunks = new List<string>();

            try
            {
                var inputDoc = PdfReader.Open(inputPath, PdfDocumentOpenMode.Import);
                int totalPages = inputDoc.PageCount;

                for (int i = 0; i < totalPages; i += pagesPerChunk)
                {
                    var output = new PdfDocument();
                    for (int j = i; j < i + pagesPerChunk && j < totalPages; j++)
                        output.AddPage(inputDoc.Pages[j]);

                    string chunkPath = Path.Combine(outputDir, $"chunk_{i + 1}-{Math.Min(i + pagesPerChunk, totalPages)}.pdf");
                    output.Save(chunkPath);
                    chunks.Add(chunkPath);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("❌ Fehler beim Verarbeiten der PDF-Datei. Prüfe ob es sich um ein gültiges PDF handelt.", ex);
            }

            return chunks;
        }


        // 🧠 Nouvelle méthode : découpage mémoire pour Azure
        public List<MemoryStream> SplitPdfToPageStreams(Stream inputPdfStream, int maxPages = 5)
        {
            var streams = new List<MemoryStream>();
            using var temp = new MemoryStream();
            inputPdfStream.CopyTo(temp);
            temp.Position = 0;

            using var inputDoc = PdfReader.Open(temp, PdfDocumentOpenMode.Import);
            int pageCount = Math.Min(maxPages, inputDoc.PageCount);

            for (int i = 0; i < pageCount; i++)
            {
                var outputDoc = new PdfDocument();
                outputDoc.AddPage(inputDoc.Pages[i]);

                var ms = new MemoryStream();
                outputDoc.Save(ms, false);
                ms.Position = 0;

                streams.Add(ms);
            }

            return streams;
        }


    }
}
