using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using DmsProjeckt.Data;
using DmsProjeckt.Helpers;
using DmsProjeckt.Service;
using DmsProjeckt.Services;
using iTextSharp.text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static DmsProjeckt.Service.OcrMetadataExtractorService;

namespace DmsProjeckt.Pages.Workflows
{
    [IgnoreAntiforgeryToken]
    public class StepUploadModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<StepUploadModel> _logger;
        private readonly FirebaseStorageService _firebaseStorageService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AzureOcrService _ocrService;
        private readonly AuditLogService _auditLogService;
        private readonly LocalIndexService _localIndexService;
        private readonly PdfMetadataReader _pdfMetadataReader;
        private readonly VersionierungsService _versionierungsService;
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

       

        public StepUploadModel(
            ApplicationDbContext db,
            ILogger<StepUploadModel> logger,
            FirebaseStorageService firebaseStorageService,
            UserManager<ApplicationUser> userManager,
            LocalIndexService localIndexService,
            PdfMetadataReader pdfMetadataReader,
            AzureOcrService ocrService,
            AuditLogService auditLogService,
            VersionierungsService versionierungsService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger;
            _firebaseStorageService = firebaseStorageService ?? throw new ArgumentNullException(nameof(firebaseStorageService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _localIndexService = localIndexService ?? throw new ArgumentNullException(nameof(localIndexService));
            _pdfMetadataReader = pdfMetadataReader ?? throw new ArgumentNullException(nameof(pdfMetadataReader));
            _ocrService = ocrService;
            _auditLogService = auditLogService;
            _versionierungsService = versionierungsService;

        }

        // ... [bind properties inchangées, code de filtre, OnGetAsync inchangé] ...
        public string WorkflowTitle { get; set; }
        public string StepTitle { get; set; }
        public string StepAssignedUserName { get; set; }
        public string StepDescription { get; set; }
        public DateTime? StepDue { get; set; }
        [BindProperty(SupportsGet = true)] public int StepId { get; set; }
        [BindProperty(SupportsGet = true)] public int WorkflowId { get; set; }
        [BindProperty(SupportsGet = true)]
        public Guid? Id { get; set; }
        [BindProperty] public IFormFile Datei { get; set; }
        [BindProperty] public string Kategorie { get; set; } = "workflow";
        [BindProperty] public string Beschreibung { get; set; }
        [BindProperty] public string Titel { get; set; }
        [BindProperty] public string Rechnungsnummer { get; set; }
        [BindProperty] public string Kundennummer { get; set; }
        [BindProperty] public string Rechnungsbetrag { get; set; }
        [BindProperty] public string Rechnungsdatum { get; set; }
        [BindProperty] public string Lieferdatum { get; set; }
        [BindProperty] public string Faelligkeitsdatum { get; set; } // ✅ Ajouté
        [BindProperty] public string Steuerbetrag { get; set; }
        [BindProperty] public string Zahlungsbedingungen { get; set; }
        [BindProperty] public string Lieferart { get; set; }
        [BindProperty] public string ArtikelAnzahl { get; set; }
        [BindProperty] public string Dateipfad { get; set; }
        [BindProperty] public string Dateiname { get; set; }
        [BindProperty] public string OCRText { get; set; }
        [BindProperty] public DokumentStatus dtStatus { get; set; }
        [BindProperty] public string Email { get; set; }
        [BindProperty] public string Telefon { get; set; }
        [BindProperty] public string Telefax { get; set; }
        [BindProperty] public string IBAN { get; set; }
        [BindProperty] public string BIC { get; set; }
        [BindProperty] public string Bankverbindung { get; set; }
        [BindProperty] public string Zeitraum { get; set; }
        [BindProperty] public string SteuerNr { get; set; }
        [BindProperty] public string Gesamtpreis { get; set; }
        [BindProperty] public string Nettobetrag { get; set; }
        [BindProperty] public string AnsprechPartner { get; set; }
        [BindProperty] public string Adresse { get; set; }
        [BindProperty] public string AbsenderAdresse { get; set; } // ✅ Ajouté
        [BindProperty] public string UIDNummer { get; set; } // ✅ Ajouté
        [BindProperty] public string Website { get; set; }
        [BindProperty] public string PdfAutor { get; set; }
        [BindProperty] public string PdfBetreff { get; set; }
        [BindProperty] public string PdfSchluesselwoerter { get; set; } // ✅ Ajouté pour PDF
        [BindProperty] public string ObjectPath { get; set; }
        public List<Dokumente> DokumentListe { get; set; } = new();
        [BindProperty]
        public DokumentSucheFilter SucheFilter { get; set; } = new();
        public List<Dokumente> GefundeneDokumente { get; set; } = [];
        public List<DmsFolder> ExplorerTree { get; set; } = new(); // 👈 racine unique
        [BindProperty]
        public string NeuerDateiname { get; set; }
        public List<DokumentVersionen> VersionenListe { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? stepId, int? workflowId)
        {
            if (!stepId.HasValue || !workflowId.HasValue)
            {
                TempData["Error"] = "❌ Schritt oder Workflow nicht gefunden (Parameter fehlen).";
                return Page();
            }

            StepId = stepId.Value;
            WorkflowId = workflowId.Value;
            _logger.LogInformation("🔍 Workflow={WorkflowId}, Step={StepId}", WorkflowId, StepId);

            var step = await _db.Steps
                .Include(s => s.Workflow)
                .FirstOrDefaultAsync(s => s.Id == StepId && s.WorkflowId == WorkflowId);

            if (step == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(step.UserId);
            WorkflowTitle = step.Workflow?.Title ?? "-";
            StepTitle = step.Kategorie;
            StepAssignedUserName = user != null ? $"{user.Vorname} {user.Nachname}" : "unbekannt";
            StepDescription = step.Description;
            StepDue = step.DueDate;
            Kategorie = "Workflow";

            // 🔁 Rückkehr nach Analyse (TempData → wiederherstellen)
            if (Request.Query.ContainsKey("analyzed"))
            {
                _logger.LogInformation("🔁 Rückkehr nach Analyse mit Metadaten (via TempData)");
                foreach (var key in TempData.Keys.ToList())
                {
                    ViewData[key] = TempData[key]?.ToString();
                }
                TempData.Keep();
                ModelState.Clear();
                return Page();
            }

            // 3️⃣ Dokument + Metadaten laden (statt nur Dokument)
            if (Id.HasValue)
            {
                var dokument = await _db.Dokumente
                    .Include(d => d.MetadatenObjekt)
                    .FirstOrDefaultAsync(d => d.Id == Id.Value && d.ApplicationUserId == UserId);

                if (dokument != null)
                {
                    // ✅ Version speichern (wenn nötig)
                    await _versionierungsService.SpeichereVersionAsync(dokument.Id, UserId);

                    // ✅ Versionsliste laden
                    VersionenListe = await _versionierungsService.HoleVersionenZumOriginalAsync(dokument);

                    // 🟡 Status aktualisieren
                    if (dokument.dtStatus == DokumentStatus.Neu)
                    {
                        dokument.dtStatus = DokumentStatus.InBearbeitung;
                        await _db.SaveChangesAsync();
                        _logger.LogInformation("✏️ Status geändert für: {Id}", dokument.Id);
                    }

                    // 🔹 Metadaten laden oder leeres Objekt erzeugen
                    var meta = dokument.MetadatenObjekt ?? new Metadaten();

                    // 🧩 ViewModel mit Metadaten füllen
                    Kategorie = meta.Kategorie;
                    Beschreibung = meta.Beschreibung;
                    Titel = meta.Titel;
                    Rechnungsnummer = meta.Rechnungsnummer;
                    Kundennummer = meta.Kundennummer;
                    Rechnungsbetrag = meta.Rechnungsbetrag?.ToString("F2", CultureInfo.InvariantCulture);
                    Rechnungsdatum = meta.Rechnungsdatum?.ToString("yyyy-MM-dd");
                    Lieferdatum = meta.Lieferdatum?.ToString("yyyy-MM-dd");
                    Faelligkeitsdatum = meta.Faelligkeitsdatum?.ToString("yyyy-MM-dd");
                    Steuerbetrag = meta.Steuerbetrag?.ToString("F2", CultureInfo.InvariantCulture);
                    Zahlungsbedingungen = meta.Zahlungsbedingungen;
                    Lieferart = meta.Lieferart;
                    ArtikelAnzahl = meta.ArtikelAnzahl?.ToString();
                    Email = meta.Email;
                    Telefon = meta.Telefon;
                    Telefax = meta.Telefax;
                    IBAN = meta.IBAN;
                    BIC = meta.BIC;
                    Bankverbindung = meta.Bankverbindung;
                    SteuerNr = meta.SteuerNr;
                    UIDNummer = meta.UIDNummer;
                    Adresse = meta.Adresse;
                    AbsenderAdresse = meta.AbsenderAdresse;
                    AnsprechPartner = meta.AnsprechPartner;
                    Zeitraum = meta.Zeitraum;
                    PdfAutor = meta.PdfAutor;
                    PdfBetreff = meta.PdfBetreff;
                    PdfSchluesselwoerter = meta.PdfSchluesselwoerter;
                    Website = meta.Website;
                    OCRText = meta.OCRText;

                    // 🔹 Technische Infos (bleiben aus Dokument)
                    Dateipfad = dokument.Dateipfad;
                    Dateiname = dokument.Dateiname;
                    NeuerDateiname = Dateiname;
                    dtStatus = dokument.dtStatus;
                    ObjectPath = dokument.ObjectPath;

                    _logger.LogInformation("📄 Dokument {0} geladen mit Metadaten {1}", dokument.Id, dokument.MetadatenId);

                    return Page();
                }
                else
                {
                    ModelState.AddModelError("", "Dokument nicht gefunden.");
                }
            }

            // 🔹 Explorer + Liste
            ExplorerTree = new();
            DokumentListe = new();

            user = await _userManager.FindByIdAsync(UserId);
            var firma = user?.FirmenName?.Trim().ToLowerInvariant();
            var rootPath = $"dokumente/{firma}";

            DokumentListe = await _db.Dokumente
                .Include(d => d.MetadatenObjekt)
                .Where(d => d.ApplicationUserId == UserId && !string.IsNullOrEmpty(d.Dateipfad))
                .OrderByDescending(d => d.HochgeladenAm)
                .ToListAsync();

            foreach (var folder in ExplorerTree.Flatten())
            {
                var matchingFiles = DokumentListe
                    .Where(d => d.Dateipfad.StartsWith(folder.Path + "/", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                folder.Files = matchingFiles.Select(d => new DmsFile
                {
                    Name = d.Dateiname,
                    Path = d.Dateipfad,
                    Kategorie = d.MetadatenObjekt?.Kategorie ?? d.Kategorie,
                    HochgeladenAm = d.HochgeladenAm,
                    SasUrl = d.SasUrl,
                    EstSigne = d.EstSigne ?? false
                }).ToList();
            }

            _logger.LogInformation("📁 {Count} Dokumente geladen für UserId: {UserId}", DokumentListe.Count, UserId);

            // 🔹 Filterung nach Status
            if (Request.Query.TryGetValue("Status", out var statusQuery))
            {
                if (Enum.TryParse<DokumentStatus>(statusQuery, out var parsedStatus))
                {
                    DokumentListe = DokumentListe
                        .Where(d => d.dtStatus == parsedStatus)
                        .ToList();

                    _logger.LogInformation("🔎 Dokumente gefiltert nach Status: {0}", parsedStatus);
                }
            }

            return Page();
        }


        [RequestSizeLimit(524_288_000)]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> OnPostAnalyzeAsync(int? stepId = null, int? workflowId = null)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            var firma = user?.FirmenName?.Trim();
            _logger.LogInformation("DEBUG: OnPostAnalyzeAsync entered");

            if (!Request.HasFormContentType)
                return BadRequest("Request war kein multipart/form-data! Content-Type: " + Request.ContentType);

            if (Datei == null)
                return BadRequest("Datei ist null!");

            if (Datei.Length == 0)
                return BadRequest("Datei ist leer!");

            var ext = Path.GetExtension(Datei.FileName)?.ToLowerInvariant();
            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".bmp" };
            if (!allowed.Contains(ext))
                return BadRequest("❌ Unterstützte Formate: PDF, JPG, PNG, TIFF, BMP");

            if (Datei.Length > 50 * 1024 * 1024 && ext != ".pdf")
                return BadRequest("❌ Nur PDF-Dateien dürfen größer als 50MB sein.");

            // Datei im tmp-Ordner speichern
          
            var tempFilename = $"{Guid.NewGuid():N}{ext}";
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "workflow");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
            var filePath = Path.Combine(uploads, tempFilename);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await Datei.CopyToAsync(stream);

            Dateipfad = $"/uploads/workflow/{tempFilename}";
            Dateiname = Datei.FileName;

            // Azure OCR
            AnalyzeResult doc;
            try
            {
                using var stream = System.IO.File.OpenRead(filePath);
                doc = await _ocrService.AnalyzeInvoiceAsync(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Azure OCR fehlgeschlagen: " + ex);
                TempData["Error"] = "❌ Azure OCR konnte die Datei nicht analysieren. " + ex.Message;
                return Page();
            }

            var azureDoc = doc.Documents.FirstOrDefault();
            string Get(string key) => azureDoc != null && azureDoc.Fields.TryGetValue(key, out var field) ? field.Content?.Trim() : null;
            string CleanEuro(string val) => string.IsNullOrWhiteSpace(val) ? null : $"{val.Replace("EUR", "", StringComparison.OrdinalIgnoreCase).Replace("€", "").Trim()} EUR";
            var fullText = string.Join(" ", doc.Pages.SelectMany(p => p.Lines).Select(l => l.Content));

            // PDF-Metadaten
            string autor = null, betreff = null, schluesselwoerter = null;
            if (ext == ".pdf")
            {
                try
                {
                    using var pdfStream = System.IO.File.OpenRead(filePath);
                    (autor, betreff, schluesselwoerter) = PdfMetadataReader.ReadMetadata(pdfStream);
                }
                catch { }
            }

            // Metadaten
            var meta = new OcrMetadataResu
            {
                Rechnungsnummer = Get("InvoiceId"),
                Kundennummer = Get("CustomerId"),
                Rechnungsdatum = Get("InvoiceDate"),
                Lieferdatum = Get("DeliveryDate") ?? Get("DueDate"),
                Faelligkeitsdatum = Get("DueDate"),
                Zeitraum = Get("BillingPeriod"),
                Nettobetrag = CleanEuro(Get("SubTotal")),
                Steuerbetrag = CleanEuro(Get("TotalTax")),
                Rechnungsbetrag = CleanEuro(Get("TotalAmount")),
                Gesamtpreis = CleanEuro(Get("TotalAmount")),
                Zahlungsbedingungen = Get("PaymentTerms"),
                AnsprechPartner = Get("CustomerName"),
                Adresse = Get("CustomerAddress"),
                AbsenderAdresse = Get("VendorAddress"),
                UIDNummer = Get("VendorTaxId"),
                IBAN = OcrRegexExtractor.ExtractIban(fullText),
                Bankverbindung = Get("VendorName"),
                BIC = Get("SWIFTCode"),
                ArtikelAnzahl = "1",
                Website = OcrRegexExtractor.ExtractWebsite(fullText),
                Email = OcrRegexExtractor.ExtractEmail(fullText),
                Telefon = OcrRegexExtractor.ExtractPhone(fullText),
                Autor = autor,
                Betreff = betreff,
                Schluesselwoerter = schluesselwoerter
            };

            // Standard-Kategorie "Workflow" falls StepId gesetzt
            meta.Kategorie = (stepId.HasValue || workflowId.HasValue) ? "workflow" : (
                fullText.ToLowerInvariant() switch
                {
                    var t when t.Contains("gebühren") => "gebühren",
                    var t when t.Contains("rechnung") => "rechnungen",
                    var t when t.Contains("vertrag") => "verträge",
                    var t when t.Contains("projekt") => "projekt_a",
                    _ => "korrespondenz"
                });

            var abteilung = string.Empty;
            string objectPath = null;
            // (Optional: Cloud-Upload hier)
            Dateipfad = await _firebaseStorageService.UploadForUserAsync(Datei, firma, abteilung, meta.Kategorie); // ✅ so muss es bleiben

            // Alle relevanten Daten in TempData
            TempData["Dateipfad"] = Dateipfad;
            TempData["ObjectPath"] = objectPath ?? "";
            TempData["Dateiname"] = Datei.FileName;
            TempData["Kategorie"] = meta.Kategorie;
            TempData["Rechnungsnummer"] = meta.Rechnungsnummer;
            TempData["Gesamtpreis"] = meta.Gesamtpreis;
            TempData["Rechnungsbetrag"] = meta.Rechnungsbetrag;
            TempData["Nettobetrag"] = meta.Nettobetrag;
            TempData["Steuerbetrag"] = meta.Steuerbetrag;
            TempData["Faelligkeitsdatum"] = meta.Faelligkeitsdatum;
            TempData["Rechnungsdatum"] = meta.Rechnungsdatum;
            TempData["Lieferdatum"] = meta.Lieferdatum;
            TempData["ArtikelAnzahl"] = meta.ArtikelAnzahl;
            TempData["Beschreibung"] = meta.Beschreibung;
            TempData["Zahlungsbedingungen"] = meta.Zahlungsbedingungen;
            TempData["AnsprechPartner"] = meta.AnsprechPartner;
            TempData["Adresse"] = meta.Adresse;
            TempData["AbsenderAdresse"] = meta.AbsenderAdresse;
            TempData["IBAN"] = meta.IBAN;
            TempData["BIC"] = meta.BIC;
            TempData["Bankverbindung"] = meta.Bankverbindung;
            TempData["UIDNummer"] = meta.UIDNummer;
            TempData["Website"] = meta.Website;
            TempData["Email"] = meta.Email;
            TempData["Telefon"] = meta.Telefon;
            TempData["PdfAutor"] = meta.Autor;
            TempData["PdfBetreff"] = meta.Betreff;
            TempData["PdfSchluesselwoerter"] = meta.Schluesselwoerter;
            TempData["OCRText"] = fullText;
            TempData["StepId"] = stepId;
            TempData["WorkflowId"] = workflowId;
            TempData["Success"] = "✅ Analyse abgeschlossen. Bitte jetzt speichern.";
            _logger.LogWarning("DEBUG Analyze: Post erhalten – Dateipfad='{Dateipfad}', Datei null? {HatDatei}", Dateipfad, Datei != null);

            return RedirectToPage("StepUpload", new { stepId = stepId, workflowId = workflowId, analyzed = true });

        }


        private class OcrRegexExtractor
        {
            public static string ExtractEmail(string text)
            {
                var match = Regex.Match(text ?? "", @"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+");
                return match.Success ? match.Value : null;
            }

            public static string ExtractWebsite(string text)
            {
                var match = Regex.Match(text ?? "", @"https?:\/\/[^\s]+|www\.[^\s]+");
                return match.Success ? match.Value : null;
            }

            public static string ExtractPhone(string text)
            {
                var match = Regex.Match(text ?? "", @"(?:(?:\+|00)\d{1,3}[\s.-]?)?(?:\(?\d{2,5}\)?[\s.-]?)?\d{3,5}[\s.-]?\d{3,5}");
                return match.Success ? match.Value : null;
            }

            public static string ExtractUID(string text)
            {
                var match = Regex.Match(text ?? "", @"\bDE\d{9}\b");
                return match.Success ? match.Value : null;
            }

            public static string ExtractIban(string text) =>
                Regex.Match(text ?? "", @"\b[A-Z]{2}[0-9]{2}(?:[ ]?[0-9A-Z]{4,}){2,}\b").Success
                ? Regex.Match(text ?? "", @"\b[A-Z]{2}[0-9]{2}(?:[ ]?[0-9A-Z]{4,}){2,}\b").Value : null;
        }
        public static string GetStatusBadgeClass(DokumentStatus status) => status switch
        {
            DokumentStatus.Neu => "bg-secondary",
            DokumentStatus.InBearbeitung => "bg-warning text-dark",
            DokumentStatus.Fertig => "bg-success",
            DokumentStatus.Fehlerhaft => "bg-danger",
            _ => "bg-dark"
        };


        public async Task<IActionResult> OnPostSaveAsync()
        {
            _logger.LogInformation("▶️ OnPostSaveAsync gestartet – Step={StepId}, Workflow={WorkflowId}", StepId, WorkflowId);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 🔹 Workflow + Step laden
            var step = await _db.Steps.Include(s => s.Workflow)
                .FirstOrDefaultAsync(s => s.Id == StepId && s.WorkflowId == WorkflowId);

            if (step == null)
            {
                TempData["Error"] = "❌ Schritt im Workflow nicht gefunden.";
                return Page();
            }

            // 🔹 Kunde prüfen
            var kundenBenutzer = await _db.KundeBenutzer
                .Include(kb => kb.Kunden)
                .FirstOrDefaultAsync(kb => kb.ApplicationUserId == UserId);

            if (kundenBenutzer?.Kunden == null)
            {
                TempData["Error"] = "❌ Kein zugeordneter Kunde gefunden.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Dateipfad))
            {
                ModelState.AddModelError("", "Dateipfad fehlt! Bitte erst analysieren.");
                return Page();
            }

            // 🔹 Dokument laden oder neu erstellen
            var dokument = await _db.Dokumente
                .Include(d => d.MetadatenObjekt)
                .FirstOrDefaultAsync(d => d.Dateipfad == Dateipfad && d.ApplicationUserId == UserId);

            bool istNeu = false;

            if (dokument == null)
            {
                dokument = new Dokumente
                {
                    Id = Guid.NewGuid(),
                    HochgeladenAm = DateTime.UtcNow,
                    ApplicationUserId = UserId,
                    KundeId = kundenBenutzer.Kunden.Id,
                    Dateipfad = Dateipfad,
                    ObjectPath = Dateipfad.Replace($"https://storage.googleapis.com/{_firebaseStorageService.Bucket}/", ""),
                    dtStatus = DokumentStatus.Neu,
                    WorkflowId = WorkflowId,
                    StepId = StepId
                };
                _db.Dokumente.Add(dokument);
                istNeu = true;
                _logger.LogInformation("🆕 Neues Dokument erstellt: {DokumentId}", dokument.Id);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dokument.ObjectPath))
                {
                    dokument.ObjectPath = dokument.Dateipfad.Replace($"https://storage.googleapis.com/{_firebaseStorageService.Bucket}/", "");
                }

                if (dokument.dtStatus != DokumentStatus.Fertig)
                {
                    dokument.dtStatus = DokumentStatus.InBearbeitung;
                }
            }

            // ✅ 1️⃣ Metadatenobjekt prüfen oder anlegen
            var meta = dokument.MetadatenObjekt;
            if (meta == null)
            {
                meta = new Metadaten();
                _db.Metadaten.Add(meta);
                await _db.SaveChangesAsync();

                dokument.MetadatenId = meta.Id;
                dokument.MetadatenObjekt = meta;
                _db.Update(dokument);
                await _db.SaveChangesAsync();

                _logger.LogInformation("📎 Neues Metadatenobjekt angelegt: {MetaId}", meta.Id);
            }

            // ✅ 2️⃣ Alle Felder in Metadaten speichern
            meta.Titel = Titel ?? Path.GetFileNameWithoutExtension(Dateiname);
            meta.Kategorie = Kategorie ?? "workflow";
            meta.Beschreibung = Beschreibung;
            meta.Rechnungsnummer = Rechnungsnummer;
            meta.Kundennummer = Kundennummer;
            meta.Rechnungsbetrag = TryParseDecimal(Rechnungsbetrag);
            meta.Nettobetrag = TryParseDecimal(Nettobetrag);
            meta.Gesamtpreis = TryParseDecimal(Gesamtpreis);
            meta.Steuerbetrag = TryParseDecimal(Steuerbetrag);
            meta.Rechnungsdatum = TryParseDate(Rechnungsdatum);
            meta.Lieferdatum = TryParseDate(Lieferdatum);
            meta.Faelligkeitsdatum = TryParseDate(Faelligkeitsdatum);
            meta.Zahlungsbedingungen = Zahlungsbedingungen;
            meta.Lieferart = Lieferart;
            meta.ArtikelAnzahl = int.TryParse(ArtikelAnzahl, out var anz) ? anz : null;
            meta.Email = Email;
            meta.Telefon = Telefon;
            meta.Telefax = Telefax;
            meta.IBAN = IBAN;
            meta.BIC = BIC;
            meta.Bankverbindung = Bankverbindung;
            meta.SteuerNr = SteuerNr;
            meta.UIDNummer = UIDNummer;
            meta.Adresse = Adresse;
            meta.AbsenderAdresse = AbsenderAdresse;
            meta.AnsprechPartner = AnsprechPartner;
            meta.Zeitraum = Zeitraum;
            meta.PdfAutor = PdfAutor;
            meta.PdfBetreff = PdfBetreff;
            meta.PdfSchluesselwoerter = PdfSchluesselwoerter;
            meta.Website = Website;
            meta.OCRText = OCRText;

            _db.Metadaten.Update(meta);
            await _db.SaveChangesAsync();

            dokument.Dateiname = !string.IsNullOrWhiteSpace(NeuerDateiname)
                ? NeuerDateiname
                : dokument.Dateiname;

            _logger.LogInformation("💾 Metadaten aktualisiert für Dokument {DokumentId}", dokument.Id);

            // ✅ Benutzerdefinierte Metadaten speichern
            var keys = Request.Form["CustomKeys"];
            var values = Request.Form["CustomValues"];

            if (keys.Count == values.Count && keys.Count > 0)
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    var key = keys[i];
                    var value = values[i];

                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        _db.BenutzerMetadaten.Add(new BenutzerMetadaten
                        {
                            Id = Guid.NewGuid(),
                            DokumentId = dokument.Id,
                            Key = key,
                            Value = value,
                            ErzeugtAm = DateTime.UtcNow
                        });
                    }
                }
                _logger.LogInformation("🧩 Benutzerdefinierte Metadaten gespeichert für Dokument {DokumentId}", dokument.Id);
            }

