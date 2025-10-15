using System.Text.RegularExpressions;
using DmsProjeckt.Data;
using DmsProjeckt.Helpers;
using DmsProjeckt.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DmsProjeckt.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class PdfApiController : ControllerBase
    {
        private readonly PdfSigningService _pdfSigningService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PdfApiController> _logger;
        private readonly string _bucket;
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        public PdfApiController(
            PdfSigningService pdfSigningService,
            UserManager<ApplicationUser> userManager,
            ILogger<PdfApiController> logger,
            IConfiguration config,
            ApplicationDbContext context,
            EmailService emailService)
        {
            _pdfSigningService = pdfSigningService;
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _emailService = emailService;
            // ✅ Bucket injecté depuis appsettings.json
            _bucket = config["Firebase:Bucket"]
                      ?? throw new Exception("❌ Firebase:Bucket ist nicht konfiguriert!");
        }

        [HttpPost("save-signature")]
        public async Task<IActionResult> SaveSignature([FromBody] SignaturePayload payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.FileName))
                return BadRequest("❌ Paramètres manquants.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("❌ Utilisateur non authentifié.");

            try
            {
                // 🔹 HTML-Text bereinigen (Tags entfernen, <br> → \n)
                string cleanText = Regex.Replace(payload.TextHtml ?? "", "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
                cleanText = Regex.Replace(cleanText, "<.*?>", "");

                // 🔹 Dateityp ermitteln (falls nicht im Payload angegeben)
                string fileType = (payload.FileType ?? "").ToLower();
                if (string.IsNullOrWhiteSpace(fileType))
                {
                    var ext = Path.GetExtension(payload.FileName)?.ToLower();
                    if (ext == ".pdf") fileType = "pdf";
                    else if (ext == ".jpg" || ext == ".jpeg" || ext == ".png") fileType = "image";
                }

                _logger.LogInformation("[SIGNATURE API] FileType erkannt = {type}", fileType);

                string? signedFileName = null;

                if (fileType == "pdf")
                {
                    // 📄 PDF-Fall → immer mit OriginalPath arbeiten, um das Ursprungsdokument in der DB zu finden
                    signedFileName = await _pdfSigningService.SaveSignedVersionAsync(
                        payload.OriginalPath ?? payload.FileName!,   // 🔹 Originalpfad für DB-Match (z.B. .jpg in DB)
                        payload.FileName!,                           // 🔹 PDF-Pfad nach Konvertierung (zum Signieren)
                        payload.ImageBase64!,
                        payload.PageNumber,
                        payload.X,
                        payload.Y,
                        payload.Width,
                        payload.Height,
                        payload.CanvasWidth,
                        payload.CanvasHeight,
                        user.Id,
                        user.FirmenName ?? "Ma Société",
                        payload.Metadaten
                    );
                }
                else if (fileType == "image")
                {
                    // 🖼️ Bild-Fall → separate Signatur-Methode
                    signedFileName = await _pdfSigningService.SaveSignedImageVersionAsync(
                        payload.FileName,
                        payload.ImageBase64!,
                        payload.X,
                        payload.Y,
                        payload.Width,
                        payload.Height,
                        user.Id,
                        cleanText,
                        payload.TextX,
                        payload.TextY,
                        payload.TextWidth,
                        payload.TextHeight,
                        payload.Metadaten
                    );
                }
                else
                {
                    return BadRequest("❌ Dateityp nicht unterstützt. FileType=" + (payload.FileType ?? "NULL"));
                }

                var originalPath = payload.OriginalPath ?? payload.FileName;
                var normalizedPath = DocumentPathHelper.NormalizePath(originalPath);

                var parentId = await _context.Dokumente
                    .Where(d => d.ObjectPath == normalizedPath)
                    .Select(d => d.Id)
                    .FirstOrDefaultAsync();


                if (parentId != Guid.Empty)
                {
                    // 🔹 Offene Signaturanfragen als "Signed" markieren
                    var requests = await _context.SignatureRequests
                        .Where(r => r.FileId == parentId
                                 && r.RequestedUserId == user.Id
                                 && r.Status == "Pending")
                        .ToListAsync();

                    foreach (var req in requests)
                    {
                        req.Status = "Signed";

                        var userr = await _userManager.GetUserAsync(User);

                        // 🔔 Interne Benachrichtigung
                        var notificationType = await _context.NotificationTypes
                            .FirstOrDefaultAsync(n => n.Name == "SignRqDone");

                        if (notificationType != null)
                        {
                            var notification = new Notification
                            {
                                Title = "Dokument signiert",
                                Content = $"Das Dokument \"{payload.FileName}\" wurde von {userr?.Vorname} {userr?.Nachname} auf Ihre Anfrage signiert.",
                                CreatedAt = DateTime.UtcNow,
                                NotificationTypeId = notificationType.Id,
                                ActionLink = Url.Page("/Dokument/Index")
                            };

                            _context.Notifications.Add(notification);
                            await _context.SaveChangesAsync();

                            _context.UserNotifications.Add(new UserNotification
                            {
                                UserId = req.RequestedByUserId,
                                NotificationId = notification.Id,
                                IsRead = false,
                                ReceivedAt = DateTime.UtcNow
                            });
                        }

                        // 📧 E-Mail-Benachrichtigung
                        var userTo = await _context.Users.FindAsync(req.RequestedByUserId);
                        if (userTo != null)
                        {
                            string subject = "Dokument signiert";
                            string body = $@"
                        <p>Hallo {userTo.Vorname},</p>
                        <p>Das Dokument <b>""{payload.FileName}""</b> wurde von <b>{userr?.Vorname} {userr?.Nachname}</b> auf Ihre Anfrage signiert.</p>
                        <p><a href='{Url.Page("/Dokument/Index")}'>Ansehen</a></p>
                        <p>Viele Grüße,<br/>Ihr DMS-Team</p>";

                            await _emailService.SendEmailAsync(userTo.Email, subject, body);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning("❌ Kein passendes Dokument in DB gefunden für Path={path}", payload.OriginalPath ?? payload.FileName);
                }

                return Ok(new
                {
                    success = true,
                    file = signedFileName,
                    redirectUrl = Url.Page("/Dokument/Index")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern der Signatur.");
                return StatusCode(500, "❌ Serverfehler : " + ex.Message);
            }
        }




        [HttpGet("get-pdf-url/{fileName}")]
        public IActionResult GetPdfUrl(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("❌ Kein Dateiname angegeben.");

            // ✅ Encodage pour éviter les problèmes avec espaces et caractères spéciaux
            var safePath = Uri.EscapeDataString(fileName);

            // ✅ Construire l’URL complète Firebase
            var pdfUrl = $"https://storage.googleapis.com/{_bucket}/{safePath}";

            return Ok(new { Url = pdfUrl });
        }

        public class SignaturePayload
        {
            public string? FileName { get; set; }     // chemin PDF (après conversion)
            public string? OriginalPath { get; set; } // chemin original en DB (jpg/png/...)
            public string? FileType { get; set; }     // "pdf" ou "image"

            public string? ImageBase64 { get; set; }
            public int PageNumber { get; set; } = 1;
            public float X { get; set; }
            public float Y { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }
            public float CanvasWidth { get; set; } = 900f;
            public float CanvasHeight { get; set; } = 1270f;

            public string? TextHtml { get; set; }
            public float TextX { get; set; }
            public float TextY { get; set; }
            public float TextWidth { get; set; }
            public float TextHeight { get; set; }

            public string? Metadaten { get; set; }
            public string? FirmaName { get; set; }
            public string? AbteilungName { get; set; }
            public string? Kategorie { get; set; }
        }


    }
}
