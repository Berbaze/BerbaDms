using Azure.AI.FormRecognizer.DocumentAnalysis;
using DmsProjeckt.Data;
using DmsProjeckt.Helpers;
using DmsProjeckt.Service;
using DmsProjeckt.Services;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PdfiumViewer;
using SkiaSharp;
using System.Drawing;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;
using static DmsProjeckt.Pages.Tests.UploadMultiModel;

namespace DmsProjeckt.Pages.Tests
{
    public class UploadMultiModel : PageModel
    {
        private readonly AzureOcrService _ocrService;
        private readonly FirebaseStorageService _firebase;
        private readonly ApplicationDbContext _db;
        private readonly AuditLogDokumentService _audit;
        private readonly DocumentHashService _hashService;
        private readonly ChunkService _chunkService;

        public UploadMultiModel(
            AzureOcrService ocrService,
            FirebaseStorageService firebase,
            ApplicationDbContext db,
            AuditLogDokumentService audit,
            DocumentHashService hashService,
            ChunkService chunkService)
        {
            _ocrService = ocrService;
            _firebase = firebase;
            _db = db;
            _audit = audit;
            _hashService = hashService;
            _chunkService = chunkService;
        }

        [BindProperty(SupportsGet = false)]
        public List<IFormFile> Dateien { get; set; }
        [BindProperty]
        public List<DokumentViewModel> Dokumente { get; set; } = new();
        public List<Abteilung> Abteilungen { get; set; } = new();
        [BindProperty]
        public int? AbteilungId { get; set; }
        [BindProperty] public string Titel { get; set; }
        [BindProperty] public string Kategorie { get; set; }
        [BindProperty] public string Beschreibung { get; set; }
        [BindProperty] public string Rechnungsnummer { get; set; }
        [BindProperty] public string Kundennummer { get; set; }
        [BindProperty] public string Rechnungsbetrag { get; set; }
        [BindProperty] public string Nettobetrag { get; set; }
        [BindProperty] public string Gesamtpreis { get; set; }
        [BindProperty] public string Steuerbetrag { get; set; }
        [BindProperty] public string Rechnungsdatum { get; set; }
        [BindProperty] public string Lieferdatum { get; set; }
        [BindProperty] public string Faelligkeitsdatum { get; set; }
        [BindProperty] public string Zahlungsbedingungen { get; set; }
        [BindProperty] public string Lieferart { get; set; }
        [BindProperty] public string ArtikelAnzahl { get; set; }
        [BindProperty] public string Email { get; set; }
        [BindProperty] public string Telefon { get; set; }
        [BindProperty] public string Telefax { get; set; }
        [BindProperty] public string IBAN { get; set; }
        [BindProperty] public string BIC { get; set; }
        [BindProperty] public string Bankverbindung { get; set; }
        [BindProperty] public string SteuerNr { get; set; }
        [BindProperty] public string UIDNummer { get; set; }
        [BindProperty] public string Adresse { get; set; }
        [BindProperty] public string AbsenderAdresse { get; set; }
        [BindProperty] public string AnsprechPartner { get; set; }
        [BindProperty] public string Zeitraum { get; set; }
        [BindProperty] public string PdfAutor { get; set; }
        [BindProperty] public string PdfBetreff { get; set; }
        [BindProperty] public string PdfSchluesselwoerter { get; set; }
        [BindProperty] public string Website { get; set; }
        [BindProperty] public string OCRText { get; set; }
        [BindProperty] public IFormFile Datei { get; set; }
        public List<string> Kategorien { get; set; } = new() { "rechnungen", "verträge", "gebühren", "projekt_A", "korrespondenz", "unbekannt" };

        public int? UserAbteilungId { get; set; }
        public string UserAbteilungName { get; set; }


        public class DokumentViewModel
        {

            public IFormFile Content { get; set; }
            public string FileName { get; set; }
            public MetadataModel Metadata { get; set; } = new();
            public DokumentStatus Status { get; set; } = DokumentStatus.Pending;
            public string ObjectPath { get; set; }
            public int? Progress { get; set; }
            public string? Titel { get; set; }
            public DateTime? HochgeladenAm { get; set; } = DateTime.UtcNow;
            public bool IsGrossDoc { get; set; }
            public string AbteilungName { get; set; }
            public int? AbteilungId { get; set; }
            public string Kategorie
            {
                get => Metadata.Kategorie;
                set => Metadata.Kategorie = value;
            }
        }
        [TempData]
        public string DokumenteJson { get; set; }

        public class MetadataModel
        {
            public int? KundeId { get; set; }
            public string Kategorie { get; set; }
            public string Beschreibung { get; set; }
            public string Titel { get; set; }
            public string Rechnungsnummer { get; set; }
            public string Kundennummer { get; set; }
            public string Rechnungsbetrag { get; set; }
            public string Nettobetrag { get; set; }
            public string Gesamtpreis { get; set; }
            public string Steuerbetrag { get; set; }
            public string Rechnungsdatum { get; set; }
            public string Lieferdatum { get; set; }
            public string Faelligkeitsdatum { get; set; }
            public string Zahlungsbedingungen { get; set; }
            public string Lieferart { get; set; }
            public string ArtikelAnzahl { get; set; }

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
            public int? AbteilungId { get; set; }

            public bool IsGrossDoc { get; set; }

            public string AbteilungName { get; set; }
            public string ObjectPath { get; set; }
            public string FileHash { get; set; }
        }



        public async Task OnGetAsync(bool analyzed = false, Guid? id = null, string ids = null)
        {
            Dokumente = new List<DokumentViewModel>();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                Console.WriteLine("❌ Kein Benutzer gefunden!");
                return;
            }

