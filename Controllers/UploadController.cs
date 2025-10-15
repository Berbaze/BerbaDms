using DmsProjeckt.Data;
using DmsProjeckt.Helpers;
using DmsProjeckt.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace DmsProjeckt.Controllers
{
    [Route("api/upload")]
    [ApiController]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly FirebaseStorageService _storageService;
        private readonly AzureOcrService _azureOcrService;
        private readonly ILogger<UploadController> _logger;

        public UploadController(ApplicationDbContext db, FirebaseStorageService storageService, ILogger<UploadController> logger, AzureOcrService azureOcrService)
        {
            _db = db;
            _storageService = storageService;
            _logger = logger;
            _azureOcrService = azureOcrService;
        }
        // 🔹 1) OCR → retourne uniquement les Metadaten
        [HttpPost("scan-ocr")]
        public async Task<IActionResult> UploadScanOcr(IFormFile file)
        {
            _logger.LogInformation("📥 OCR Analyse gestartet");

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "❌ Kein Datei erhalten" });

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                ms.Position = 0;

                // 🔹 OCR Azure Service
                var ocrErgebnis = await _azureOcrService.AnalyzeInvoiceAsync(ms);
                if (ocrErgebnis == null)
                    return BadRequest(new { success = false, message = "❌ OCR Analyse fehlgeschlagen" });

                return Ok(new { success = true, metadata = ocrErgebnis });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Fehler bei UploadScanOcr");
                return StatusCode(500, new { success = false, message = "Serverfehler bei OCR" });
            }
        }

        [HttpPost("scan")]
        public async Task<IActionResult> UploadScan(
         IFormFile file,
         [FromForm] int? AbteilungId,
         [FromForm] string? category,
         [FromForm] string? Titel,
         [FromForm] string? Beschreibung,
         [FromForm] string? Rechnungsnummer,
         [FromForm] string? Kundennummer,
         [FromForm] string? Rechnungsbetrag,
         [FromForm] string? Nettobetrag,
         [FromForm] string? Gesamtpreis,
         [FromForm] string? Steuerbetrag,
         [FromForm] string? Rechnungsdatum,
         [FromForm] string? Lieferdatum,
         [FromForm] string? Faelligkeitsdatum,
         [FromForm] string? Zahlungsbedingungen,
         [FromForm] string? Lieferart,
         [FromForm] string? ArtikelAnzahl,
         [FromForm] string? Email,
         [FromForm] string? Telefon,
         [FromForm] string? Telefax,
         [FromForm] string? IBAN,
         [FromForm] string? BIC,
         [FromForm] string? Bankverbindung,
         [FromForm] string? SteuerNr,
         [FromForm] string? UIDNummer,
         [FromForm] string? Adresse,
         [FromForm] string? AbsenderAdresse,
         [FromForm] string? AnsprechPartner,
         [FromForm] string? Zeitraum,
         [FromForm] string? PdfAutor,
         [FromForm] string? PdfBetreff,
         [FromForm] string? PdfSchluesselwoerter,
         [FromForm] string? Website,
         [FromForm] string? OCRText
     )
        {
            try
            {
                _logger.LogInformation("📥 UploadScan gestartet");

                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "❌ Keine Datei erhalten." });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequest(new { success = false, message = "❌ Benutzer ist nicht eingeloggt." });

                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                    return BadRequest(new { success = false, message = "❌ Benutzer konnte nicht gefunden werden." });

                var firmaName = user?.FirmenName?.Trim()?.ToLowerInvariant() ?? "unbekannt";

                // 🔑 KundeId aus KundeBenutzer laden
                var kundeId = await _db.KundeBenutzer
                    .Where(kb => kb.ApplicationUserId == userId)
                    .Select(kb => kb.KundenId)
                    .FirstOrDefaultAsync();

                if (kundeId == 0)
                {
                    _logger.LogWarning("❌ Benutzer {UserId} ist keinem Kunden zugeordnet.", userId);
                    return BadRequest(new { success = false, message = "❌ Benutzer ist keinem Kunden zugeordnet." });
                }

                // 🔑 Abteilung auflösen
                Abteilung? abteilung;
                if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                {
                    if (user.AbteilungId == null)
                        return BadRequest(new { success = false, message = "❌ Benutzer ist keiner Abteilung zugeordnet." });

                    abteilung = await _db.Abteilungen.FindAsync(user.AbteilungId);
                }
                else
                {
                    if (AbteilungId == null || AbteilungId == 0)
                        return BadRequest(new { success = false, message = "❌ Bitte wählen Sie eine Abteilung aus." });

                    abteilung = await _db.Abteilungen.FindAsync(AbteilungId);
                }

                if (abteilung == null)
                    return BadRequest(new { success = false, message = "❌ Abteilung ungültig." });

                // ✅ Dateipfad aufbauen
                var (objectPath, abtId) = DocumentPathHelper.BuildFinalPath(
                    firma: firmaName,
                    fileName: $"{Path.GetFileNameWithoutExtension(file.FileName)}_scan_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}",
                    kategorie: category ?? "gescannte-dokumente",
                    abteilungId: abteilung.Id,
                    abteilungName: abteilung.Name
                );

                using (var stream = file.OpenReadStream())
                {
                    await _storageService.UploadStreamAsync(stream, objectPath, file.ContentType);
                }

                // 💾 Metadaten speichern
                var meta = new Metadaten
                {
                    Titel = Titel ?? Path.GetFileNameWithoutExtension(file.FileName),
                    Beschreibung = Beschreibung,
                    Kategorie = "gescannte-dokumente",
                    Rechnungsnummer = Rechnungsnummer,
                    Kundennummer = Kundennummer,
                    Rechnungsbetrag = ParseDecimal(Rechnungsbetrag),
                    Nettobetrag = ParseDecimal(Nettobetrag),
                    Gesamtpreis = ParseDecimal(Gesamtpreis),
                    Steuerbetrag = ParseDecimal(Steuerbetrag),
                    Rechnungsdatum = ParseDate(Rechnungsdatum),
                    Lieferdatum = ParseDate(Lieferdatum),
                    Faelligkeitsdatum = ParseDate(Faelligkeitsdatum),
                    Zahlungsbedingungen = Zahlungsbedingungen,
                    Lieferart = Lieferart,
                    ArtikelAnzahl = ParseInt(ArtikelAnzahl),
                    Email = Email,
                    Telefon = Telefon,
                    Telefax = Telefax,
                    IBAN = IBAN,
                    BIC = BIC,
                    Bankverbindung = Bankverbindung,
                    SteuerNr = SteuerNr,
                    UIDNummer = UIDNummer,
                    Adresse = Adresse,
                    AbsenderAdresse = AbsenderAdresse,
                    AnsprechPartner = AnsprechPartner,
                    Zeitraum = Zeitraum,
                    PdfAutor = PdfAutor,
                    PdfBetreff = PdfBetreff,
                    PdfSchluesselwoerter = PdfSchluesselwoerter,
                    Website = Website,
                    OCRText = OCRText
                };

                // In DB speichern, um ID zu erhalten
                await _db.Metadaten.AddAsync(meta);
                await _db.SaveChangesAsync();

                // 💾 Dokument speichern
                var newDoc = new Dokumente
                {
                    Id = Guid.NewGuid(),
                    ApplicationUserId = userId,
                    KundeId = kundeId,
                    Titel = Titel ?? Path.GetFileNameWithoutExtension(file.FileName),
                    Beschreibung = Beschreibung,
                    Kategorie = "gescannte-dokumente",
                    ErkannteKategorie = category ?? "allgemein",
                    Dateiname = Path.GetFileName(objectPath),
                    Dateipfad = $"https://storage.googleapis.com/{_storageService.Bucket}/{objectPath}",
                    ObjectPath = objectPath,
                    HochgeladenAm = DateTime.UtcNow,
                    dtStatus = DokumentStatus.Neu,
                    AbteilungId = abtId,

                    // 🔗 Verknüpfung mit Metadaten
                    MetadatenId = meta.Id,
                    MetadatenObjekt = meta
                };

                // Dokument in DB speichern
                _db.Dokumente.Add(newDoc);
                await _db.SaveChangesAsync();

                Console.WriteLine($"✅ Neues Dokument gespeichert: {newDoc.Dateiname} mit Metadaten-ID {meta.Id}");


                return Ok(new { success = true, redirectUrl = "/Dokument/Index" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Fehler beim UploadScan");
                return StatusCode(500, new { success = false, message = "❌ Unerwarteter Fehler beim Upload. Details siehe Server-Logs." });
            }
        }


        private decimal? ParseDecimal(string? input)
        {
            if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;
            return null;
        }

        private DateTime? ParseDate(string? input)
        {
            if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                return result;
            return null;
        }

        private int? ParseInt(string? input)
        {
            if (int.TryParse(input, out var result))
                return result;
            return null;
        }



    }
}
