using DmsProjeckt.Data;
using DmsProjeckt.Service;
using Google.Apis.Storage.v1;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DokumentIndexController : ControllerBase
    {
        private readonly DokumentIndexService _service;
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly FirebaseStorageService _firebaseStorageService;
        public DokumentIndexController(DokumentIndexService service , ApplicationDbContext context, EmailService emailService, FirebaseStorageService firebaseStorageService)
        {
            _service = service;

            _context = context;
            _emailService = emailService;
            _firebaseStorageService = firebaseStorageService;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var docs = await _service.GetAllIndexedAsync();
            return Ok(docs);
        }
   
        [HttpPost]
        public async Task<IActionResult> SendEmail([FromBody] ShareEmailDto dto)
        {
            // Dokument anhand des Dateinamens finden
            var dokument = await _context.Dokumente.FirstOrDefaultAsync(d => d.Dateiname == dto.Dateiname);

            if (dokument == null)
                return NotFound("Dokument nicht gefunden!");

            // Bucket-Name aus deiner Firebase/Google-Konsole
            var bucketName = "berbaze-4fbc8.appspot.com"; // <-- HIER DEIN BUCKET-NAME!

            // Der Pfad der Datei im Bucket, z.B. "uploads/2024/07/xyz.pdf"
            var objectName = dokument.ObjectPath;

            // Datei von Firebase/Google Cloud Storage laden
            var fileBytes = await _firebaseStorageService.LadeDateiAusFirebaseAsync(bucketName, objectName);

            if (fileBytes == null || fileBytes.Length == 0)
                return NotFound("Datei nicht gefunden!");

            await _emailService.SendEmailAsync(
                dto.Empfaenger,
                dto.Betreff,
                dto.Nachricht,
                new List<(byte[], string, string)>
                {
            (fileBytes, dto.Dateiname, "application/pdf")
                });

            return Ok();
        }

        [HttpGet("Download")]
        public async Task<IActionResult> Download(string file)
        {
            var bucketName = "berbaze-4fbc8.appspot.com";
            var dokument = await _context.Dokumente.FirstOrDefaultAsync(d => d.Dateiname == file);
            if (dokument == null)
                return NotFound("Dokument nicht gefunden!");

            // Falls du Firebase benutzt:
            var fileBytes = await _firebaseStorageService.LadeDateiAusFirebaseAsync(bucketName, dokument.ObjectPath);
            if (fileBytes == null)
                return NotFound("Datei nicht gefunden!");
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{file}\"";
            return File(fileBytes, "application/pdf");

        }

    }
    public class ShareEmailDto
    {
        public string Dateiname { get; set; }
        public string Empfaenger { get; set; }
        public string Betreff { get; set; }
        public string Nachricht { get; set; }
    }
    public class RenameFolderRequest
    {
        public string Path { get; set; }
        public string NewName { get; set; }
    }

}