            // 🔐 Abteilungen laden
            if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
            {
                Abteilungen = await _db.Abteilungen.OrderBy(a => a.Name).ToListAsync();
                Console.WriteLine($"[DEBUG] Admin angemeldet → {Abteilungen.Count} Abteilungen geladen");
            }
            else
            {
                if (user.AbteilungId.HasValue)
                {
                    var abt = await _db.Abteilungen.FindAsync(user.AbteilungId.Value);
                    if (abt != null)
                    {
                        UserAbteilungId = abt.Id;
                        UserAbteilungName = abt.Name;
                        Abteilungen = new List<Abteilung> { abt };
                        Console.WriteLine($"[DEBUG] User angemeldet → Abteilung {abt.Name}");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Benutzer hat keine Abteilung.");
                    Abteilungen = new List<Abteilung>();
                }
            }

            // =======================
            // 1️⃣ Analyse: TempData laden
            // =======================
            if (analyzed && TempData.ContainsKey(nameof(DokumenteJson)))
            {
                DokumenteJson = TempData[nameof(DokumenteJson)]?.ToString();
                if (!string.IsNullOrEmpty(DokumenteJson))
                {
                    var serialisiert = JsonSerializer.Deserialize<List<DokumentSerialisiert>>(DokumenteJson);
                    if (serialisiert != null)
                    {
                        Dokumente = serialisiert.Select(d => new DokumentViewModel
                        {
                            FileName = d.FileName,
                            Metadata = d.Metadata ?? new MetadataModel(),
                            Status = d.Status
                        }).ToList();
                    }
                }
                TempData.Keep(nameof(DokumenteJson));
                Console.WriteLine($"✔️ Geladene Dokumente: {Dokumente.Count}");
                return;
            }

            // =======================
            // 2️⃣ Einzelnes Dokument (?id=)
            // =======================
            if (id.HasValue)
            {
                var doc = await _db.Dokumente
                    .Include(d => d.MetadatenObjekt)
                    .FirstOrDefaultAsync(d => d.Id == id.Value);

                if (doc != null)
                {
                    var meta = doc.MetadatenObjekt ?? new Metadaten();

                    Dokumente.Add(new DokumentViewModel
                    {
                        FileName = doc.Dateiname,
                        Metadata = new MetadataModel
                        {
                            Kategorie = meta.Kategorie ?? doc.Kategorie,
                            Beschreibung = meta.Beschreibung,
                            Titel = meta.Titel,
                            Rechnungsnummer = meta.Rechnungsnummer,
                            Kundennummer = meta.Kundennummer,
                            Rechnungsbetrag = meta.Rechnungsbetrag?.ToString("F2"),
                            Nettobetrag = meta.Nettobetrag?.ToString("F2"),
                            Gesamtpreis = meta.Gesamtpreis?.ToString("F2"),
                            Steuerbetrag = meta.Steuerbetrag?.ToString("F2"),
                            Rechnungsdatum = meta.Rechnungsdatum?.ToString("yyyy-MM-dd"),
                            Lieferdatum = meta.Lieferdatum?.ToString("yyyy-MM-dd"),
                            Faelligkeitsdatum = meta.Faelligkeitsdatum?.ToString("yyyy-MM-dd"),
                            Zahlungsbedingungen = meta.Zahlungsbedingungen,
                            Lieferart = meta.Lieferart,
                            ArtikelAnzahl = meta.ArtikelAnzahl?.ToString(),
                            Email = meta.Email,
                            Telefon = meta.Telefon,
                            Telefax = meta.Telefax,
                            IBAN = meta.IBAN,
                            BIC = meta.BIC,
                            Bankverbindung = meta.Bankverbindung,
                            SteuerNr = meta.SteuerNr,
                            UIDNummer = meta.UIDNummer,
                            Adresse = meta.Adresse,
                            AbsenderAdresse = meta.AbsenderAdresse,
                            AnsprechPartner = meta.AnsprechPartner,
                            Zeitraum = meta.Zeitraum,
                            PdfAutor = meta.PdfAutor,
                            PdfBetreff = meta.PdfBetreff,
                            PdfSchluesselwoerter = meta.PdfSchluesselwoerter,
                            Website = meta.Website,
                            OCRText = meta.OCRText,
                            ObjectPath = doc.ObjectPath
                        },
                        Status = DokumentStatus.Analyzed
                    });

                    Console.WriteLine($"✔️ Dokument geladen: {doc.Dateiname} ({doc.Id})");
                }

                return;
            }

            // =======================
            // 3️⃣ Mehrere Dokumente (?ids=...)
            // =======================
            if (!string.IsNullOrEmpty(ids))
            {
                var guids = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => Guid.TryParse(g, out var guid) ? guid : (Guid?)null)
                    .Where(g => g.HasValue)
                    .Select(g => g.Value)
                    .ToList();

                if (guids.Any())
                {
                    var docs = await _db.Dokumente
                        .Include(d => d.MetadatenObjekt)
                        .Where(d => guids.Contains(d.Id))
                        .ToListAsync();

                    foreach (var doc in docs)
                    {
                        var meta = doc.MetadatenObjekt ?? new Metadaten();

                        Dokumente.Add(new DokumentViewModel
                        {
                            FileName = doc.Dateiname,
                            Metadata = new MetadataModel
                            {
                                Kategorie = meta.Kategorie ?? doc.Kategorie,
                                Beschreibung = meta.Beschreibung,
                                Titel = meta.Titel,
                                Rechnungsnummer = meta.Rechnungsnummer,
                                Kundennummer = meta.Kundennummer,
                                Rechnungsbetrag = meta.Rechnungsbetrag?.ToString("F2"),
                                Nettobetrag = meta.Nettobetrag?.ToString("F2"),
                                Gesamtpreis = meta.Gesamtpreis?.ToString("F2"),
                                Steuerbetrag = meta.Steuerbetrag?.ToString("F2"),
                                Rechnungsdatum = meta.Rechnungsdatum?.ToString("yyyy-MM-dd"),
                                Lieferdatum = meta.Lieferdatum?.ToString("yyyy-MM-dd"),
                                Faelligkeitsdatum = meta.Faelligkeitsdatum?.ToString("yyyy-MM-dd"),
                                Zahlungsbedingungen = meta.Zahlungsbedingungen,
                                Lieferart = meta.Lieferart,
                                ArtikelAnzahl = meta.ArtikelAnzahl?.ToString(),
                                Email = meta.Email,
                                Telefon = meta.Telefon,
                                Telefax = meta.Telefax,
                                IBAN = meta.IBAN,
                                BIC = meta.BIC,
                                Bankverbindung = meta.Bankverbindung,
                                SteuerNr = meta.SteuerNr,
                                UIDNummer = meta.UIDNummer,
                                Adresse = meta.Adresse,
                                AbsenderAdresse = meta.AbsenderAdresse,
                                AnsprechPartner = meta.AnsprechPartner,
                                Zeitraum = meta.Zeitraum,
                                PdfAutor = meta.PdfAutor,
                                PdfBetreff = meta.PdfBetreff,
                                PdfSchluesselwoerter = meta.PdfSchluesselwoerter,
                                Website = meta.Website,
                                OCRText = meta.OCRText,
                                ObjectPath = doc.ObjectPath
                            },
                            Status = DokumentStatus.Analyzed
                        });
                    }

                    Console.WriteLine($"✔️ {Dokumente.Count} Dokumente geladen");
                }
            }
        }


        private (string Title, string Author, string Subject, string Keywords) ExtractPdfMetadata(string filePath)
        {
            using var pdf = PdfSharp.Pdf.IO.PdfReader.Open(filePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.ReadOnly);

            string title = pdf.Info.Title ?? "";
            string author = pdf.Info.Author ?? "";
            string subject = pdf.Info.Subject ?? "";
            string keywords = pdf.Info.Keywords ?? "";

            return (title, author, subject, keywords);
        }
        /// <summary>
        /// Corrige les erreurs OCR fréquentes (ex: Saufmann → Kaufmann).
        /// Peut être enrichi avec un dictionnaire.
        /// </summary>

