using DmsProjeckt.Data;
using DmsProjeckt.Helpers;
using DmsProjeckt.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Pages.Dokument
{
    [IgnoreAntiforgeryToken]
    public class BearbeitenModel : PageModel
    {
        private readonly FirebaseStorageService _firebaseStorageService;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<BearbeitenModel> _logger;
        private readonly DocumentHashService _documentHashService;
        public class TempSignatureStore
        {
            public Guid FileId { get; set; }
            public List<SignaturePayload> Signatures { get; set; } = new();
        }

        // Cache für Signaturen (kann später Redis oder DB sein)
        private static readonly Dictionary<Guid, TempSignatureStore> _pendingSignatures = new();

        public BearbeitenModel(
            FirebaseStorageService firebaseStorageService,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            ILogger<BearbeitenModel> logger,
            DocumentHashService documentHashService)
        {
            _firebaseStorageService = firebaseStorageService;
            _dbContext = dbContext;
            _userManager = userManager;
            _logger = logger;
            _documentHashService = documentHashService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }
        [BindProperty(SupportsGet = true)]
        public bool FromTask { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string OriginalPath { get; set; } = string.Empty;
        public string SasUrl { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id == null)
                return BadRequest("❌ Dokument-Id fehlt.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            // 🔎 Dokument direkt aus der DB anhand Id laden
            var dokument = await _dbContext.Dokumente.FindAsync(Id);
            if (dokument == null)
            {
                _logger.LogWarning("❌ Dokument mit Id {Id} nicht gefunden", Id);
                return NotFound();
            }

            // Wir nehmen den Dateipfad aus der DB
            var filePath = dokument.ObjectPath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return BadRequest("❌ Dokument hat keinen gültigen Dateipfad.");
            }

            OriginalPath = filePath;

            // Falls nicht PDF → konvertieren
            if (!filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                using var ms = new MemoryStream();
                await _firebaseStorageService.DownloadToStreamAsync(filePath, ms);
                var fileBytes = ms.ToArray();

                var pdfStream = FileConversionHelper.ConvertToPdf(filePath, fileBytes);
                pdfStream.Position = 0;

                string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                string newFileName = fileNameWithoutExt + ".pdf";
                string newPath = Path.Combine(directory, newFileName).Replace("\\", "/");

                await _firebaseStorageService.UploadStreamAsync(pdfStream, newPath, "application/pdf");

                filePath = newPath;
            }

            // Download-URL aus Firebase
            var exists = await _firebaseStorageService.ObjectExistsAsync(filePath);
            if (exists)
            {
                SasUrl = await _firebaseStorageService.GetDownloadUrlAsync(filePath, 15);
            }
            else
            {
                _logger.LogWarning("❌ Datei nicht gefunden: {FilePath}", filePath);
                SasUrl = string.Empty;
            }
            _logger.LogInformation("Geladenes Dokument: {Id}, IsVersion={IsVersion}, Pfad={Path}",
    dokument.Id, dokument.IsVersion, dokument.ObjectPath);

            return Page();
        }
        [HttpPost]
        public async Task<IActionResult> OnPostSaveSignature([FromBody] SignaturePayload payload)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.ImageBase64))
                return BadRequest("❌ Ungültige Signatur-Daten.");

            var dokument = await _dbContext.Dokumente.FindAsync(payload.FileId);
            if (dokument == null)
                return NotFound("❌ Dokument nicht gefunden.");

            try
            {
                // --- Original-PDF laden ---
                var inputStream = new MemoryStream();
                await _firebaseStorageService.DownloadToStreamAsync(dokument.ObjectPath, inputStream);
                inputStream.Position = 0;

                var outputStream = new MemoryStream();

                // --- iText starten ---
                var pdfReader = new iText.Kernel.Pdf.PdfReader(inputStream);
                var pdfWriter = new iText.Kernel.Pdf.PdfWriter(outputStream, new iText.Kernel.Pdf.WriterProperties().SetFullCompressionMode(true));

                var pdfDoc = new iText.Kernel.Pdf.PdfDocument(pdfReader, pdfWriter);
                var doc = new iText.Layout.Document(pdfDoc);

                // --- Bild-Daten vorbereiten ---
                var base64Data = payload.ImageBase64.Contains(",")
                    ? payload.ImageBase64.Split(',')[1]
                    : payload.ImageBase64;
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                var img = new iText.Layout.Element.Image(iText.IO.Image.ImageDataFactory.Create(imageBytes));
                img.ScaleToFit(payload.Width, payload.Height);

                // --- Position berechnen ---
                var page = pdfDoc.GetPage(payload.PageNumber);
                var pageHeight = page.GetPageSize().GetHeight();


                img.SetFixedPosition(payload.PageNumber, payload.X, payload.Y, payload.Width);

                // --- Signatur ins Dokument einfügen ---
                doc.Add(img);

                // ⚡ Alles sauber schließen
                doc.Close();
                pdfDoc.Close();
                pdfReader.Close();
                pdfWriter.Close();

                // --- Stream zurücksetzen ---
                outputStream.Position = 0;

                // --- Upload ins Firebase ---
                await _firebaseStorageService.UploadStreamAsync(outputStream, dokument.ObjectPath, "application/pdf");

                // --- Dokument als signiert markieren ---
                dokument.EstSigne = true;
                _dbContext.Update(dokument);
                await _dbContext.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "✔️ Signatur gespeichert." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Fehler beim direkten Speichern der Signatur für Dokument {Id}", payload.FileId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        public async Task<IActionResult> OnGetUserSignature()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(user.SignaturePath))
                return new JsonResult(new { success = false, message = "❌ Keine Signatur gefunden." });

            try
            {
                using var ms = new MemoryStream();
                await _firebaseStorageService.DownloadToStreamAsync(user.SignaturePath, ms);

                if (ms.Length == 0)
                {
                    return new JsonResult(new { success = false, message = "❌ Signatur-Datei leer oder nicht gefunden." });
                }

                var base64 = Convert.ToBase64String(ms.ToArray());
                return new JsonResult(new { success = true, base64 = $"data:image/png;base64,{base64}" });
            }
            catch (Exception ex)
            {
                // 🔥 Hier sauber Fehler zurückgeben
                return new JsonResult(new { success = false, message = $"Fehler beim Laden der Signatur: {ex.Message}" });
            }
        }



        public async Task<IActionResult> OnPostSaveUserSignature([FromBody] SignatureSaveRequest payload)
        {
            if (payload == null || string.IsNullOrWhiteSpace(payload.ImageBase64))
                return BadRequest(new { success = false, message = "❌ Ungültige Signatur-Daten." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            try
            {
                // Falls schon eine Signatur existiert → nur löschen, wenn sie wirklich existiert
                if (!string.IsNullOrWhiteSpace(user.SignaturePath) &&
                    await _firebaseStorageService.ObjectExistsAsync(user.SignaturePath))
                {
                    await _firebaseStorageService.DeleteFileAsync(user.SignaturePath);
                }

                // Neue Signatur speichern
                var base64Data = payload.ImageBase64.Contains(",")
                    ? payload.ImageBase64.Split(',')[1]
                    : payload.ImageBase64;

                byte[] imageBytes = Convert.FromBase64String(base64Data);
                var firma = user.FirmenName?.ToLower() ?? "default";

                string path = $"dokumente/{firma}/signatures/{user.Id}.png";
                using var ms = new MemoryStream(imageBytes);

                await _firebaseStorageService.UploadStreamAsync(ms, path, "image/png");

                // Pfad aktualisieren
                user.SignaturePath = path;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                    return StatusCode(500, new { success = false, message = "❌ User konnte nicht aktualisiert werden." });

                return new JsonResult(new { success = true, message = "✔️ Neue Signatur gespeichert.", path });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Fehler beim Speichern der Signatur");
                return StatusCode(500, new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }




        public async Task<IActionResult> OnPostSaveWithName([FromBody] SaveRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FileName))
                return new JsonResult(new { success = false, message = "❌ Ungültige Daten" });
            _logger.LogInformation("📥 SaveWithName Request erhalten: {Id}, Datei={Name}", request.FileId, request.FileName);


            if (FromTask)
            {
                // ⚡ Dokument direkt überschreiben
                return await SaveOverwriteAsync(request);
            }
            else
            {
                // ✅ Neue Version anlegen
                return await SaveAsNewVersionAsync(request);
            }
        }


        public async Task<IActionResult> OnGetFileNameAsync(Guid id)
        {
            var dokument = await _dbContext.Dokumente.FindAsync(id);
            if (dokument == null)
            {
                return new JsonResult(new { success = false, message = "❌ Dokument nicht gefunden." });
            }

            return new JsonResult(new
            {
                success = true,
                suggestedName = string.IsNullOrWhiteSpace(dokument.Dateiname)
                    ? $"Dokument_{DateTime.UtcNow:yyyy-MM-dd}.pdf"
                    : dokument.Dateiname
            });
        }


        public async Task<IActionResult> OnGetPdfProxyAsync(Guid id)
        {
            var dokument = await _dbContext.Dokumente.FindAsync(id);
            if (dokument == null)
                return NotFound("❌ Dokument nicht gefunden.");

            try
            {
                var ms = new MemoryStream();
                await _firebaseStorageService.DownloadToStreamAsync(dokument.ObjectPath, ms);
                ms.Position = 0;

                if (ms.Length == 0)
                {
                    _logger.LogError("❌ Proxy: Geladene PDF ist leer. Pfad={Path}", dokument.ObjectPath);
                    return BadRequest("❌ Fehler: PDF leer oder nicht gefunden.");
                }

                _logger.LogInformation("📄 Proxy liefert PDF zurück: Id={Id}, Bytes={Bytes}, Path={Path}",
                    dokument.Id, ms.Length, dokument.ObjectPath);

                // ⚡ Stream direkt zurückgeben
                return File(ms, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Fehler beim Laden von PDF {DokumentId} aus Firebase", id);
                return BadRequest("❌ Fehler beim PDF-Laden: " + ex.Message);
            }
        }

        public async Task<JsonResult> OnGetOriginalMetadataAsync(Guid id)
        {
            // 1️⃣ Dokument inkl. Metadaten laden
            var dokument = await _dbContext.Dokumente
                .Include(d => d.MetadatenObjekt)
                .FirstOrDefaultAsync(d => d.Id == id || d.OriginalId == id);

            if (dokument == null)
            {
                return new JsonResult(new { success = false, message = "❌ Originaldokument nicht gefunden." });
            }

            // 2️⃣ Metadatenquelle bestimmen
            var meta = dokument.MetadatenObjekt;

            if (meta == null)
            {
                return new JsonResult(new { success = false, message = "⚠️ Keine Metadaten zu diesem Dokument gefunden." });
            }

            // 3️⃣ Metadaten serialisieren
            var metadata = new
            {
                titel = meta.Titel,
                beschreibung = meta.Beschreibung,
                kategorie = meta.Kategorie,
                stichworte = meta.Stichworte,

                rechnungsnummer = meta.Rechnungsnummer,
                kundennummer = meta.Kundennummer,
                rechnungsbetrag = meta.Rechnungsbetrag,
                nettobetrag = meta.Nettobetrag,
                gesamtpreis = meta.Gesamtpreis,
                steuerbetrag = meta.Steuerbetrag,

                rechnungsdatum = meta.Rechnungsdatum,
                lieferdatum = meta.Lieferdatum,
                faelligkeitsdatum = meta.Faelligkeitsdatum,

                zahlungsbedingungen = meta.Zahlungsbedingungen,
                lieferart = meta.Lieferart,
                artikelanzahl = meta.ArtikelAnzahl,

                email = meta.Email,
                telefon = meta.Telefon,
                telefax = meta.Telefax,

                iban = meta.IBAN,
                bic = meta.BIC,
                bankverbindung = meta.Bankverbindung,

                steuernr = meta.SteuerNr,
                uidnummer = meta.UIDNummer,

                adresse = meta.Adresse,
                absenderadresse = meta.AbsenderAdresse,
                ansprechpartner = meta.AnsprechPartner,
                zeitraum = meta.Zeitraum,

                pdfautor = meta.PdfAutor,
                pdfbetreff = meta.PdfBetreff,
                pdfschluesselwoerter = meta.PdfSchluesselwoerter,
                website = meta.Website,
                ocrtext = meta.OCRText,

                // 🔹 Dateiinfos bleiben im Dokument selbst
                filesizebytes = dokument.FileSizeBytes,
                dateiname = dokument.Dateiname,
                hochgeladenam = dokument.HochgeladenAm,
                abteilung = dokument.Abteilung?.Name ?? "Unbekannt"
            };

            return new JsonResult(new { success = true, metadata });
        }




        public class SaveRequest
        {
            public string FileName { get; set; } = string.Empty;
            public List<SignaturePayload> Signatures { get; set; } = new();
            public OriginalMetadataDto Metadata { get; set; }
            public Guid FileId { get; set; } // 🔥 neu, damit Dokument auch ohne Signatur referenziert werden kann
            public List<HighlightPayload> Highlights { get; set; } = new(); // 🔥 NEU

        }

        private bool HasMetadataChanged(Dokumente original, OriginalMetadataDto meta)
        {
            var m = original.MetadatenObjekt;

            if (m == null)
                return true; // 🔸 Kein Metadatenobjekt vorhanden → gilt als geändert

            return
                m.Beschreibung != meta.Beschreibung
                || m.Rechnungsnummer != meta.Rechnungsnummer
                || m.Kundennummer != meta.Kundennummer
                || m.Rechnungsbetrag != meta.Rechnungsbetrag
                || m.Nettobetrag != meta.Nettobetrag
                || m.Gesamtpreis != meta.Gesamtpreis
                || m.Steuerbetrag != meta.Steuerbetrag
                || m.Rechnungsdatum != meta.Rechnungsdatum
                || m.Lieferdatum != meta.Lieferdatum
                || m.Faelligkeitsdatum != meta.Faelligkeitsdatum
                || m.Zahlungsbedingungen != meta.Zahlungsbedingungen
                || m.Lieferart != meta.Lieferart
                || m.ArtikelAnzahl != meta.ArtikelAnzahl
                || m.Email != meta.Email
                || m.Telefon != meta.Telefon
                || m.Telefax != meta.Telefax
                || m.IBAN != meta.IBAN
                || m.BIC != meta.BIC
                || m.Bankverbindung != meta.Bankverbindung
                || m.SteuerNr != meta.SteuerNr
                || m.UIDNummer != meta.UIDNummer
                || m.Adresse != meta.Adresse
                || m.AbsenderAdresse != meta.AbsenderAdresse
                || m.AnsprechPartner != meta.AnsprechPartner
                || m.Zeitraum != meta.Zeitraum
                || m.PdfAutor != meta.PdfAutor
                || m.PdfBetreff != meta.PdfBetreff
                || m.PdfSchluesselwoerter != meta.PdfSchluesselwoerter
                || m.Website != meta.Website
                || m.OCRText != meta.OCRText
                || original.FileSizeBytes != meta.FileSizeBytes;
        }
        private async Task<JsonResult> SaveAsNewVersionAsync(SaveRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.FileName))
                    return new JsonResult(new { success = false, message = "❌ Ungültige Daten" });

                var dokument = await _dbContext.Dokumente
                    .Include(d => d.MetadatenObjekt)
                    .Include(d => d.Abteilung)
                    .FirstOrDefaultAsync(d => d.Id == request.FileId);

                if (dokument == null)
                    return new JsonResult(new { success = false, message = "❌ Dokument nicht gefunden" });

                // === 1️⃣ PDF original laden ===
                using var ms = new MemoryStream();
                await _firebaseStorageService.DownloadToStreamAsync(dokument.ObjectPath, ms);
                ms.Position = 0;
                byte[] originalBytes = ms.ToArray();

                // === 2️⃣ Neues PDF generieren (nur bei visuellen Änderungen) ===
                byte[] newPdfBytes;
                bool hasPdfChanges =
                    (request.Signatures != null && request.Signatures.Any()) ||
                    (request.Highlights != null && request.Highlights.Any());

                if (!hasPdfChanges)
                {
                    // ♻️ Kein visueller Unterschied → PDF unverändert
                    newPdfBytes = originalBytes;
                    _logger.LogInformation("♻️ Keine visuelle Änderung erkannt – Original PDF bleibt unverändert.");
                }
                else
                {
                    var outputStream = new MemoryStream();
                    using (var reader = new iText.Kernel.Pdf.PdfReader(new MemoryStream(originalBytes)))
                    using (var writer = new iText.Kernel.Pdf.PdfWriter(outputStream))
                    using (var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader, writer))
                    using (var doc = new iText.Layout.Document(pdfDoc))
                    {
                        // === Highlights einfügen ===
                        if (request.Highlights != null && request.Highlights.Any())
                        {
                            foreach (var hl in request.Highlights)
                            {
                                if (string.IsNullOrWhiteSpace(hl.ImageBase64))
                                    continue;

                                var base64 = hl.ImageBase64.Contains(",") ? hl.ImageBase64.Split(',')[1] : hl.ImageBase64;
                                byte[] imgBytes = Convert.FromBase64String(base64);
                                var img = new iText.Layout.Element.Image(iText.IO.Image.ImageDataFactory.Create(imgBytes));
                                img.SetOpacity(0.4f);

                                var page = pdfDoc.GetPage(hl.PageNumber);
                                float width = page.GetPageSize().GetWidth();
                                float height = page.GetPageSize().GetHeight();

                                img.ScaleAbsolute(width, height);
                                img.SetFixedPosition(hl.PageNumber, 0, 0);
                                doc.Add(img);
                            }
                        }

                        // === Signaturen einfügen ===
                        if (request.Signatures != null && request.Signatures.Any())
                        {
                            foreach (var sig in request.Signatures)
                            {
                                var base64 = sig.ImageBase64.Contains(",") ? sig.ImageBase64.Split(',')[1] : sig.ImageBase64;
                                byte[] imgBytes = Convert.FromBase64String(base64);
                                var img = new iText.Layout.Element.Image(iText.IO.Image.ImageDataFactory.Create(imgBytes));
                                var page = pdfDoc.GetPage(sig.PageNumber);

                                float width = page.GetPageSize().GetWidth();
                                float height = page.GetPageSize().GetHeight();

                                float x = sig.RelX * width;
                                float y = (1 - sig.RelY - sig.RelH) * height;
                                float w = sig.RelW * width;
                                float h = sig.RelH * height;

                                img.ScaleAbsolute(w, h);
                                img.SetFixedPosition(sig.PageNumber, x, y);
                                doc.Add(img);
                            }
                        }
                    }

                    newPdfBytes = outputStream.ToArray();
                    _logger.LogInformation("🧩 Neues PDF mit visuellen Änderungen generiert.");
                }

                // === 3️⃣ Hash vergleichen ===
                string oldHash = dokument.FileHash;
                using var msHash = new MemoryStream(newPdfBytes);
                string newHash = _documentHashService.ComputeHash(msHash);

                // === 4️⃣ Gleicher Hash → nur Metadaten aktualisieren ===
                // === 4️⃣ Gleicher Hash → nur Metadaten aktualisieren ===
                if (!string.IsNullOrWhiteSpace(oldHash) && oldHash == newHash)
                {
                    _logger.LogInformation("♻️ Gleicher Hash erkannt – nur Metadaten werden aktualisiert.");

                    // 🔍 Si le document est déjà une version, on met à jour ses propres métadaten
                    if (dokument.IsVersion)
                    {
                        var meta = dokument.MetadatenObjekt ?? new Metadaten();
                        if (request.Metadata != null)
                        {
                            meta.Titel = request.Metadata.Titel ?? meta.Titel;
                            meta.Beschreibung = request.Metadata.Beschreibung ?? meta.Beschreibung;
                            meta.Kategorie = request.Metadata.Kategorie ?? meta.Kategorie;
                            meta.Rechnungsnummer = request.Metadata.Rechnungsnummer ?? meta.Rechnungsnummer;
                            meta.Kundennummer = request.Metadata.Kundennummer ?? meta.Kundennummer;
                            meta.Rechnungsbetrag = request.Metadata.Rechnungsbetrag ?? meta.Rechnungsbetrag;
                            meta.Nettobetrag = request.Metadata.Nettobetrag ?? meta.Nettobetrag;
                            meta.Steuerbetrag = request.Metadata.Steuerbetrag ?? meta.Steuerbetrag;
                            meta.Gesamtpreis = request.Metadata.Gesamtpreis ?? meta.Gesamtpreis;
                            meta.Email = request.Metadata.Email ?? meta.Email;
                            meta.Adresse = request.Metadata.Adresse ?? meta.Adresse;
                            meta.AnsprechPartner = request.Metadata.AnsprechPartner ?? meta.AnsprechPartner;
                        }

                        dokument.MetadatenObjekt = meta;
                        dokument.HochgeladenAm = DateTime.UtcNow;
                        _dbContext.Update(dokument);
                        await _dbContext.SaveChangesAsync();

                        return new JsonResult(new
                        {
                            success = true,
                            message = "✔️ Metadaten für Version aktualisiert – keine neue Version erstellt.",
                            dokumentId = dokument.Id
                        });
                    }
                    else
                    {
                        // 🔁 Si c’est l’original → on met à jour la DERNIÈRE version liée
                        var letzteVersion = await _dbContext.DokumentVersionen
                            .OrderByDescending(v => v.HochgeladenAm)
                            .FirstOrDefaultAsync(v => v.DokumentId == dokument.Id);

                        if (letzteVersion != null)
                        {
                            letzteVersion.MetadataJson = System.Text.Json.JsonSerializer.Serialize(request.Metadata);
                            letzteVersion.HochgeladenAm = DateTime.UtcNow;
                            await _dbContext.SaveChangesAsync();

                            return new JsonResult(new
                            {
                                success = true,
                                message = "✔️ Metadaten auf die letzte Version übertragen – keine neue Version erstellt.",
                                dokumentId = letzteVersion.DokumentId
                            });
                        }

                        // ⚠️ Si aucune version n’existe → update l’original (fallback)
                        _logger.LogWarning("⚠️ Keine Version gefunden – Metadaten direkt im Original gespeichert.");
                        dokument.MetadatenObjekt ??= new Metadaten();
                        dokument.MetadatenObjekt.Beschreibung = request.Metadata?.Beschreibung ?? dokument.MetadatenObjekt.Beschreibung;
                        await _dbContext.SaveChangesAsync();
                    }
                }


                // === 5️⃣ Unterschiedlicher Hash → Neue Version erstellen ===
                _logger.LogInformation("🆕 Neuer Hash erkannt – Neue Version wird erstellt.");

                // 🔹 Pfade vorbereiten (gleiche Abteilung, Unterordner /versionen/)
                // 🔹 Récupère l’utilisateur connecté
                var user = await _userManager.GetUserAsync(User);
                string firma = user?.FirmenName?.Trim().ToLowerInvariant() ?? "allgemein";

                string abteilung = dokument.Abteilung?.Name?.ToLower() ?? "allgemein";
                string versionFileName = $"{Path.GetFileNameWithoutExtension(request.FileName)}_v{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
                string versionPath = $"dokumente/{firma}/{abteilung}/versionen/{versionFileName}";

                // 🔹 Upload neuer Version (Original bleibt unverändert)
                using var versionStream = new MemoryStream(newPdfBytes);
                await _firebaseStorageService.UploadAsync(versionStream, versionPath);

                _logger.LogInformation($"✅ Neue Version hochgeladen: {versionPath}");

                // 🔹 Neue Metadaten erzeugen
                var metaSource = dokument.MetadatenObjekt ?? new Metadaten();
                var metaDto = request.Metadata ?? new OriginalMetadataDto();

                var newMeta = new Metadaten
                {
                    Titel = metaDto.Titel ?? metaSource.Titel,
                    Beschreibung = metaDto.Beschreibung ?? metaSource.Beschreibung,
                    Kategorie = "versionen", // 🧩 immer im Versionen-Ordner
                    Rechnungsnummer = metaDto.Rechnungsnummer ?? metaSource.Rechnungsnummer,
                    Kundennummer = metaDto.Kundennummer ?? metaSource.Kundennummer,
                    Rechnungsbetrag = metaDto.Rechnungsbetrag ?? metaSource.Rechnungsbetrag,
                    Nettobetrag = metaDto.Nettobetrag ?? metaSource.Nettobetrag,
                    Steuerbetrag = metaDto.Steuerbetrag ?? metaSource.Steuerbetrag,
                    Gesamtpreis = metaDto.Gesamtpreis ?? metaSource.Gesamtpreis,
                    Email = metaDto.Email ?? metaSource.Email,
                    Adresse = metaDto.Adresse ?? metaSource.Adresse,
                    AnsprechPartner = metaDto.AnsprechPartner ?? metaSource.AnsprechPartner
                };

                await _dbContext.Metadaten.AddAsync(newMeta);
                await _dbContext.SaveChangesAsync();

                // 🔹 Neue Version-Datenbankeintrag
                 user = await _userManager.GetUserAsync(User);

                var newVersion = new DokumentVersionen
                {
                    DokumentId = dokument.Id,
                    Dateiname = versionFileName,
                    Dateipfad = $"https://storage.googleapis.com/{_firebaseStorageService.Bucket}/{versionPath}",
                    ObjectPath = versionPath,
                    HochgeladenAm = DateTime.UtcNow,
                    ApplicationUserId = user.Id,
                    AbteilungId = dokument.AbteilungId,
                    VersionsLabel = $"v{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                    MetadataJson = System.Text.Json.JsonSerializer.Serialize(newMeta),
                    IsVersion = true // ✅ für Anzeige im Frontend
                };

                _dbContext.DokumentVersionen.Add(newVersion);
                await _dbContext.SaveChangesAsync();

                return new JsonResult(new
                {
                    success = true,
                    message = $"🆕 Neue Version erstellt unter '{versionPath}'.",
                    dokumentId = dokument.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Fehler beim Speichern einer neuen Version");
                return new JsonResult(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        private async Task<JsonResult> SaveOverwriteAsync(SaveRequest request)
        {
            var dokument = await _dbContext.Dokumente
                .Include(d => d.MetadatenObjekt)
                .FirstOrDefaultAsync(d => d.Id == request.FileId);

            if (dokument == null)
                return new JsonResult(new { success = false, message = "❌ Dokument nicht gefunden" });

            try
            {
                using var ms = new MemoryStream();
                await _firebaseStorageService.DownloadToStreamAsync(dokument.ObjectPath, ms);
                ms.Position = 0;

                var outputStream = new MemoryStream();

                var writerProps = new iText.Kernel.Pdf.WriterProperties().SetFullCompressionMode(true);
                var pdfWriter = new iText.Kernel.Pdf.PdfWriter(outputStream, writerProps);
                pdfWriter.SetCloseStream(false);

                using (var reader = new iText.Kernel.Pdf.PdfReader(ms))
                using (var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader, pdfWriter))
                using (var doc = new iText.Layout.Document(pdfDoc))
                {
                    // === Highlights einbetten ===
                    if (request.Highlights != null && request.Highlights.Any())
                    {
                        foreach (var hl in request.Highlights)
                        {
                            if (string.IsNullOrWhiteSpace(hl.ImageBase64))
                                continue;

                            var base64Data = hl.ImageBase64.Contains(",")
                                ? hl.ImageBase64.Split(',')[1]
                                : hl.ImageBase64;

                            byte[] imageBytes = Convert.FromBase64String(base64Data);
                            var img = new iText.Layout.Element.Image(
                                iText.IO.Image.ImageDataFactory.Create(imageBytes)
                            );

                            img.SetOpacity(0.4f);
                            var page = pdfDoc.GetPage(hl.PageNumber);
                            float width = page.GetPageSize().GetWidth();
                            float height = page.GetPageSize().GetHeight();

                            img.ScaleAbsolute(width, height);
                            img.SetFixedPosition(hl.PageNumber, 0, 0);
                            doc.Add(img);
                        }
                    }

                    // === Signaturen einbetten ===
                    if (request.Signatures != null && request.Signatures.Any())
                    {
                        foreach (var sig in request.Signatures)
                        {
                            byte[] imgBytes = Convert.FromBase64String(sig.ImageBase64.Split(',')[1]);
                            var img = new iText.Layout.Element.Image(
                                iText.IO.Image.ImageDataFactory.Create(imgBytes));

                            var page = pdfDoc.GetPage(sig.PageNumber);
                            float pageWidth = page.GetPageSize().GetWidth();
                            float pageHeight = page.GetPageSize().GetHeight();

                            float x = sig.RelX * pageWidth;
                            float w = sig.RelW * pageWidth;
                            float h = sig.RelH * pageHeight;
                            float y = (1 - sig.RelY - sig.RelH) * pageHeight;

                            img.ScaleAbsolute(w, h);
                            img.SetFixedPosition(sig.PageNumber, x, y);
                            doc.Add(img);

                            _logger.LogInformation($"✔ Signatur eingebettet (X={x}, Y={y}, W={w}, H={h})");
                        }
                    }
                }

                // ============================
                // 🔐 HASH & Firebase Upload
                // ============================
                var pdfBytes = outputStream.ToArray();
                var (reused, firebasePath, hash) = await _documentHashService.SaveOrReuseAsync(dokument.Id, pdfBytes);

                dokument.FileHash = hash;
                dokument.ObjectPath = firebasePath;

                if (reused)
                    _logger.LogInformation("♻️ Bestehende Datei wiederverwendet: {Path}", firebasePath);
                else
                    _logger.LogInformation("📤 Neue Datei hochgeladen: {Path}", firebasePath);

                // === METADATEN aktualisieren ===
                if (request.Metadata != null)
                {
                    var meta = dokument.MetadatenObjekt ?? new Metadaten();

                    meta.Titel = request.Metadata.Titel ?? meta.Titel;
                    meta.Beschreibung = request.Metadata.Beschreibung ?? meta.Beschreibung;
                    meta.Kategorie = request.Metadata.Kategorie ?? meta.Kategorie;
                    meta.Stichworte = request.Metadata.PdfSchluesselwoerter ?? meta.Stichworte;

                    meta.Rechnungsnummer = request.Metadata.Rechnungsnummer ?? meta.Rechnungsnummer;
                    meta.Kundennummer = request.Metadata.Kundennummer ?? meta.Kundennummer;
                    meta.Rechnungsbetrag = request.Metadata.Rechnungsbetrag ?? meta.Rechnungsbetrag;
                    meta.Nettobetrag = request.Metadata.Nettobetrag ?? meta.Nettobetrag;
                    meta.Gesamtpreis = request.Metadata.Gesamtpreis ?? meta.Gesamtpreis;
                    meta.Steuerbetrag = request.Metadata.Steuerbetrag ?? meta.Steuerbetrag;

                    meta.Rechnungsdatum = request.Metadata.Rechnungsdatum ?? meta.Rechnungsdatum;
                    meta.Lieferdatum = request.Metadata.Lieferdatum ?? meta.Lieferdatum;
                    meta.Faelligkeitsdatum = request.Metadata.Faelligkeitsdatum ?? meta.Faelligkeitsdatum;
                    meta.Zahlungsbedingungen = request.Metadata.Zahlungsbedingungen ?? meta.Zahlungsbedingungen;
                    meta.Lieferart = request.Metadata.Lieferart ?? meta.Lieferart;
                    meta.ArtikelAnzahl = request.Metadata.ArtikelAnzahl ?? meta.ArtikelAnzahl;

                    meta.Email = request.Metadata.Email ?? meta.Email;
                    meta.Telefon = request.Metadata.Telefon ?? meta.Telefon;
                    meta.Telefax = request.Metadata.Telefax ?? meta.Telefax;
                    meta.IBAN = request.Metadata.IBAN ?? meta.IBAN;
                    meta.BIC = request.Metadata.BIC ?? meta.BIC;
                    meta.Bankverbindung = request.Metadata.Bankverbindung ?? meta.Bankverbindung;
                    meta.SteuerNr = request.Metadata.SteuerNr ?? meta.SteuerNr;
                    meta.UIDNummer = request.Metadata.UIDNummer ?? meta.UIDNummer;

                    meta.Adresse = request.Metadata.Adresse ?? meta.Adresse;
                    meta.AbsenderAdresse = request.Metadata.AbsenderAdresse ?? meta.AbsenderAdresse;
                    meta.AnsprechPartner = request.Metadata.AnsprechPartner ?? meta.AnsprechPartner;
                    meta.Zeitraum = request.Metadata.Zeitraum ?? meta.Zeitraum;

                    meta.PdfAutor = request.Metadata.PdfAutor ?? meta.PdfAutor;
                    meta.PdfBetreff = request.Metadata.PdfBetreff ?? meta.PdfBetreff;
                    meta.PdfSchluesselwoerter = request.Metadata.PdfSchluesselwoerter ?? meta.PdfSchluesselwoerter;
                    meta.Website = request.Metadata.Website ?? meta.Website;
                    meta.OCRText = request.Metadata.OCRText ?? meta.OCRText;

                    if (dokument.MetadatenObjekt == null)
                        _dbContext.Metadaten.Add(meta);

                    dokument.MetadatenObjekt = meta;
                }

                // === DOKUMENT-Infos aktualisieren ===
                dokument.HochgeladenAm = DateTime.UtcNow;
                dokument.FileSizeBytes = request.Metadata?.FileSizeBytes ?? dokument.FileSizeBytes;
                dokument.IsUpdated = true;

                _dbContext.Dokumente.Update(dokument);

                // === Aufgaben / Workflow-Status ===
                string redirectUrl = null;

                if (dokument.AufgabeId != null)
                {
                    var aufgabe = await _dbContext.Aufgaben.FindAsync(dokument.AufgabeId);
                    if (aufgabe != null)
                    {
                        aufgabe.Erledigt = true;
                        _dbContext.Aufgaben.Update(aufgabe);
                        redirectUrl = $"/Tests/Aufgaben/Details/{aufgabe.Id}";
                    }
                }
                else if (dokument.StepId != null && dokument.WorkflowId != null)
                {
                    var step = await _dbContext.Steps.FindAsync(dokument.StepId);
                    var aufgabe = await _dbContext.Aufgaben.FindAsync(dokument.AufgabeId);
                    if (step != null)
                    {
                        step.Completed = true;
                        if (aufgabe != null) aufgabe.Erledigt = true;
                        _dbContext.Steps.Update(step);
                        redirectUrl = $"/Workflow/StepDetails/{dokument.StepId}{dokument.WorkflowId}";
                    }
                }

                await _dbContext.SaveChangesAsync();

                return new JsonResult(new
                {
                    success = true,
                    message = reused
                        ? "♻️ Datei bereits vorhanden – keine neue Upload nötig."
                        : "✔️ Dokument erfolgreich überschrieben.",
                    dokumentId = dokument.Id,
                    redirectUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Fehler beim Überschreiben");
                return new JsonResult(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }


        [HttpPost]
        public IActionResult OnPostSaveTempSignatures([FromBody] TempSignatureStore payload)
        {
            if (payload == null || payload.FileId == Guid.Empty)
                return BadRequest(new { success = false, message = "❌ Ungültige Daten." });

            _pendingSignatures[payload.FileId] = payload;
            return new JsonResult(new { success = true });
        }

        // Zugriff von außen (für Metadaten-Seite)
        public static List<SignaturePayload> GetPendingSignatures(Guid fileId)
        {
            if (_pendingSignatures.TryGetValue(fileId, out var store))
                return store.Signatures;
            return new List<SignaturePayload>();
        }

        // Nach finalem Speichern aufräumen
        public static void ClearPendingSignatures(Guid fileId)
        {
            if (_pendingSignatures.ContainsKey(fileId))
                _pendingSignatures.Remove(fileId);
        }

        public async Task<IActionResult> OnPostSaveMetaAsync(Guid id, string title, string category)
        {
            var dokument = await _dbContext.Dokumente.FindAsync(id);
            if (dokument == null) return NotFound();

            // ✅ Metadaten speichern
            dokument.Titel = title;
            dokument.Kategorie = category;
            await _dbContext.SaveChangesAsync();

            // ✅ Hol Signaturen aus Bearbeiten.cshtml.cs
            var signatures = BearbeitenModel.GetPendingSignatures(id);

            if (signatures.Any())
            {
                using var ms = new MemoryStream();
                await _firebaseStorageService.DownloadToStreamAsync(dokument.ObjectPath, ms);
                ms.Position = 0;

                var outputStream = new MemoryStream();

                using (var reader = new iText.Kernel.Pdf.PdfReader(ms))
                using (var writer = new iText.Kernel.Pdf.PdfWriter(outputStream))
                using (var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader, writer))
                {
                    var doc = new iText.Layout.Document(pdfDoc);

                    foreach (var sig in signatures)
                    {
                        var base64Data = sig.ImageBase64.Contains(",")
                            ? sig.ImageBase64.Split(',')[1]
                            : sig.ImageBase64;

                        byte[] imageBytes = Convert.FromBase64String(base64Data);
                        var img = new iText.Layout.Element.Image(
                            iText.IO.Image.ImageDataFactory.Create(imageBytes)
                        );
                        img.ScaleToFit(sig.Width, sig.Height);

                        var page = pdfDoc.GetPage(sig.PageNumber);
                        var pageHeight = page.GetPageSize().GetHeight();


                        img.SetFixedPosition(sig.PageNumber, sig.X, sig.Y, sig.Width);

                        doc.Add(img);
                    }

                    doc.Close();
                }

                // Neue Version speichern
                outputStream.Position = 0;
                await _firebaseStorageService.UploadStreamAsync(outputStream, dokument.ObjectPath, "application/pdf");

                // Pending Signatures aufräumen
                BearbeitenModel.ClearPendingSignatures(id);
            }

            return RedirectToPage("/Dokument/AlleVersionen");
        }



        public async Task<IActionResult> OnGetStampText()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            string firma = user.FirmenName ?? "Firma Unbekannt";
            string datum = DateTime.UtcNow.ToString("dd.MM.yyyy");
            string text = $"{firma} - {datum}";

            return new JsonResult(new { success = true, text });
        }


        public class OriginalMetadataDto
        {

            public string Kategorie { get; set; }
            public string Titel { get; set; }
            public string Beschreibung { get; set; }
            public string Rechnungsnummer { get; set; }
            public string Kundennummer { get; set; }
            public decimal? Rechnungsbetrag { get; set; }
            public decimal? Nettobetrag { get; set; }
            public decimal? Gesamtpreis { get; set; }
            public decimal? Steuerbetrag { get; set; }
            public DateTime? Rechnungsdatum { get; set; }
            public DateTime? Lieferdatum { get; set; }
            public DateTime? Faelligkeitsdatum { get; set; }
            public string Zahlungsbedingungen { get; set; }
            public string Lieferart { get; set; }
            public int? ArtikelAnzahl { get; set; }
            public string Email { get; set; }
            public string Telefon { get; set; }
            public string Telefax { get; set; }
            public string IBAN { get; set; }
            public string BIC { get; set; }
            public string Bankverbindung { get; set; }
            public string SteuerNr { get; set; }
            public string UIDNummer { get; set; }
            public string Adresse { get; set; }
            public string AbsenderAdresse { get; set; }
            public string AnsprechPartner { get; set; }
            public string Zeitraum { get; set; }
            public string PdfAutor { get; set; }
            public string PdfBetreff { get; set; }
            public string PdfSchluesselwoerter { get; set; }
            public string Website { get; set; }
            public string OCRText { get; set; }
            public long? FileSizeBytes { get; set; }

        }


        public class SignatureSaveRequest
        {
            public string? ImageBase64 { get; set; }
        }

        public class SignaturePayload
        {
            public Guid FileId { get; set; }
            public int PageNumber { get; set; }
            public string ImageBase64 { get; set; } = string.Empty;

            // alte absolute Werte (kannst du behalten falls du sie noch nutzt)
            public float X { get; set; }
            public float Y { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }

            public float ViewportWidth { get; set; }
            public float ViewportHeight { get; set; }

            // ✅ neue relative Werte (Frontend sendet die)
            public float RelX { get; set; }
            public float RelY { get; set; }
            public float RelW { get; set; }
            public float RelH { get; set; }
        }

        public class HighlightPayload
        {
            public Guid FileId { get; set; }
            public int PageNumber { get; set; }
            public string ImageBase64 { get; set; } = string.Empty;
        }

    }
}
