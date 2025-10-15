using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
namespace DmsProjeckt.Data
{
    public class WordUtil
    {
        public static string ExtractDocxText(Stream docxStream)
        {
            docxStream.Position = 0;
            using var memStream = new MemoryStream();
            docxStream.CopyTo(memStream);
            memStream.Position = 0;

            var sb = new StringBuilder();
            using var wordDoc = WordprocessingDocument.Open(memStream, false);
            var body = wordDoc.MainDocumentPart?.Document?.Body;

            if (body != null)
                sb.Append(body.InnerText);

            return sb.ToString();
        }
    }
}