        // 🔧 Helpers de conversion
        // 🔧 Helpers de conversion
        private static string ToStringValue(decimal? value)
        {
            return value?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static decimal? ToDecimalValue(string value)
        {
            if (decimal.TryParse(value?.Replace("€", "").Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
                return result;
            return null;
        }
        [RequestSizeLimit(4L * 1024 * 1024 * 1024)]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> OnPostAnalyzeAsync()
        {
            Dokumente = new List<DokumentViewModel>();

            if (Dateien == null || Dateien.Count == 0)
            {
                ModelState.AddModelError("Dateien", "Bitte wählen Sie mindestens eine Datei aus.");
                return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var firma = user?.FirmenName?.Trim()?.ToLowerInvariant(); // 🔹 Normalisation

            // 🔑 Kunde auflösen
            var kunde = await ResolveKundeForUserAsync(user);
            if (string.IsNullOrWhiteSpace(firma) || kunde == null)
            {
                TempData["Error"] = "❌ Benutzer/Kunde ungültig.";
                return RedirectToPage(new { analyzed = true });
            }

            Console.WriteLine($"👤 Benutzer = {user?.UserName}, Firma = {firma}, KundeId = {kunde.Id}");
            Console.WriteLine($"📂 Anzahl Dateien zum Analysieren: {Dateien.Count}");

            foreach (var file in Dateien)
            {
                Console.WriteLine($"--- 📝 Datei: {file.FileName}, Größe={file.Length / 1024 / 1024:F2} MB ---");

                var vm = new DokumentViewModel
                {
                    Content = file,
                    FileName = file.FileName,
                    Titel = Path.GetFileNameWithoutExtension(file.FileName),
                    Progress = 10,
                    Metadata = new MetadataModel()
                };

                try
                {
                    if (file.Length == 0)
                    {
                        vm.Metadata.Beschreibung = "⚠️ Datei ist leer oder konnte nicht gelesen werden.";
                        Dokumente.Add(vm);
                        continue;
                    }

                    // =======================================================
                    // 🔹 1️⃣ Hash berechnen & Prüfen auf Duplikat
                    // =======================================================
                    string hash;
                    using (var hashStream = file.OpenReadStream())
                        hash = _hashService.ComputeHash(hashStream);

                    vm.Metadata.FileHash = hash;

                    var existing = await _db.Dokumente
                        .Include(d => d.MetadatenObjekt)
                        .Include(d => d.Abteilung)
                        .Include(d => d.ApplicationUser)
                        .FirstOrDefaultAsync(d => d.FileHash == hash);

                    if (existing != null)
                    {
                        Console.WriteLine($"♻️ Datei bereits vorhanden → {existing.Dateiname}");
                        string currentUserId = user.Id;
                        Guid existingDokumentId = existing.Id;

                        bool alreadyLogged = await _db.DuplicateUploads
                            .AnyAsync(x => x.DokumentId == existingDokumentId && x.UserId == currentUserId);

                        if (!alreadyLogged)
                        {
                            _db.DuplicateUploads.Add(new DuplicateUpload
                            {
                                DokumentId = existingDokumentId,
                                UserId = currentUserId,
                                FileName = file.FileName,
                                UploadedAt = DateTime.UtcNow
                            });
                            await _db.SaveChangesAsync();
                        }

                        vm.ObjectPath = existing.ObjectPath;
                        vm.Metadata = new MetadataModel
                        {
                            Titel = existing.MetadatenObjekt?.Titel ?? existing.Titel,
                            Beschreibung = "Duplikat erkannt – Datei wurde bereits hochgeladen.",
                            Kategorie = existing.MetadatenObjekt?.Kategorie ?? existing.Kategorie ?? "allgemein"
                        };

                        vm.Status = DokumentStatus.Analyzed;
                        vm.Progress = 100;
                        Dokumente.Add(vm);
                        continue;
                    }

                    // =======================================================
                    // 🔹 2️⃣ OCR & Metadaten-Analyse (Smart je nach Größe)
                    // =======================================================
                    var ext = Path.GetExtension(file.FileName).ToLower();

                    if (ext == ".pdf" || ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".tif" || ext == ".tiff")
                    {
                        if (file.Length > 20 * 1024 * 1024)
                        {
                            // ⚠️ Großes PDF → kein OCR, nur PDF-Metadaten
                            Console.WriteLine($"⚠️ {file.FileName} > 20MB → OCR übersprungen, nur PDF-Metadaten gelesen.");

                            try
                            {
                                using var pdfStream = file.OpenReadStream();
                                using var reader = new iText.Kernel.Pdf.PdfReader(pdfStream);
                                using var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader);
                                var info = pdfDoc.GetDocumentInfo();

                                vm.Metadata.Titel = info.GetTitle() ?? Path.GetFileNameWithoutExtension(file.FileName);
                                vm.Metadata.PdfAutor = info.GetAuthor();
                                vm.Metadata.PdfBetreff = info.GetSubject();
                                vm.Metadata.PdfSchluesselwoerter = info.GetKeywords();
                                vm.Metadata.Beschreibung = "Großes PDF (>20MB) – nur Metadaten (Autor, Betreff, Stichwörter) extrahiert.";
                                vm.Metadata.Kategorie = "allgemein";
                            }
                            catch (Exception metaEx)
                            {
                                vm.Metadata.Beschreibung = $"Fehler beim Lesen der PDF-Metadaten: {metaEx.Message}";
                            }
                        }
                        else
                        {
                            // 🔹 OCR-Analyse (nur kleine Dateien)
                            using var ocrStream = file.OpenReadStream();
                            var res = await _ocrService.AnalyzeInvoiceAsync(ocrStream);
                            var doc = res.Documents.FirstOrDefault();

                            var fullText = string.Join(" ", res.Pages.SelectMany(p => p.Lines).Select(l => l.Content));
                            var (erkannteKategorie, autoAbteilungName) = DetectKategorieUndAbteilung(file.FileName, fullText);
                            vm.Metadata.Kategorie = erkannteKategorie?.Trim()?.ToLowerInvariant() ?? "allgemein";
                            vm.Metadata.Titel = Path.GetFileNameWithoutExtension(file.FileName);
                            vm.Metadata.OCRText = fullText;

                            if (!string.IsNullOrWhiteSpace(autoAbteilungName))
                            {
                                var abt = await _db.Abteilungen.FirstOrDefaultAsync(a =>
                                    a.Name.ToLower().Trim() == autoAbteilungName.ToLower().Trim());
                                if (abt != null)
                                {
                                    vm.Metadata.AbteilungId = abt.Id;
                                    vm.Metadata.AbteilungName = abt.Name.ToLowerInvariant();
                                }
                            }

                            if (doc != null)
                            {
                                var fields = doc.Fields;
                                string Extract(string key) => fields.GetValueOrDefault(key)?.Content?.Trim();
                                var helperMetadata = OcrMetadataExtractorService.Extract(fullText);

                                vm.Metadata.Rechnungsnummer = Extract("InvoiceId");
                                vm.Metadata.Rechnungsdatum = Extract("InvoiceDate");
                                vm.Metadata.Faelligkeitsdatum = Extract("DueDate");
                                vm.Metadata.Nettobetrag = SanitizeEuroValue(Extract("SubTotal"));
                                vm.Metadata.Steuerbetrag = SanitizeEuroValue(Extract("TotalTax"));
                                vm.Metadata.Gesamtpreis = SanitizeEuroValue(Extract("TotalAmount"));
                                vm.Metadata.Rechnungsbetrag = vm.Metadata.Gesamtpreis;
                                vm.Metadata.IBAN = Extract("IBAN") ?? helperMetadata?.IBAN;
                                vm.Metadata.UIDNummer = Extract("VendorTaxId") ?? Extract("VATNumber");
                                vm.Metadata.Beschreibung = "Metadaten automatisch extrahiert (OCR)";
                            }
                        }
                    }

                    // =======================================================
                    // 🔹 3️⃣ Upload oder Chunk-Speicherung
                    // =======================================================
                    Guid dokumentId = Guid.NewGuid();

                    if (file.Length > 20 * 1024 * 1024) // >20MB → Chunk-Modus
                    {
                        var abtName = vm.Metadata.AbteilungName ?? "allgemein";
                        var katName = vm.Metadata.Kategorie ?? "allgemein";

                        var chunkedDoc = new Dokumente
                        {
                            Id = dokumentId,
                            ApplicationUserId = userId,
                            KundeId = kunde.Id,
                            Dateiname = file.FileName,
                            Kategorie = katName,
                            FileHash = vm.Metadata.FileHash,
                            FileSizeBytes = file.Length,
                            HochgeladenAm = DateTime.UtcNow,
                            IsChunked = true,
                            ObjectPath = $"dokumente/{firma}/{abtName}/{katName}/chunks/{dokumentId}/",
                            Beschreibung = "Chunked Upload initialisiert"
                        };

                        _db.Dokumente.Add(chunkedDoc);
                        await _db.SaveChangesAsync();

                        using var fileStream = file.OpenReadStream();
                        var chunks = await _chunkService.SaveFileAsChunksToFirebaseAsync(
                            fileStream,
                            dokumentId,
                            firma,
                            abtName,
                            katName
                        );

                        vm.ObjectPath = $"chunked://{dokumentId}";
                        vm.Metadata.Beschreibung = $"Datei in {chunks.Count} Chunks aufgeteilt und in Firebase gespeichert.";
                        vm.Metadata.FileHash = hash;

                        chunkedDoc.Beschreibung = $"Chunked Upload abgeschlossen ({chunks.Count} Teile)";
                        _db.Dokumente.Update(chunkedDoc);
                        await _db.SaveChangesAsync();

                        Console.WriteLine($"✅ Chunked Upload abgeschlossen: {file.FileName} ({chunks.Count} Teile)");
                    }
                    else
                    {
                        using var fileStream = file.OpenReadStream();
                        vm.ObjectPath = await _firebase.UploadForUserAsync(
                            fileStream,
                            file.FileName,
                            firma,
                            (vm.Metadata.AbteilungName ?? "allgemein"),
                            (vm.Metadata.Kategorie ?? "allgemein"),
                            false
                        );
                        Console.WriteLine($"☁️ Datei {file.FileName} zu Firebase hochgeladen.");
                    }

                    // =======================================================
                    // 🔹 4️⃣ Abschluss der Analyse
                    // =======================================================
                    vm.Status = DokumentStatus.Analyzed;
                    vm.Progress = 100;
                }
                catch (Exception ex)
                {
                    vm.Metadata.Beschreibung = $"❌ Fehler: {ex.Message}";
                    vm.Status = DokumentStatus.Analyzed;
                }

                Dokumente.Add(vm);
            }

            // =======================================================
            // 🔹 5️⃣ Finalisierung (TempData JSON)
            // =======================================================
            var serialisiert = Dokumente.Select(d => new DokumentSerialisiert
            {
                FileName = d.FileName,
                Metadata = d.Metadata,
                Status = d.Status,
                ObjectPath = d.ObjectPath
            }).ToList();

            DokumenteJson = JsonSerializer.Serialize(serialisiert);
            TempData[nameof(DokumenteJson)] = DokumenteJson;
            TempData.Keep(nameof(DokumenteJson));

            return RedirectToPage(new { analyzed = true });
        }




        private static string NormalizeSegment(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "allgemein";

            value = value.Trim().Replace(" ", "_");

            foreach (var c in new[] { "/", "\\", "#", "?", "%", "&", ":", "*", "\"", "<", ">", "|" })
                value = value.Replace(c, "_");

            var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in normalized)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            value = sb.ToString().ToLowerInvariant();

            return string.IsNullOrWhiteSpace(value) ? "allgemein" : value;
        }



        private static string SanitizeEuroValue(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            // Entfernt "EUR", Leerzeichen, Punkt als Tausendertrennzeichen und ersetzt Komma mit Punkt
            var cleaned = input.Replace("EUR", "", StringComparison.OrdinalIgnoreCase)
                               .Replace("€", "")
                               .Replace(" ", "")
                               .Replace(".", "")
                               .Replace(",", ".")
                               .Trim();

            // Validiert ob Zahl, sonst null
            return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _)
                ? cleaned
                : null;
        }
        private async Task UpsertDokumentIndexAsync(Guid dokumentId, MetadataModel meta, bool save = true)
        {
            var index = await _db.DokumentIndex.FirstOrDefaultAsync(x => x.DokumentId == dokumentId);
            if (index == null)
            {
                index = new DokumentIndex { DokumentId = dokumentId };
                _db.DokumentIndex.Add(index);
            }

            index.Kategorie = meta.Kategorie;
            index.Beschreibung = meta.Beschreibung;
            index.Titel = meta.Titel;
            index.Rechnungsnummer = meta.Rechnungsnummer;
            index.Kundennummer = meta.Kundennummer;
            index.Rechnungsbetrag = decimal.TryParse(meta.Rechnungsbetrag, out var rb) ? rb : null;
            index.Nettobetrag = decimal.TryParse(meta.Nettobetrag, out var nb) ? nb : null;
            index.Gesamtbetrag = decimal.TryParse(meta.Gesamtpreis, out var gp) ? gp : null;
            index.Steuerbetrag = decimal.TryParse(meta.Steuerbetrag, out var sb) ? sb : null;
            index.Rechnungsdatum = DateTime.TryParse(meta.Rechnungsdatum, out var rd) ? rd : null;
            index.Lieferdatum = DateTime.TryParse(meta.Lieferdatum, out var ld) ? ld : null;
            index.Faelligkeitsdatum = DateTime.TryParse(meta.Faelligkeitsdatum, out var fd) ? fd : null;
            index.Zahlungsbedingungen = meta.Zahlungsbedingungen;
            index.lieferart = meta.Lieferart;
            index.ArtikelAnzahl = int.TryParse(meta.ArtikelAnzahl, out var aa) ? aa : null;
            index.Email = meta.Email;
            index.Telefon = meta.Telefon;
            index.Telefax = meta.Telefax;
            index.IBAN = meta.IBAN;
            index.BIC = meta.BIC;
            index.Bankverbindung = meta.Bankverbindung;
            index.SteuerNr = meta.SteuerNr;
            index.UIDNummer = meta.UIDNummer;
            index.Adresse = meta.Adresse;
            index.AbsenderAdresse = meta.AbsenderAdresse;
            index.AnsprechPartner = meta.AnsprechPartner;
            index.Zeitraum = meta.Zeitraum;
            index.Autor = meta.PdfAutor;
            index.Betreff = meta.PdfBetreff;
            index.Schluesselwoerter = meta.PdfSchluesselwoerter;
            index.Website = meta.Website;
            index.OCRText = meta.OCRText;
            index.ObjectPath = meta.ObjectPath;

            if (save)
            {
                await _db.SaveChangesAsync();
            }
        }



        /// <summary>
        /// Détection finale Abteilung + Kategorie (priorité : user > auto-detect > fallback)
        /// </summary>

        public async Task<IActionResult> OnPostSaveAllAsync()
        {
            Console.WriteLine("📥 [START] Speichern aller Dokumente gestartet");

            if (!TempData.TryGetValue(nameof(DokumenteJson), out var obj)
                || obj is not string json || string.IsNullOrWhiteSpace(json))
            {
                TempData["Error"] = "⚠️ Keine Dokumente gefunden!";
                return RedirectToPage(new { analyzed = true });
            }

            var list = JsonSerializer.Deserialize<List<DokumentSerialisiert>>(json);
            if (list == null || list.Count == 0)
            {
                TempData["Error"] = "⚠️ Keine Dokumente analysiert!";
                return RedirectToPage(new { analyzed = true });
            }

            list = list
                .GroupBy(d => d.FileName, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _db.Users.FindAsync(userId);
            var firma = user?.FirmenName?.Trim().ToLowerInvariant();
            var kunde = await ResolveKundeForUserAsync(user);

            if (string.IsNullOrWhiteSpace(firma) || kunde == null)
            {
                TempData["Error"] = "❌ Benutzer/Kunde ungültig.";
                return RedirectToPage(new { analyzed = true });
            }

            Console.WriteLine($"👤 Benutzer: {user?.UserName}, Firma: {firma}");
            Console.WriteLine($"📊 {list.Count} Dokumente werden gespeichert (Duplikate werden ignoriert)");

            var savedDocs = new List<object>();
            int counter = 0;

            foreach (var meta in list)
            {
                if (meta.Status != DokumentStatus.Analyzed)
                {
                    Console.WriteLine($"⚠️ [Skip] {meta.FileName} → Status={meta.Status}");
                    continue;
                }

                var m = meta.Metadata;
                m.Kategorie = m.Kategorie?.Trim().ToLowerInvariant() ?? "allgemein";

                string katKey = $"Dokumente[{list.IndexOf(meta)}].Metadata.Kategorie";
                if (Request.Form.ContainsKey(katKey))
                {
                    string katValue = Request.Form[katKey].ToString();
                    if (!string.IsNullOrWhiteSpace(katValue))
                        m.Kategorie = katValue.Trim().ToLowerInvariant();
                }

                int? abteilungId = null;
                string abtName = null;

                if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                {
                    if (user.AbteilungId == null)
                    {
                        Console.WriteLine($"❌ Benutzer ohne Abteilung → Datei {meta.FileName} übersprungen");
                        continue;
                    }

                    abteilungId = user.AbteilungId;
                    var abt = await _db.Abteilungen.FindAsync(abteilungId);
                    abtName = abt?.Name?.ToLowerInvariant() ?? "allgemein";
                }
                else
                {
                    string abtKey = $"Dokumente[{list.IndexOf(meta)}].Metadata.AbteilungId";
                    if (Request.Form.ContainsKey(abtKey))
                    {
                        string abtValue = Request.Form[abtKey].ToString();
                        if (int.TryParse(abtValue, out var abtId))
                        {
                            var abt = await _db.Abteilungen.FindAsync(abtId);
                            abteilungId = abt?.Id;
                            abtName = abt?.Name?.ToLowerInvariant() ?? "allgemein";
                            Console.WriteLine($"✅ Abteilung gewählt: {abtName}");
                        }
                    }

                    if (abteilungId == null)
                    {
                        var defaultAbt = await _db.Abteilungen.FirstOrDefaultAsync(a => a.Name.ToLower() == "allgemein");
                        if (defaultAbt != null)
                        {
                            abteilungId = defaultAbt.Id;
                            abtName = defaultAbt.Name.ToLowerInvariant();
                            Console.WriteLine($"⚠️ Fallback auf Abteilung: {abtName}");
                        }
                    }
                }

                // ======================== Chunk-Erkennung ========================
                bool isChunked = meta.ObjectPath?.StartsWith("chunked://") == true;
                Guid? chunkedId = null;

                if (isChunked)
                {
                    chunkedId = Guid.Parse(meta.ObjectPath.Replace("chunked://", ""));
                    Console.WriteLine($"🧩 Chunked Dokument erkannt: {meta.FileName}, Id={chunkedId}");
                }

                // ======================== Final Path ========================
                var (finalPath, finalAbteilungId) = DocumentPathHelper.BuildFinalPath(
                    firma, meta.FileName, m.Kategorie, abteilungId, abtName);

                // ======================== MOVE (normal) ========================
                if (!isChunked && !string.IsNullOrWhiteSpace(meta.ObjectPath) &&
                    !string.Equals(meta.ObjectPath, finalPath, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"📦 [Firebase] Move {meta.ObjectPath} → {finalPath}");
                    await _firebase.MoveFilesAsync(meta.ObjectPath, finalPath);
                }

                // ======================== MOVE CHUNKED ========================
                // ======================== MOVE CHUNKED ========================
                if (isChunked && chunkedId.HasValue)
                {
                    string oldFolder = $"dokumente/{firma}/allgemein/allgemein/chunks/{chunkedId}";
                    string newFolder = $"dokumente/{firma}/{abtName ?? "allgemein"}/{m.Kategorie ?? "allgemein"}/chunks/{chunkedId}";

                    Console.WriteLine($"📦 [Chunk Move] Verschiebe einzelne Chunk-Dateien von {oldFolder} → {newFolder}");

                    try
                    {
                        // 🔹 Récupère tous les chunks liés à ce document
                        var chunkFiles = await _db.DokumentChunks
                            .Where(c => c.DokumentId == chunkedId.Value)
                            .ToListAsync();

                        foreach (var chunk in chunkFiles)
                        {
                            var oldFilePath = chunk.FirebasePath;
                            if (string.IsNullOrWhiteSpace(oldFilePath))
                                continue;

                            var fileName = Path.GetFileName(oldFilePath); // ex: chunk_0.bin
                            var newFilePath = $"{newFolder}/{fileName}";

                            Console.WriteLine($"➡️ Verschiebe {oldFilePath} → {newFilePath}");

                            try
                            {
                                await _firebase.MoveFilesAsync(oldFilePath, newFilePath);
                                // ✅ Mise à jour du chemin dans la base
                                chunk.FirebasePath = newFilePath;
                                _db.DokumentChunks.Update(chunk);
                            }
                            catch (Exception innerEx)
                            {
                                Console.WriteLine($"❌ Fehler beim Verschieben von {fileName}: {innerEx.Message}");
                            }
                        }

                        await _db.SaveChangesAsync();
                        Console.WriteLine($"✅ Alle Chunk-Dateien erfolgreich verschoben ({chunkFiles.Count}).");
                    }
                    catch (Exception moveEx)
                    {
                        Console.WriteLine($"❌ Fehler beim Verschieben der Chunks: {moveEx.Message}");
                    }
                }


                // ======================== Dokument prüfen / speichern ========================
                var docId = isChunked ? chunkedId.Value : Guid.NewGuid();

                var existingDoc = await _db.Dokumente
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == docId || d.FileHash == m.FileHash);

                if (existingDoc != null)
                {
                    Console.WriteLine($"⚠️ Dokument {meta.FileName} existiert bereits ({existingDoc.Id}) – wird aktualisiert.");

                    existingDoc.ObjectPath = meta.ObjectPath;
                    existingDoc.Dateipfad = finalPath;
                    existingDoc.Kategorie = m.Kategorie;
                    existingDoc.AbteilungId = finalAbteilungId;
                    existingDoc.IsUpdated = true;
                    existingDoc.dtStatus = DokumentStatus.Fertig;

                    _db.Update(existingDoc);
                    await _db.SaveChangesAsync();
                    continue;
                }

                var doc = new Dokumente
                {
                    Id = docId,
                    ApplicationUserId = userId,
                    KundeId = kunde.Id,
                    Dateiname = meta.FileName,
                    Dateipfad = finalPath,
                    ObjectPath = meta.ObjectPath,
                    HochgeladenAm = DateTime.UtcNow,
                    Kategorie = m.Kategorie,
                    AbteilungId = finalAbteilungId,
                    DokumentStatus = Status.Aktiv,
                    dtStatus = DokumentStatus.Fertig,
                    IsIndexed = true,
                    FileHash = m.FileHash,
                    IsChunked = isChunked
                };

                ApplyMetadataToDocument(doc, m);
                _db.Dokumente.Add(doc);
                await _db.SaveChangesAsync();

                // ======================== Metadaten speichern ========================
                var metaEntity = new Metadaten
                {
                    DokumentId = doc.Id,
                    Titel = m.Titel?.Trim(),
                    Beschreibung = string.IsNullOrWhiteSpace(m.Beschreibung)
                        ? "Grosses PDF ohne OCR (nur Metadaten)"
                        : m.Beschreibung,
                    Kategorie = m.Kategorie,
                    PdfAutor = m.PdfAutor,
                    PdfBetreff = m.PdfBetreff,
                    PdfSchluesselwoerter = m.PdfSchluesselwoerter,
                    Stichworte = m.PdfSchluesselwoerter,
                    Rechnungsnummer = m.Rechnungsnummer,
                    Kundennummer = m.Kundennummer,
                    Rechnungsbetrag = decimal.TryParse(m.Rechnungsbetrag, out var betrag) ? betrag : null,
                    Nettobetrag = decimal.TryParse(m.Nettobetrag, out var netto) ? netto : null,
                    Steuerbetrag = decimal.TryParse(m.Steuerbetrag, out var steuer) ? steuer : null,
                    Gesamtpreis = decimal.TryParse(m.Gesamtpreis, out var preis) ? preis : null,
                    Rechnungsdatum = DateTime.TryParse(m.Rechnungsdatum, out var rDat) ? rDat : null,
                    Lieferdatum = DateTime.TryParse(m.Lieferdatum, out var lDat) ? lDat : null,
                    Faelligkeitsdatum = DateTime.TryParse(m.Faelligkeitsdatum, out var fDat) ? fDat : null,
                    Zahlungsbedingungen = m.Zahlungsbedingungen,
                    Lieferart = m.Lieferart,
                    ArtikelAnzahl = int.TryParse(m.ArtikelAnzahl, out var aAnz) ? aAnz : null,
                    Email = m.Email,
                    Telefon = m.Telefon,
                    Telefax = m.Telefax,
                    IBAN = m.IBAN,
                    BIC = m.BIC,
                    Bankverbindung = m.Bankverbindung,
                    SteuerNr = m.SteuerNr,
                    UIDNummer = m.UIDNummer,
                    Adresse = m.Adresse,
                    AbsenderAdresse = m.AbsenderAdresse,
                    AnsprechPartner = m.AnsprechPartner,
                    Zeitraum = m.Zeitraum,
                    Website = m.Website,
                    OCRText = m.OCRText
                };

                _db.Metadaten.Add(metaEntity);
                await _db.SaveChangesAsync();

                doc.MetadatenId = metaEntity.Id;
                _db.Dokumente.Update(doc);
                await _db.SaveChangesAsync();

                await UpsertDokumentIndexAsync(doc.Id, m);
                await _audit.EnregistrerAsync("Dokument gespeichert", userId, doc.Id);

                savedDocs.Add(new
                {
                    doc.Id,
                    Rechnungsnummer = m.Rechnungsnummer,
                    Rechnungsbetrag = m.Rechnungsbetrag,
                    Titel = m.Titel
                });

                counter++;
            }

            TempData["Success"] = $"✅ {counter} Dokument(e) erfolgreich gespeichert.";
            TempData["SavedDocuments"] = JsonSerializer.Serialize(savedDocs);
            TempData.Remove(nameof(DokumenteJson));

            Console.WriteLine($"🏁 [END] {counter} Dokumente erfolgreich gespeichert.");
            return RedirectToPage(new { analyzed = true });
        }



        private void ApplyMetadataToDocument(Dokumente doc, MetadataModel m)
        {
            if (doc == null || m == null)
                return;

            // 🧠 Sicherstellen, dass Metadatenobjekt existiert
            if (doc.MetadatenObjekt == null)
                doc.MetadatenObjekt = new Metadaten();

            var meta = doc.MetadatenObjekt;

            // 🔹 Textfelder
            meta.Titel = m.Titel;
            meta.Beschreibung = m.Beschreibung;
            meta.Kategorie = m.Kategorie ?? meta.Kategorie;
            meta.Stichworte = m.PdfSchluesselwoerter;

            // 🔹 Rechnungsdaten
            meta.Rechnungsnummer = m.Rechnungsnummer;
            meta.Kundennummer = m.Kundennummer;

            meta.Rechnungsbetrag = decimal.TryParse(m.Rechnungsbetrag, out var rb) ? rb : null;
            meta.Nettobetrag = decimal.TryParse(m.Nettobetrag, out var nb) ? nb : null;
            meta.Gesamtpreis = decimal.TryParse(m.Gesamtpreis, out var gp) ? gp : null;
            meta.Steuerbetrag = decimal.TryParse(m.Steuerbetrag, out var sb) ? sb : null;

            meta.Rechnungsdatum = DateTime.TryParse(m.Rechnungsdatum, out var rd) ? rd : null;
            meta.Lieferdatum = DateTime.TryParse(m.Lieferdatum, out var ld) ? ld : null;
            meta.Faelligkeitsdatum = DateTime.TryParse(m.Faelligkeitsdatum, out var fd) ? fd : null;

            meta.Zahlungsbedingungen = m.Zahlungsbedingungen;
            meta.Lieferart = m.Lieferart;

            meta.ArtikelAnzahl = int.TryParse(m.ArtikelAnzahl, out var aa) ? aa : null;

            // 🔹 Kontakt & Bankdaten
            meta.Email = m.Email;
            meta.Telefon = m.Telefon;
            meta.Telefax = m.Telefax;
            meta.IBAN = m.IBAN;
            meta.BIC = m.BIC;
            meta.Bankverbindung = m.Bankverbindung;
            meta.SteuerNr = m.SteuerNr;
            meta.UIDNummer = m.UIDNummer;

            // 🔹 Adresse & Personeninfos
            meta.Adresse = m.Adresse;
            meta.AbsenderAdresse = m.AbsenderAdresse;
            meta.AnsprechPartner = m.AnsprechPartner;
            meta.Zeitraum = m.Zeitraum;

            // 🔹 PDF-Infos
            meta.PdfAutor = m.PdfAutor;
            meta.PdfBetreff = m.PdfBetreff;
            meta.PdfSchluesselwoerter = m.PdfSchluesselwoerter;
            meta.Website = m.Website;
            meta.OCRText = m.OCRText;

            // 🔹 Dokument-Hauptfelder aktualisieren (nur bei Bedarf)
            doc.Kategorie = meta.Kategorie;
            doc.Beschreibung = meta.Beschreibung;
            doc.Titel = meta.Titel;
        }

        private async Task<Kunden?> ResolveKundeForUserAsync(ApplicationUser user)
        {
            // Cas 1 : Normaler User → CreatedByAdminId nutzen
            if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            {
                if (user.CreatedByAdminId != null)
                {
                    var kundenBenutzer = await _db.KundeBenutzer
                        .Include(k => k.Kunden)
                        .FirstOrDefaultAsync(k => k.ApplicationUserId == user.CreatedByAdminId);

                    return kundenBenutzer?.Kunden;
                }
                return null;
            }

            // Cas 2 : Admin / SuperAdmin → über FirmenName
            if (!string.IsNullOrWhiteSpace(user.FirmenName))
            {
                return await _db.Kunden
                    .FirstOrDefaultAsync(k => k.FirmenName.ToLower() == user.FirmenName.ToLower());
            }

            return null;
        }

        public async Task<IActionResult> OnPostSaveAsync(int index)
        {
            Console.WriteLine($"💾 [START] Speichern einzelnes Dokument index={index}");

            if (!TempData.TryGetValue(nameof(DokumenteJson), out var obj)
                || obj is not string json || string.IsNullOrWhiteSpace(json))
            {
                TempData["Error"] = "⚠️ Keine Dokumente gefunden!";
                return RedirectToPage(new { analyzed = true });
            }

            var list = JsonSerializer.Deserialize<List<DokumentSerialisiert>>(json);
            if (list == null || list.Count <= index)
            {
                TempData["Error"] = "⚠️ Dokument nicht gefunden!";
                return RedirectToPage(new { analyzed = true });
            }

            var meta = list[index];
            if (meta.Status != DokumentStatus.Analyzed)
            {
                TempData["Error"] = "⚠️ Dokument nicht analysiert!";
                return RedirectToPage(new { analyzed = true });
            }

            var m = meta.Metadata;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _db.Users.FindAsync(userId);
            var firma = user?.FirmenName?.Trim()?.ToLowerInvariant();
            var kunde = await ResolveKundeForUserAsync(user);

            if (string.IsNullOrWhiteSpace(firma) || kunde == null)
            {
                TempData["Error"] = "❌ Benutzer/Kunde ungültig.";
                return RedirectToPage(new { analyzed = true });
            }

            // ======================== Abteilung ========================
            int? abteilungId = null;
            string abtName = null;

            if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
            {
                if (user.AbteilungId == null)
                {
                    TempData["Error"] = "❌ Benutzer ist keiner Abteilung zugeordnet.";
                    return RedirectToPage(new { analyzed = true });
                }

                abteilungId = user.AbteilungId;
                var abt = await _db.Abteilungen.FindAsync(abteilungId);
                abtName = abt?.Name?.ToLowerInvariant() ?? "allgemein";
            }
            else
            {
                string abtKey = $"Dokumente[{index}].Metadata.AbteilungId";
                if (Request.Form.ContainsKey(abtKey))
                {
                    var abtValue = Request.Form[abtKey].ToString();
                    if (int.TryParse(abtValue, out var abtId))
                    {
                        var abt = await _db.Abteilungen.FindAsync(abtId);
                        abteilungId = abt?.Id;
                        abtName = abt?.Name?.ToLowerInvariant() ?? "allgemein";
                        Console.WriteLine($"✅ Abteilung gewählt: {abtName}");
                    }
                }

                if (abteilungId == null)
                {
                    TempData["Error"] = "❌ Bitte wählen Sie eine Abteilung aus.";
                    return RedirectToPage(new { analyzed = true });
                }
            }

            // ======================== Kategorie ========================
            string katKey = $"Dokumente[{index}].Metadata.Kategorie";
            string katValue = Request.Form.ContainsKey(katKey)
                ? Request.Form[katKey].ToString()
                : null;

            m.Kategorie = !string.IsNullOrWhiteSpace(katValue)
                ? katValue.Trim().ToLowerInvariant()
                : m.Kategorie?.Trim()?.ToLowerInvariant() ?? "allgemein";

            // ======================== Titel / Beschreibung Fallback ========================
            if (string.IsNullOrWhiteSpace(m.Titel))
            {
                m.Titel = Path.GetFileNameWithoutExtension(meta.FileName);
                if (string.IsNullOrWhiteSpace(m.OCRText))
                    m.Titel += " (ohne OCR / automatisch erkannt)";
            }

            if (string.IsNullOrWhiteSpace(m.Beschreibung))
                m.Beschreibung = "Grosses PDF ohne OCR (nur Metadaten)";

            // ======================== Chunked-Erkennung ========================
            bool isChunked = meta.ObjectPath?.StartsWith("chunked://") == true;
            Guid? chunkedId = null;

            if (isChunked)
            {
                chunkedId = Guid.Parse(meta.ObjectPath.Replace("chunked://", ""));
                Console.WriteLine($"🧩 Chunked Dokument erkannt: {meta.FileName}, Id={chunkedId}");
            }

            // ======================== Final Path ========================
            var (finalPath, finalAbteilungId) = DocumentPathHelper.BuildFinalPath(
                firma, meta.FileName, m.Kategorie, abteilungId, abtName);

            // 🔁 Move (nur für non-chunked)
            if (!isChunked && !string.IsNullOrWhiteSpace(meta.ObjectPath) &&
                !string.Equals(meta.ObjectPath, finalPath, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"📦 [Firebase] Move {meta.ObjectPath} → {finalPath}");
                await _firebase.MoveFilesAsync(meta.ObjectPath, finalPath);
            }

            // ======================== EXISTENZ PRÜFEN ========================
            var existingDoc = await _db.Dokumente
                .Include(d => d.MetadatenObjekt)
                .FirstOrDefaultAsync(d =>
                    d.FileHash == m.FileHash ||
                    (d.Dateiname == meta.FileName && d.Dateipfad == finalPath && d.KundeId == kunde.Id));

            if (existingDoc != null)
            {
                Console.WriteLine($"⚠️ Dokument existiert bereits ({existingDoc.Id}) – wird aktualisiert.");

                var metaEntity = existingDoc.MetadatenObjekt ?? new Metadaten { DokumentId = existingDoc.Id };

                metaEntity.Titel = m.Titel?.Trim();
                metaEntity.Beschreibung = m.Beschreibung;
                metaEntity.Kategorie = m.Kategorie;
                metaEntity.PdfAutor = m.PdfAutor;
                metaEntity.PdfBetreff = m.PdfBetreff;
                metaEntity.PdfSchluesselwoerter = m.PdfSchluesselwoerter;
                metaEntity.Stichworte = m.PdfSchluesselwoerter;
                metaEntity.Rechnungsnummer = m.Rechnungsnummer;
                metaEntity.Kundennummer = m.Kundennummer;

                _db.Metadaten.Update(metaEntity);
                await _db.SaveChangesAsync();

                existingDoc.MetadatenId = metaEntity.Id;
                existingDoc.Kategorie = m.Kategorie;
                existingDoc.AbteilungId = finalAbteilungId;
                existingDoc.ObjectPath = meta.ObjectPath;
                existingDoc.IsChunked = isChunked;
                existingDoc.HochgeladenAm = DateTime.UtcNow;
                existingDoc.IsUpdated = true;
                existingDoc.DokumentStatus = Status.Aktiv;
                existingDoc.dtStatus = DokumentStatus.Fertig;

                _db.Dokumente.Update(existingDoc);
                await _db.SaveChangesAsync();

                await UpsertDokumentIndexAsync(existingDoc.Id, m);
                await _audit.EnregistrerAsync("Dokument aktualisiert", userId, existingDoc.Id);

                TempData["Success"] = $"✅ Dokument aktualisiert: {existingDoc.Dateiname}";
                Console.WriteLine($"✅ Dokument aktualisiert: {existingDoc.Dateiname}");
                return RedirectToPage(new { analyzed = true });
            }

            // ======================== NEUES DOKUMENT ========================
            var metaEntityNew = new Metadaten
            {
                Titel = m.Titel?.Trim(),
                Beschreibung = m.Beschreibung,
                Kategorie = m.Kategorie,
                PdfAutor = m.PdfAutor,
                PdfBetreff = m.PdfBetreff,
                PdfSchluesselwoerter = m.PdfSchluesselwoerter,
                Stichworte = m.PdfSchluesselwoerter,
                Rechnungsnummer = m.Rechnungsnummer,
                Kundennummer = m.Kundennummer,
                Rechnungsbetrag = decimal.TryParse(m.Rechnungsbetrag, out var betrag) ? betrag : null,
                Nettobetrag = decimal.TryParse(m.Nettobetrag, out var netto) ? netto : null,
                Steuerbetrag = decimal.TryParse(m.Steuerbetrag, out var steuer) ? steuer : null,
                Gesamtpreis = decimal.TryParse(m.Gesamtpreis, out var preis) ? preis : null,
                Rechnungsdatum = DateTime.TryParse(m.Rechnungsdatum, out var rDat) ? rDat : null,
                Lieferdatum = DateTime.TryParse(m.Lieferdatum, out var lDat) ? lDat : null,
                Faelligkeitsdatum = DateTime.TryParse(m.Faelligkeitsdatum, out var fDat) ? fDat : null,
                Zahlungsbedingungen = m.Zahlungsbedingungen,
                Lieferart = m.Lieferart,
                ArtikelAnzahl = int.TryParse(m.ArtikelAnzahl, out var aAnz) ? aAnz : null,
                Email = m.Email,
                Telefon = m.Telefon,
                Telefax = m.Telefax,
                IBAN = m.IBAN,
                BIC = m.BIC,
                Bankverbindung = m.Bankverbindung,
                SteuerNr = m.SteuerNr,
                UIDNummer = m.UIDNummer,
                Adresse = m.Adresse,
                AbsenderAdresse = m.AbsenderAdresse,
                AnsprechPartner = m.AnsprechPartner,
                Zeitraum = m.Zeitraum,
                Website = m.Website,
                OCRText = m.OCRText
            };

            _db.Metadaten.Add(metaEntityNew);
            await _db.SaveChangesAsync();

            var newDoc = new Dokumente
            {
                Id = isChunked ? chunkedId.Value : Guid.NewGuid(),
                ApplicationUserId = userId,
                KundeId = kunde.Id,
                Dateiname = meta.FileName,
                Dateipfad = finalPath,
                ObjectPath = meta.ObjectPath,
                HochgeladenAm = DateTime.UtcNow,
                Kategorie = m.Kategorie,
                AbteilungId = finalAbteilungId,
                DokumentStatus = Status.Aktiv,
                dtStatus = DokumentStatus.Fertig,
                IsIndexed = true,
                MetadatenId = metaEntityNew.Id,
                FileHash = m.FileHash,
                IsChunked = isChunked
            };

            ApplyMetadataToDocument(newDoc, m);
            _db.Dokumente.Add(newDoc);
            await _db.SaveChangesAsync();

            await UpsertDokumentIndexAsync(newDoc.Id, m);
            await _audit.EnregistrerAsync("Dokument gespeichert", userId, newDoc.Id);

            TempData["Success"] = $"✅ Neues Dokument gespeichert: {newDoc.Dateiname}";
            Console.WriteLine($"✅ Neues Dokument gespeichert: {newDoc.Dateiname}");

            return RedirectToPage(new { analyzed = true });
        }


        public async Task<IActionResult> OnPostRemoveAsync(int index)
        {
            if (!TempData.TryGetValue(nameof(DokumenteJson), out var obj)
                || obj is not string json || string.IsNullOrWhiteSpace(json))
            {
                TempData["Error"] = "⚠️ Keine Dokumente gefunden!";
                return RedirectToPage(new { analyzed = true });
            }
            var list = JsonSerializer.Deserialize<List<DokumentSerialisiert>>(json);
            if (list == null || list.Count <= index)
            {
                TempData["Error"] = "⚠️ Dokument nicht gefunden!";
                return RedirectToPage(new { analyzed = true });
            }
            var removed = list[index];
            list.RemoveAt(index);

            // Liste neu serialisieren und speichern
            TempData[nameof(DokumenteJson)] = JsonSerializer.Serialize(list);
            TempData["Success"] = $"❌ Dokument gelöscht: {removed.FileName}";
            TempData.Keep(nameof(DokumenteJson));
            return RedirectToPage(new { analyzed = true });
        }

        private (string Kategorie, string Abteilung) DetectKategorieUndAbteilung(string fileName, string ocrText)
        {
            if (string.IsNullOrWhiteSpace(fileName) && string.IsNullOrWhiteSpace(ocrText))
                return ("Unbekannt", "Allgemein");

            string content = (fileName + " " + ocrText).ToLowerInvariant();

            // ================================
            // 📚 Règles (en Allemand uniquement)
            // ================================
            var rules = new List<(string Kategorie, string Abteilung, string[] Keywords)>
    {
        // 👨‍💼 HR
        ("Gehaltsabrechnungen", "HR", new[] { "gehaltsabrechnung", "lohnabrechnung" }),
        ("Arbeitsverträge", "HR", new[] { "arbeitsvertrag" }),
        ("Mitarbeiterakten", "HR", new[] { "personalakte" }),
        ("Arbeitszeugnisse", "HR", new[] { "arbeitszeugnis" }),
        ("Schulungsunterlagen", "HR", new[] { "schulung", "weiterbildung" }),

        // 🎓 Studium / Ausbildung
        ("Diplome", "Studium", new[] { "diplom", "abschlusszeugnis" }),
        ("Bachelor", "Studium", new[] { "bachelor" }),
        ("Master", "Studium", new[] { "master" }),
        ("Zertifikate", "Studium", new[] { "zertifikat", "bescheinigung", "urkunde" }),
        ("Notenübersicht", "Studium", new[] { "notenübersicht", "transkript" }),
        ("Abschlussarbeiten", "Studium", new[] { "abschlussarbeit", "thesis", "dissertation" }),

        // 💰 Finanzen
        ("Rechnungen", "Finanzen", new[] { "rechnung", "invoice" }),
        ("Gutschriften", "Finanzen", new[] { "gutschrift" }),
        ("Steuerunterlagen", "Finanzen", new[] { "steuer", "umsatzsteuer", "einkommensteuer" }),
        ("Bankunterlagen", "Finanzen", new[] { "kontoauszug", "bank" }),
        ("Jahresabschlüsse", "Finanzen", new[] { "jahresabschluss", "bilanz" }),
        ("Budgets", "Finanzen", new[] { "budget", "planung" }),
        ("Spesenabrechnungen", "Finanzen", new[] { "spesenabrechnung" }),
        ("Finanzberichte", "Finanzen", new[] { "finanzbericht" }),

        // 📑 Recht
        ("Verträge", "Recht", new[] { "vertrag" }),
        ("Genehmigungen", "Recht", new[] { "genehmigung", "bewilligung" }),
        ("Compliance", "Recht", new[] { "compliance" }),
        ("Rechtsstreitigkeiten", "Recht", new[] { "klage", "prozess", "gericht" }),

        // 🛒 Einkauf
        ("Bestellungen", "Einkauf", new[] { "bestellung" }),
        ("Angebote", "Einkauf", new[] { "angebot", "offerte" }),
        ("Lieferverträge", "Einkauf", new[] { "liefervertrag" }),
        ("Lieferscheine", "Einkauf", new[] { "lieferschein" }),

        // 💼 Verkauf
        ("Kundenaufträge", "Verkauf", new[] { "kundenauftrag" }),
        ("Sales Reports", "Verkauf", new[] { "umsatzbericht" }),

        // 🚚 Logistik
        ("Frachtbriefe", "Logistik", new[] { "frachtbrief" }),
        ("Zolldokumente", "Logistik", new[] { "zoll" }),
        ("Inventar", "Logistik", new[] { "inventar" }),

        // 🛠️ IT / Support
        ("Technische Zeichnungen", "Technik", new[] { "zeichnung", "konstruktionsplan" }),
        ("Handbücher", "Support", new[] { "handbuch" }),
        ("Softwaredokumentation", "IT", new[] { "dokumentation", "benutzerhandbuch" }),
        ("Lizenzen", "IT", new[] { "lizenz" }),
        ("IT Audits", "IT", new[] { "it audit", "sicherheitsaudit" }),

        // 📊 Management
        ("Projektpläne", "Projektmanagement", new[] { "projektplan" }),
        ("Projektberichte", "Projektmanagement", new[] { "projektbericht" }),
        ("Protokolle", "Management", new[] { "protokoll" }),
        ("KPI Reports", "Management", new[] { "kpi", "kennzahlen" }),

        // 📣 Marketing
        ("Marketingunterlagen", "Marketing", new[] { "marketing", "kampagne", "werbung" }),
        ("Broschüren", "Marketing", new[] { "broschüre", "flyer" }),
        ("Pressemitteilungen", "Marketing", new[] { "pressemitteilung" }),

        // ⚙️ Qualität
        ("ISO-Zertifikate", "Qualität", new[] { "iso", "zertifikat" }),
        ("Qualitätsberichte", "Qualität", new[] { "qualitätsbericht" }),
        ("Audits", "Qualität", new[] { "auditplan" }),

        // 🏢 Verwaltung
        ("Memos", "Verwaltung", new[] { "memo", "notiz" }),
        ("Richtlinien", "Verwaltung", new[] { "richtlinie" }),
        ("Allgemeine Dokumente", "Allgemein", new[] { "divers", "misc" }),
    };

            // ================================
            // 📊 Scoring intelligent
            // ================================
            var bestMatch = ("Unbekannt", "Allgemein");
            int bestScore = 0;

            foreach (var rule in rules)
            {
                int score = rule.Keywords.Count(k =>
                    Regex.IsMatch(content, $@"(?<![a-z0-9]){Regex.Escape(k)}(?![a-z0-9])", RegexOptions.IgnoreCase));

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = (rule.Kategorie, rule.Abteilung);
                }
            }

            return bestMatch;
        }

