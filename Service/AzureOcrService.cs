using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace DmsProjeckt.Service
{
    public class AzureOcrService
    {
        private readonly DocumentAnalysisClient _client;

        public AzureOcrService(IConfiguration config)
        {
            var endpoint = config["AzureFormRecognizer:Endpoint"];
            var key = config["AzureFormRecognizer:ApiKey"];
            _client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));
        }

        // Analyse une facture/receipt et retourne les métadonnées
        public async Task<AnalyzeResult> AnalyzeInvoiceAsync(Stream fileStream)
        {
            var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-invoice", fileStream);
            return operation.Value;
        }
    }
}