            await _db.SaveChangesAsync();

            // ✅ 3️⃣ Workflow- und Aufgabenlogik bleibt unverändert

            var aufgabe = await _db.Aufgaben
                .Include(a => a.StepNavigation)
                .ThenInclude(s => s.Workflow)
                .FirstOrDefaultAsync(a => a.StepId == StepId);

            if (aufgabe != null)
            {
                aufgabe.Erledigt = true;
                await _db.SaveChangesAsync();

                await _auditLogService.LogActionOnlyAsync(
                    $"Aufgabe \"{aufgabe.Titel}\" ({aufgabe.Id}) erledigt", userId);

                if (aufgabe.StepNavigation != null)
                {
                    var currentStep = aufgabe.StepNavigation;
                    currentStep.Completed = true;

                    var nextStep = await _db.Steps
                        .Where(s => s.WorkflowId == currentStep.WorkflowId && s.Order == currentStep.Order + 1)
                        .FirstOrDefaultAsync();

                    if (nextStep != null && !nextStep.TaskCreated && !string.IsNullOrWhiteSpace(nextStep.UserId))
                    {
                        var neueAufgabe = new Aufgaben
                        {
                            Titel = nextStep.Title,
                            Beschreibung = nextStep.Description,
                            FaelligBis = nextStep.DueDate ?? DateTime.Today.AddDays(3),
                            Prioritaet = 1,
                            VonUser = aufgabe.VonUser,
                            FuerUser = nextStep.UserId,
                            Erledigt = false,
                            ErstelltAm = DateTime.Now,
                            StepId = nextStep.Id,
                            Aktiv = true
                        };

                        _db.Aufgaben.Add(neueAufgabe);
                        nextStep.TaskCreated = true;
                        _logger.LogInformation("🧩 Neue Aufgabe für nächsten Step erstellt: {NextStepId}", nextStep.Id);
                    }

                    await _db.SaveChangesAsync();

                    // ✅ Notifications wie gehabt
                    var notificationType = await _db.NotificationTypes.FirstOrDefaultAsync(n => n.Name == "Workflowaufgabe");
                    if (notificationType != null && nextStep != null)
                    {
                        var notification = new Notification
                        {
                            Title = "Neue Aufgabe zugewiesen",
                            Content = $"Du hast eine neue Aufgabe im Workflow \"{step.Workflow.Title}\" erhalten.",
                            CreatedAt = DateTime.UtcNow,
                            NotificationTypeId = notificationType.Id
                        };
                        _db.Notifications.Add(notification);
                        await _db.SaveChangesAsync();

                        var userNotification = new UserNotification
                        {
                            UserId = nextStep.UserId,
                            NotificationId = notification.Id,
                            IsRead = false,
                            ReceivedAt = DateTime.UtcNow
                        };
                        _db.UserNotifications.Add(userNotification);
                        await _db.SaveChangesAsync();
                    }
                }
            }

            await _auditLogService?.LogActionOnlyAsync(
                $"StepUpload: Datei '{dokument.Dateiname}' für Step {step.Id} in Workflow '{step.Workflow.Title}' hochgeladen",
                userId);

            _logger.LogInformation("✅ Schritt abgeschlossen und Metadaten gespeichert für Dokument {DokumentId}", dokument.Id);

            TempData["Success"] = "✅ Schritt abgeschlossen und Metadaten gespeichert!";
            return RedirectToPage("/Workflows/StepDetail", new { stepId = StepId, workflowId = WorkflowId });
        }



        private DateTime? TryParseDate(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            return DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                ? dt
                : (DateTime?)null;
        }
        private decimal? TryParseDecimal(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            input = input.Replace("EUR", "")
                         .Replace("€", "")
                         .Replace(".", "")
                         .Replace(",", ".")
                         .Trim();

            return decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }


    }
}
