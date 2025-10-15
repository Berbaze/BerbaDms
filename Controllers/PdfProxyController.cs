using DmsProjeckt.Data;
using DmsProjeckt.Service;
using DmsProjeckt.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Controllers
{
    [Route("api/dokumente")]
    public class PdfProxyController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _db;
        private readonly ChunkService _chunkService;
        private readonly FirebaseStorageService _firebase;
        private readonly string _bucketName = "berbaze-4fbc8.appspot.com";

        public PdfProxyController(
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext db,
            ChunkService chunkService,
            FirebaseStorageService firebase)
        {
            _httpClientFactory = httpClientFactory;
            _db = db;
            _chunkService = chunkService;
            _firebase = firebase;
        }

        [HttpGet]
        public async Task<IActionResult> GetPdf([FromQuery] string? url, [FromQuery] string? objectPath, [FromQuery] Guid? dokumentId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(objectPath))
                    objectPath = Uri.UnescapeDataString(objectPath);

                if (!string.IsNullOrWhiteSpace(objectPath) &&
                    (objectPath.StartsWith("chunked://", StringComparison.OrdinalIgnoreCase) ||
                     objectPath.StartsWith("chunked%3A%2F%2F", StringComparison.OrdinalIgnoreCase)))
                {
                    // 🔍 On détecte un fichier chunké
                    var guidPart = objectPath
                        .Replace("chunked://", "")
                        .Replace("chunked%3A%2F%2F", "")
                        .Trim();

                    Console.WriteLine($"🧩 Chunked document detected: {guidPart}");

                    // 🔎 On cherche le document dans la base
                    var dokument = dokumentId.HasValue
                        ? await _db.Dokumente.FirstOrDefaultAsync(d => d.Id == dokumentId.Value)
                        : await _db.Dokumente.FirstOrDefaultAsync(d => d.ObjectPath.Contains(guidPart));

                    if (dokument == null)
                        return NotFound($"❌ Kein Dokument gefunden für {objectPath}");

                    // 🧠 Reconstruit depuis Firebase
                    var reconstructedPath = await _chunkService.ReconstructFileFromFirebaseAsync(dokument.Id);

                    if (string.IsNullOrWhiteSpace(reconstructedPath) || !System.IO.File.Exists(reconstructedPath))
                        return Content("❌ Chunked-Datei konnte nicht rekonstruiert werden.");

                    Console.WriteLine($"✅ Rekonstruktion abgeschlossen: {reconstructedPath}");

                    var stream = System.IO.File.OpenRead(reconstructedPath);
                    return File(stream, "application/pdf", dokument.Dateiname ?? "chunked_document.pdf");
                }

                // 🔹 Cas standard (non chunké)
                string fileUrl;
                if (!string.IsNullOrWhiteSpace(url))
                    fileUrl = url;
                else if (!string.IsNullOrWhiteSpace(objectPath))
                {
                    var encodedPath = Uri.EscapeDataString(objectPath);
                    fileUrl = $"https://storage.googleapis.com/{_bucketName}/{encodedPath}";
                }
                else
                    return BadRequest("❌ Kein URL oder objectPath angegeben");

                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(fileUrl);

                if (!response.IsSuccessStatusCode)
                    return NotFound($"❌ PDF nicht gefunden: {fileUrl}");

                var content = await response.Content.ReadAsByteArrayAsync();
                return File(content, "application/pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fehler beim PDF-Laden: {ex.Message}");
                return BadRequest("❌ Fehler beim PDF-Laden: " + ex.Message);
            }
        }

        [HttpGet("view/{id}")]
        public async Task<IActionResult> ViewDokument(Guid id)
        {
            try
            {
                // 🧩 Reconstruit le PDF si nécessaire
                var filePath = await _chunkService.ReconstructFileFromFirebaseAsync(id);

                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    Console.WriteLine($"❌ Fichier introuvable pour l'ID {id}");
                    return NotFound("Le fichier n’a pas pu être reconstruit.");
                }

                Console.WriteLine($"📄 Lecture du fichier reconstruit : {filePath}");

                // ✅ Utilise FileStream avec FileShare.ReadWrite (autorise lecture concurrente)
                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // 🧠 Détermine le MIME dynamiquement
                var contentType = "application/pdf";
                return File(stream, contentType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de l’ouverture du PDF : {ex.Message}");
                return StatusCode(500, "Erreur interne lors de la lecture du fichier PDF.");
            }
        }
    }
}
