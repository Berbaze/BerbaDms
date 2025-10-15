using iText.Kernel.Pdf;

namespace DmsProjeckt.Services
{
    public class PdfMetadataReader
    {
        public static (string Autor, string Betreff, string Schluesselwoerter) ReadMetadata(Stream pdfStream)
        {
            try
            {
                var reader = new PdfReader(pdfStream);
                var pdfDoc = new PdfDocument(reader);

                var info = pdfDoc.GetDocumentInfo();

                string autor = info.GetAuthor();
                string betreff = info.GetSubject();
                string schluesselwoerter = info.GetKeywords();

                pdfDoc.Close(); // important

                return (autor, betreff, schluesselwoerter);
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ Fehler beim PDF-Metadaten lesen: " + ex.Message);
                return (null, null, null);
            }
        }
    }
}