        private async Task<bool> HasAccessToFolderAsync(ApplicationUser user, string targetFolder)
        {
            var claims = await _db.UserClaims
                .Where(c => c.UserId == user.Id && c.ClaimType == "FolderAccess")
                .Select(c => c.ClaimValue)
                .ToListAsync();

            // 🔹 Wenn KEINE Claims → Default = Vollzugriff
            if (claims == null || claims.Count == 0)
                return true;

            // 🔹 Prüfen ob Zielordner von den Claims abgedeckt ist
            return claims.Any(c =>
                targetFolder.StartsWith(c, StringComparison.OrdinalIgnoreCase));
        }

        public static string GetBadgeClass(DokumentStatus status) => status switch
        {
            DokumentStatus.Analyzed => "info",
            DokumentStatus.Fertig => "success",
            DokumentStatus.Pending => "secondary",
            _ => "dark"
        };


    }
    public class DokumentSerialisiert
    {
        public Guid? Id { get; set; }
        public string FileName { get; set; }
        public MetadataModel Metadata { get; set; }
        public DokumentStatus Status { get; set; }
        public string ObjectPath { get; set; }
        public DateTime HochgeladenAm { get; set; }
        public bool IsGrossDoc { get; set; }
        public string AbteilungName { get; set; }
        public int? AbteilungId { get; set; }
    }

}
