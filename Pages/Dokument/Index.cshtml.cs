using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DinkToPdf;
using DinkToPdf.Contracts;
using DmsProjeckt.Controllers;
using DmsProjeckt.Data;
using DmsProjeckt.Helpers;
using DmsProjeckt.Service;
using DmsProjeckt.Services;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Firebase.Database;
using Google.Api;
using Google.Cloud.Storage.V1;
using iText.Commons.Actions.Contexts;
using iText.Kernel.Utils.Objectpathitems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using Org.BouncyCastle.Ocsp;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static DmsProjeckt.Pages.Dokument.AlleVersionenModel;
using static DmsProjeckt.Pages.Tests.UploadMultiModel;
using DrawingColor = System.Drawing.Color;

namespace DmsProjeckt.Pages.Dokument
{
    [IgnoreAntiforgeryToken]
    // [Authorize(Roles = "Admin,SuperAdmin")]
    public class IndexModel : PageModel
    {
        // ===========================
        //    Services / Constructor
        // ===========================
        private readonly ApplicationDbContext _db;
        private readonly FirebaseStorageService _storageService;
        private readonly IConverter _pdfConverter;
        private readonly IRazorViewToStringRenderer _viewRenderer;
        private readonly ILogger<IndexModel> _logger;
        private readonly AuditLogDokumentService _auditLogDokumentService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly ChunkService _chunkService;
        public List<Abteilung> AlleAbteilungen { get; set; }


        public IndexModel(
            ApplicationDbContext db,
            IConverter pdfConverter,
            IRazorViewToStringRenderer viewRenderer,
            UserManager<ApplicationUser> userManager,
            FirebaseStorageService storageService,
            ILogger<IndexModel> logger,
            AuditLogDokumentService auditLogDokumentService,
            EmailService emailService,
            DbContextOptions<ApplicationDbContext> dbOptions,
            ChunkService chunkService)
        {
            _db = db;
            _pdfConverter = pdfConverter;
            _viewRenderer = viewRenderer;
            _storageService = storageService;
            _userManager = userManager;
            _logger = logger;
            _auditLogDokumentService = auditLogDokumentService;
            _emailService = emailService;
            _dbOptions = dbOptions;
            _chunkService = chunkService;
        }

        // ===========================
        //     UI & Filter Bindings
        // ===========================
        [BindProperty(SupportsGet = true)] public string Typ { get; set; }
        [BindProperty(SupportsGet = true)] public string Kategorie { get; set; }
        [BindProperty(SupportsGet = true)] public string Status { get; set; }
        [BindProperty(SupportsGet = true)] public string Benutzer { get; set; }
        [BindProperty(SupportsGet = true)] public string BenutzerId { get; set; }
        [BindProperty(SupportsGet = true)] public string Dateiname { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Von { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Bis { get; set; }
        [BindProperty(SupportsGet = true)] public string Rechnungsnummer { get; set; }
        [BindProperty(SupportsGet = true)] public string Kundennummer { get; set; }
        [BindProperty(SupportsGet = true)] public string PdfAutor { get; set; }
        [BindProperty(SupportsGet = true)] public string OCRText { get; set; }
        [BindProperty(SupportsGet = true)] public string Query { get; set; }
        [BindProperty(SupportsGet = true)] public string? SelectedFolder { get; set; }
        [BindProperty(SupportsGet = false)] public string NewFolder { get; set; }
        [NotMapped] public string Unterschrieben { get; set; }

        public int PageSize { get; set; } = 20;
        public int PageNumber { get; set; } = 1;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        // ===========================
        //     Data Models/Listen
        // ===========================
        public List<Dokumente> DokumentListe { get; set; } = new();
        public List<Dokumente> GefundeneDokumente { get; set; } = new();
        public List<Dokumente> SignierteDokumente { get; set; }
        public List<Dokumente> NichtSignierteDokumente { get; set; }
        public List<BenutzerMetadaten> MetaListe { get; set; }
        public List<FolderItem> FolderListe { get; set; } = new();
        public List<AuditLogDokument> AuditLogs { get; set; } = new();
        public List<string> TypListe { get; set; } = new();
        public List<string> AlleKategorien { get; set; } = new();
        public List<(string Name, string Path)> AlleOrdner { get; set; } = new();
        public List<ApplicationUser> AlleBenutzer { get; set; } = new();
        public List<DmsFolder> ExplorerTree { get; set; } = new();
        public Dictionary<Guid, int> DokumentVersionenMap { get; set; } = new();
        // public string Firma { get; set; }
        public bool IsVersion { get; set; } = false;
        public string RowCssClass { get; set; }


        [BindProperty] public List<IFormFile> Files { get; set; } = new();
        public List<DokumentIndex> IndexListe { get; set; }
        public List<Guid> DokumenteAvecLogs { get; set; } = new();

        public DmsFolder RootFolder { get; set; }


        public async Task<IActionResult> OnPostUploadAsync(IFormFile uploadedFile, Dokumente dokument)
        {
            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                TempData["Error"] = "⚠️ Keine Datei ausgewählt.";
                return RedirectToPage();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "⚠️ Benutzer nicht gefunden.";
                return RedirectToPage();
            }

            // 🔹 Normaliser le nom de la société
            var firma = user.FirmenName?.Trim().ToLowerInvariant() ?? "unbekannt";

            // 🔹 Détecter automatiquement la catégorie si non renseignée
            var category = !string.IsNullOrWhiteSpace(dokument.Kategorie)
                ? dokument.Kategorie.Trim()
                : DetectCategoryFromFileName(uploadedFile.FileName); // ✅ auto-détection

            if (string.IsNullOrWhiteSpace(category))
                category = "ohne_kategorie"; // valeur par défaut

            // 🔹 Construire le chemin complet pour la catégorie
            var categoryFolderPath = $"dokumente/{firma}/{category}/";

            // 🔹 Vérifier si le dossier existe dans Firebase, sinon le créer
            var existingFolders = await _storageService.ListFoldersAsync($"dokumente/{firma}/");
            if (!existingFolders.Any(f => string.Equals(f.Name, category, StringComparison.OrdinalIgnoreCase)))
            {
                await _storageService.CreateFolderAsync(categoryFolderPath);
            }

            // 🔹 Nom du fichier
            var fileName = Path.GetFileName(uploadedFile.FileName);

            // 🔹 Chemin complet pour stockage
            var fullPath = $"{categoryFolderPath}{fileName}";

            // 🔹 Upload vers Firebase
            using var stream = uploadedFile.OpenReadStream();
            await _storageService.UploadAsync(stream, fullPath);

            // 🔹 Enregistrer le document en base
            dokument.Id = Guid.NewGuid();
            dokument.ApplicationUserId = userId;
            dokument.Dateiname = fileName;
            dokument.Kategorie = category; // ✅ On sauvegarde la catégorie détectée
            dokument.Dateipfad = fullPath;
            dokument.ObjectPath = fullPath;
            dokument.HochgeladenAm = DateTime.UtcNow;
            dokument.IsVersion = false;
            dokument.OriginalId = Guid.Empty;

            _db.Dokumente.Add(dokument);
            await _db.SaveChangesAsync();

            TempData["Success"] = "✅ Datei erfolgreich hochgeladen!";
            return RedirectToPage();
        }

        // 🔹 Détection simple de catégorie
        private string DetectCategoryFromFileName(string filename)
        {
            filename = filename.ToLowerInvariant();

            // Factures et finances
            if (filename.Contains("rechnung") || filename.Contains("invoice") || filename.Contains("facture"))
                return "rechnungen";
            if (filename.Contains("angebot") || filename.Contains("offre") || filename.Contains("offer"))
                return "angebote";
            if (filename.Contains("bestellung") || filename.Contains("order") || filename.Contains("purchase"))
                return "bestellungen";
            if (filename.Contains("quittung") || filename.Contains("receipt"))
                return "quittungen";
            if (filename.Contains("gebühr") || filename.Contains("gebuehr") || filename.Contains("fee"))
                return "gebuehren";
            if (filename.Contains("gutschrift") || filename.Contains("credit"))
                return "gutschriften";

            // Correspondance et communication
            if (filename.Contains("korres") || filename.Contains("mail") || filename.Contains("brief") || filename.Contains("letter"))
                return "korrespondenz";

            // Ressources humaines
            if (filename.Contains("lebenslauf") || filename.Contains("cv") || filename.Contains("bewerbung") || filename.Contains("application"))
                return "bewerbungen";
            if (filename.Contains("vertrag") || filename.Contains("contract") || filename.Contains("agreement"))
                return "vertraege";
            if (filename.Contains("zeugnis") || filename.Contains("certificate") || filename.Contains("diplom"))
                return "zeugnisse";

            // Administratif & Légal
            if (filename.Contains("lizenz") || filename.Contains("license"))
                return "lizenzen";
            if (filename.Contains("versicherung") || filename.Contains("insurance"))
                return "versicherungen";
            if (filename.Contains("genehmigung") || filename.Contains("permit") || filename.Contains("authorisation"))
                return "genehmigungen";

            // Comptabilité & Fiscalité
            if (filename.Contains("steuer") || filename.Contains("tax"))
                return "steuerunterlagen";
            if (filename.Contains("bilanz") || filename.Contains("balance"))
                return "bilanzen";

            // Technique et Projets
            if (filename.Contains("projekt") || filename.Contains("project"))
                return "projekte";
            if (filename.Contains("handbuch") || filename.Contains("manual") || filename.Contains("guide"))
                return "handbuecher";
            if (filename.Contains("plan") || filename.Contains("drawing") || filename.Contains("blueprint"))
                return "plaene";

            // Divers
            if (filename.Contains("bericht") || filename.Contains("report"))
                return "berichte";
            if (filename.Contains("foto") || filename.Contains("photo") || filename.Contains("image") || filename.Contains("bild"))
                return "bilder";

            // Fallback
            return "sonstige"; // catégorie par défaut
        }



        public List<Dokumente> OriginaleDokumente { get; set; }
        public List<Dokumente> VersionierteDokumente { get; set; }
        public List<Dokumente> ArchivierteDokumente { get; set; }
        public List<Dokumente> SonstigeDokumente { get; set; }


        public class DokumentViewModel
        {
            public Dokumente Dokument { get; set; }
            public int CommentCount { get; set; }
            public string CommentSummary { get; set; }
        }
        public List<DokumentViewModel> DokumenteMitKommentare { get; set; } = new();
        public class VersionItem
        {
            public string OriginalName { get; set; }
            public string Dateiname { get; set; }
            public string SasUrl { get; set; }
            public string ObjectPath { get; set; }
            public DateTime HochgeladenAm { get; set; }
            public Guid DokumentId { get; set; }
            public string Kategorie { get; set; }
            public int CommentCount { get; set; }
            public string CommentSummary { get; set; }
        }
        public List<VersionGroup> GruppierteVersionen { get; set; } = new();
        public class VersionGroup
        {
            public string OriginalName { get; set; }
            public List<VersionItem> Versions { get; set; }
        }
        public string? Selected { get; set; }
        public string? InitialFolderPath { get; set; }
        public string Firma { get; set; }
        public string FirmaName { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string AbteilungName { get; set; } = string.Empty;
        public string ProfilbildUrl { get; set; } = "/images/default-avatar.png";
        public string UserRoles { get; set; } = string.Empty;
        
        public List<string> AbteilungenMitDocs { get; set; } = new();

        public async Task OnGetAsync(string? SelectedFolder, string? Kategorie, DateTime? Von, DateTime? Bis, int pageNumber = 1, int pageSize = 15)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ================= BENUTZERDATEN =================
            var user = await _userManager.Users
                .Include(u => u.Abteilung)
                .FirstOrDefaultAsync(u => u.Id == userId);

            PageNumber = pageNumber;
            PageSize = pageSize;

            Firma = user?.FirmenName?.Trim().ToLowerInvariant();
            FirmaName = user?.FirmenName ?? "Meine Firma GmbH";
            FullName = user?.FullName ?? "";
            AbteilungName = user?.Abteilung?.Name ?? "Unbekannt";
            ProfilbildUrl = string.IsNullOrEmpty(user?.ProfilbildUrl)
                ? "/images/default-avatar.png"
                : user.ProfilbildUrl;

            // Rollen laden
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                UserRoles = roles.Any() ? string.Join(", ", roles) : "Keine Rolle";
            }
            else
            {
                UserRoles = "Unbekannt";
            }

            if (string.IsNullOrWhiteSpace(Firma))
                return;

            // 🧭 Ajuster automatiquement Firma selon les claims (pour éviter mismatch)
            var firstClaim = User.FindFirst("FolderAccess")?.Value;
            if (!string.IsNullOrEmpty(firstClaim))
            {
                var parts = firstClaim.Split('/');
                if (parts.Length > 1)
                {
                    Firma = parts[1].Trim().ToLowerInvariant();
                    Console.WriteLine($"🧭 Firma ajustée selon claims : {Firma}");
                }
            }

            var rootPath = $"dokumente/{Firma}";

            // ================= ORIGINALE + VERSIONEN =================

            // Étape 1 : Charger depuis la base (sans User.HasAccess)
            var originaleTemp = await _db.Dokumente
                .Include(d => d.Abteilung)
                .Include(d => d.MetadatenObjekt)
                .Where(d =>
                    d.Abteilung != null &&
                    !string.IsNullOrEmpty(d.Dateipfad) &&
                    !string.IsNullOrEmpty(d.Dateiname))
                .ToListAsync();

            // Étape 2 : Filtrer côté C# (EF ne traduit pas HasAccess en SQL)
            var originale = originaleTemp
                .Where(d => User.HasAccess($"dokumente/{Firma}/{d.Abteilung?.Name}/*"))
                .ToList();

            foreach (var c in User.FindAll("FolderAccess"))
                Console.WriteLine($"📂 Claim: {c.Value}");

            // ================= VERSIONEN =================
            var versionenTemp = await _db.DokumentVersionen
                .Include(v => v.Abteilung)
                .ToListAsync();

            var versionen = versionenTemp
                .Where(v => User.HasAccess($"dokumente/{Firma}/{v.Abteilung?.Name}/*"))
                .ToList();

            // 🚀 Versionen in Dokumente umwandeln
            var versionDocs = versionen.Select(v =>
            {
                var meta = new Metadaten();

                if (!string.IsNullOrWhiteSpace(v.MetadataJson))
                {
                    try
                    {
                        var metaDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v.MetadataJson);
                        if (metaDict != null)
                        {
                            meta.Titel = metaDict.GetValueOrDefault("Titel")?.ToString();
                            meta.Beschreibung = metaDict.GetValueOrDefault("Beschreibung")?.ToString();
                            meta.Kategorie = metaDict.GetValueOrDefault("Kategorie")?.ToString();
                            meta.Rechnungsnummer = metaDict.GetValueOrDefault("Rechnungsnummer")?.ToString();
                            meta.Kundennummer = metaDict.GetValueOrDefault("Kundennummer")?.ToString();
                            meta.Email = metaDict.GetValueOrDefault("Email")?.ToString();
                            meta.Telefon = metaDict.GetValueOrDefault("Telefon")?.ToString();
                            meta.IBAN = metaDict.GetValueOrDefault("IBAN")?.ToString();
                            meta.BIC = metaDict.GetValueOrDefault("BIC")?.ToString();

                            if (decimal.TryParse(metaDict.GetValueOrDefault("Rechnungsbetrag")?.ToString(), out var rb))
                                meta.Rechnungsbetrag = rb;
                            if (decimal.TryParse(metaDict.GetValueOrDefault("Nettobetrag")?.ToString(), out var nb))
                                meta.Nettobetrag = nb;
                            if (decimal.TryParse(metaDict.GetValueOrDefault("Gesamtpreis")?.ToString(), out var gp))
                                meta.Gesamtpreis = gp;
                            if (decimal.TryParse(metaDict.GetValueOrDefault("Steuerbetrag")?.ToString(), out var sb))
                                meta.Steuerbetrag = sb;

                            if (DateTime.TryParse(metaDict.GetValueOrDefault("Rechnungsdatum")?.ToString(), out var rd))
                                meta.Rechnungsdatum = rd;
                            if (DateTime.TryParse(metaDict.GetValueOrDefault("Faelligkeitsdatum")?.ToString(), out var fd))
                                meta.Faelligkeitsdatum = fd;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Fehler beim Deserialisieren der Metadaten: {ex.Message}");
                    }
                }

                return new Dokumente
                {
                    Id = Guid.NewGuid(),
                    Dateiname = v.Dateiname,
                    Kategorie = "versionen",
                    ObjectPath = v.ObjectPath,
                    Dateipfad = v.Dateipfad,
                    ApplicationUserId = v.ApplicationUserId,
                    ApplicationUser = v.ApplicationUser,
                    AbteilungId = v.AbteilungId,
                    Abteilung = v.Abteilung,
                    HochgeladenAm = v.HochgeladenAm,
                    IsVersion = true,
                    EstSigne = v.EstSigne,
                    IsIndexed = false,
                    MetadatenObjekt = meta
                };
            }).ToList();

            // 🌳 Explorer-Tree: ALLE Dokumente
            var explorerDocs = originale.ToList();
            explorerDocs.AddRange(versionDocs);

            // 🧠 Metadaten aus beiden Richtungen laden
            try
            {
                var dokumentIds = explorerDocs.Select(d => d.Id).ToList();

                if (dokumentIds.Any())
                {
                    var dokumentMetadaten = await _db.Metadaten
                        .Where(m => (m.DokumentId != null && dokumentIds.Contains(m.DokumentId.Value)) ||
                                    _db.Dokumente.Any(d => d.MetadatenId == m.Id && dokumentIds.Contains(d.Id)))
                        .ToListAsync();

                    int assignedCount = 0;
                    foreach (var doc in explorerDocs)
                    {
                        var meta = dokumentMetadaten.FirstOrDefault(m =>
                            (m.DokumentId.HasValue && m.DokumentId.Value == doc.Id) ||
                            (doc.MetadatenId.HasValue && m.Id == doc.MetadatenId.Value));

                        if (meta != null)
                        {
                            doc.MetadatenObjekt = meta;
                            assignedCount++;
                        }
                    }

                    Console.WriteLine($"📦 {assignedCount} Metadaten erfolgreich verknüpft.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Fehler beim Laden der Metadaten: {ex.Message}");
            }

            // 📂 Explorer-Baum aufbauen
            ExplorerTree = await _storageService.BuildExplorerTreeAsync(rootPath, explorerDocs);

            // 🔒 Nur erlaubte Ordner anzeigen
            ExplorerTree = ExplorerTree
                .Where(folder =>
                    User.HasAccess($"dokumente/{Firma}/{folder.Name}/*") ||
                    folder.SubFolders.Any(sf => User.HasAccess($"dokumente/{Firma}/{folder.Name}/{sf.Name}"))
                ).ToList();

            // 📑 Alle Dokumente (Original + Versionen + Archiv)
            var alleDocs = originale.Concat(versionDocs)
                .OrderByDescending(d => d.HochgeladenAm)
                .ToList();

            // 📋 Filter anwenden
            if (!string.IsNullOrEmpty(SelectedFolder))
                alleDocs = alleDocs.Where(d => d.Abteilung?.Name.Equals(SelectedFolder, StringComparison.OrdinalIgnoreCase) == true).ToList();

            if (!string.IsNullOrEmpty(Kategorie))
                alleDocs = alleDocs.Where(d => d.Kategorie?.Equals(Kategorie, StringComparison.OrdinalIgnoreCase) == true).ToList();

            if (Von.HasValue)
                alleDocs = alleDocs.Where(d => d.HochgeladenAm >= Von.Value).ToList();

            if (Bis.HasValue)
            {
                var bisEndOfDay = Bis.Value.Date.AddDays(1).AddTicks(-1);
                alleDocs = alleDocs.Where(d => d.HochgeladenAm <= bisEndOfDay).ToList();
            }

            // 📄 Pagination
            TotalCount = alleDocs.Count;
            DokumentListe = alleDocs.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

            // 🔗 SAS URLs generieren
            foreach (var d in DokumentListe)
            {
                if (!string.IsNullOrEmpty(d.ObjectPath))
                    d.SasUrl = _storageService.GenerateSignedUrl(d.ObjectPath, 15);
            }

            // 📊 Indexierte Dokumente
            var indexedIds = await _db.DokumentIndex.Select(x => x.DokumentId).ToListAsync();
            foreach (var d in DokumentListe)
                d.IsIndexed = indexedIds.Contains(d.Id);

            // 📜 Audit-Logs
            AuditLogs = await _auditLogDokumentService.ObtenirHistoriquePourBenutzerAsync(userId);

            // 🏢 Abteilungen (nur erlaubte)
            var alleAbteilungenDb = await _db.Abteilungen.OrderBy(a => a.Name).ToListAsync();
            AlleAbteilungen = alleAbteilungenDb
                .Where(a => User.HasAccess($"dokumente/{Firma}/{a.Name}/*"))
                .ToList();

            // 🗂 Kategorien
            AlleKategorien = alleDocs
                .Where(d => !string.IsNullOrWhiteSpace(d.Kategorie))
                .Select(d => d.Kategorie)
                .Distinct()
                .OrderBy(k => k)
                .ToList();
        }



        // Comparateur custom pour Union
        public class AbteilungNameComparer : IEqualityComparer<Abteilung>
        {
            public bool Equals(Abteilung? x, Abteilung? y)
                => string.Equals(x?.Name, y?.Name, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode(Abteilung obj)
                => obj.Name.ToLowerInvariant().GetHashCode();
        }


        private bool IsBase64String(string s)
        {
            // Mini-Schutz, damit kein Leerstring crasht
            if (string.IsNullOrWhiteSpace(s)) return false;
            Span<byte> buffer = new Span<byte>(new byte[s.Length]);
            return Convert.TryFromBase64String(
                s.Replace('-', '+').Replace('_', '/'),
                buffer,
                out int bytesParsed);
        }




        public static string ExtractRelativePathFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            // Cherche le point de départ du chemin relatif
            var marker = "/dokumente/";
            var idx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return idx >= 0 ? url.Substring(idx + 1) : null;
        }


        private string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";

            // Entferne Firebase-Storage-Prefix (wenn vorhanden)
            var bucketPrefix = $"https://storage.googleapis.com/{_storageService.Bucket}/";
            if (path.StartsWith(bucketPrefix))
                path = path.Substring(bucketPrefix.Length);

            return path.Replace("\\", "/");
        }


        public static string GetStatusBadgeClass(DokumentStatus status) => status switch
        {
            DokumentStatus.Neu => "bg-secondary",
            DokumentStatus.InBearbeitung => "bg-warning text-dark",
            DokumentStatus.Fertig => "bg-success",
            DokumentStatus.Fehlerhaft => "bg-danger",
            _ => "bg-dark"
        };


        public async Task<IActionResult> OnGetExportPdfAsync()
        {
            if (DokumentListe == null || !DokumentListe.Any())
                await LoadDokumente();

            // 📥 Charger le modèle HTML brut
            var htmlTemplatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", "pdf-template.html");

            if (!System.IO.File.Exists(htmlTemplatePath))
                return Content("❌ Template HTML introuvable : " + htmlTemplatePath);

            var htmlTemplate = await System.IO.File.ReadAllTextAsync(htmlTemplatePath, Encoding.UTF8);

            // 🧱 Générer les lignes HTML du tableau
            var sb = new StringBuilder();

            foreach (var d in DokumentListe)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{WebUtility.HtmlEncode(d.Dateiname)}</td>");
                sb.AppendLine($"<td>{WebUtility.HtmlEncode(d.Kategorie)}</td>");
                sb.AppendLine($"<td>{WebUtility.HtmlEncode(d.ApplicationUserId)}</td>");
                sb.AppendLine($"<td>{d.HochgeladenAm:dd.MM.yyyy}</td>");
                sb.AppendLine($"<td>{WebUtility.HtmlEncode(d.dtStatus.ToString())}</td>");
                sb.AppendLine("</tr>");
            }

            var html = htmlTemplate.Replace("{{rows}}", sb.ToString());

            // 🖨️ Construire le document PDF
            var doc = new HtmlToPdfDocument
            {
                GlobalSettings = new GlobalSettings
                {
                    PaperSize = PaperKind.A4,
                    Orientation = DinkToPdf.Orientation.Landscape,  // ✅ namespace explicite
                    DocumentTitle = $"Dokumentenliste - {DateTime.Now:yyyy-MM-dd}"
                },

                Objects = {
            new ObjectSettings
            {
                HtmlContent = html,
                WebSettings = { DefaultEncoding = "utf-8" }
            }
        }
            };

            var pdfBytes = _pdfConverter.Convert(doc);

            // 📄 Retourner le fichier PDF en téléchargement
            return File(pdfBytes, "application/pdf", $"Dokumente_{DateTime.Now:yyyyMMdd}.pdf");
        }

        public async Task<IActionResult> OnGetFilterByFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Partial("_DokumentRowList", new List<DmsProjeckt.Data.DmsFile>());

            var docs = await _storageService.GetDocumentsByFolderAsync(path);

            if (docs == null || !docs.Any())
                return Partial("_DokumentRowList", new List<DmsProjeckt.Data.DmsFile>());

            return Partial("_DokumentRowList", docs);
        }



        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            if (DokumentListe == null || !DokumentListe.Any())
                await LoadDokumente();

            var sb = new StringBuilder();
            sb.AppendLine("Dateiname;Kategorie;Rechnungsnummer;Rechnungsdatum;Gesamtpreis;Email;Telefon;Adresse;UIDNummer;IBAN;BIC;Bankverbindung;Status");

            foreach (var d in DokumentListe)
            {
                var meta = d.MetadatenObjekt;

                sb.AppendLine(string.Join(";", new[]
                {
            Quote(d.Dateiname),
            Quote(meta?.Kategorie ?? d.Kategorie),
            Quote(meta?.Rechnungsnummer),
            Quote(meta?.Rechnungsdatum?.ToString("yyyy-MM-dd")),
            Quote(meta?.Gesamtpreis?.ToString("F2")),
            Quote(meta?.Email),
            Quote(meta?.Telefon),
            Quote(meta?.Adresse),
            Quote(meta?.UIDNummer),
            Quote(meta?.IBAN),
            Quote(meta?.BIC),
            Quote(meta?.Bankverbindung),
            Quote(d.dtStatus.ToString())
        }));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"Dokumente_{DateTime.Now:yyyyMMdd}.csv");

            string Quote(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return "\"\"";
                return $"\"{s.Replace("\"", "\"\"")}\"";
            }
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            if (DokumentListe == null || !DokumentListe.Any())
                await LoadDokumente();

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Dokumente");

            // 🧠 Spaltenüberschriften
            var headers = new[]
            {
        "Dateiname", "Kategorie", "Rechnungsnummer", "Rechnungsdatum", "Gesamtpreis", "Email",
        "Telefon", "Adresse", "UIDNummer", "IBAN", "BIC", "Bankverbindung", "Status"
    };

            // 🧾 Kopfzeile
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 2;

            foreach (var d in DokumentListe)
            {
                var meta = d.MetadatenObjekt;

                sheet.Cells[row, 1].Value = d.Dateiname;
                sheet.Cells[row, 2].Value = meta?.Kategorie ?? d.Kategorie;
                sheet.Cells[row, 3].Value = meta?.Rechnungsnummer;
                sheet.Cells[row, 4].Value = meta?.Rechnungsdatum?.ToString("yyyy-MM-dd");

                if (double.TryParse(meta?.Gesamtpreis?.ToString(), out double preis))
                {
                    sheet.Cells[row, 5].Value = preis;
                    sheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00 €";
                }

                sheet.Cells[row, 6].Value = meta?.Email;
                sheet.Cells[row, 7].Value = meta?.Telefon;
                sheet.Cells[row, 8].Value = meta?.Adresse;
                sheet.Cells[row, 9].Value = meta?.UIDNummer;
                sheet.Cells[row, 10].Value = meta?.IBAN;
                sheet.Cells[row, 11].Value = meta?.BIC;
                sheet.Cells[row, 12].Value = meta?.Bankverbindung;
                sheet.Cells[row, 13].Value = d.dtStatus.ToString();

                // 🎨 Status-Farben
                var status = d.dtStatus.ToString();
                var statusCell = sheet.Cells[row, 13];
                statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;

                switch (status)
                {
                    case "Fertig":
                        statusCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                        break;
                    case "InBearbeitung":
                        statusCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Khaki);
                        break;
                    case "Fehlerhaft":
                        statusCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCoral);
                        break;
                    default:
                        statusCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        break;
                }

                row++;
            }

            // 📊 Tabelle + Formatierung
            var dataRange = sheet.Cells[1, 1, row - 1, headers.Length];
            var table = sheet.Tables.Add(dataRange, "DokumenteTabelle");
            table.ShowHeader = true;
            table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium2;

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();

            var fileBytes = package.GetAsByteArray();
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Dokumente_PRO_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        private async Task LoadDokumente()
        {
            DokumentListe = await _db.Dokumente.ToListAsync();
        }
        [BindProperty]
        public MoveRequest MoveRequestModel { get; set; }

        public async Task<IActionResult> OnPostDownloadFileAsync([FromBody] MoveRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Source)) return BadRequest("Missing path");
            var url = await _storageService.GetDownloadUrlAsync(req.Source);
            if (url == null) return NotFound("File not found");
            return new JsonResult(new { url });
        }

        public async Task<IActionResult> OnPostCopyPathAsync([FromBody] MoveRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Source)) return BadRequest();
            return new JsonResult(new { path = req.Source });
        }





        public async Task<IActionResult> OnPostGetPropertiesAsync([FromBody] MoveRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Source)) return BadRequest("Missing path");
            var props = await _storageService.GetPropertiesAsync(req.Source);
            if (props == null) return NotFound();
            return new JsonResult(new { success = true, properties = props });
        }

        [HttpPost]
        public async Task<IActionResult> OnPostRenameFileAsync([FromBody] RenameRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.SourcePath) || string.IsNullOrWhiteSpace(req.TargetPath))
                    return new JsonResult(new { success = false, message = "Ungültige Anfrage." });

                // Dokument aus DB holen (nach Source/ObjectPath)
                var doc = await _db.Dokumente.FirstOrDefaultAsync(d => d.ObjectPath == req.SourcePath);
                if (doc == null)
                    return new JsonResult(new { success = false, message = "Datei nicht gefunden." });


                // Dateiname und Pfad bereinigen
                var src = req.SourcePath.Trim().Replace("%2F", "/");
                var dest = req.TargetPath.Trim().Replace("%2F", "/");

                // Cloud/Storage-Umbenennung (copy + delete)
                await _storageService.CopyAsync(src, dest);
                await _storageService.DeleteAsync(src);

                // Datenbank aktualisieren
                doc.ObjectPath = dest;
                doc.Dateipfad = $"https://storage.googleapis.com/{_storageService.Bucket}/{dest}";
                doc.Dateiname = System.IO.Path.GetFileName(dest);

                await _db.SaveChangesAsync();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Fehler: " + ex.Message });
            }
        }


        public async Task<IActionResult> OnPostMoveFileAsync([FromBody] MoveRequest req)
        {
            _logger.LogInformation("📤 MoveFile handler aufgerufen. Source: {Source}, Target: {Target}", req.Source, req.Target);

            if (string.IsNullOrWhiteSpace(req.Source) || string.IsNullOrWhiteSpace(req.Target))
                return BadRequest(new { success = false, message = "Quelle oder Ziel fehlt." });

            try
            {
                // 🔹 Datei in Firebase verschieben (kopieren + löschen)
                await _storageService.CopyAsync(req.Source, req.Target);
                await _storageService.DeleteAsync(req.Source);

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doc = await _db.Dokumente
                    .Include(d => d.MetadatenObjekt)
                    .FirstOrDefaultAsync(d => d.ApplicationUserId == userId && d.ObjectPath == req.Source);

                if (doc == null)
                {
                    _logger.LogError("❗ Dokument nicht gefunden: {Source}", req.Source);
                    return new JsonResult(new { success = false, message = "Dokument nicht gefunden." });
                }

                // 🔹 Kategorie aus dem Zielpfad extrahieren
                string newKategorie;
                try
                {
                    var segments = req.Target.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (segments.Length >= 3)
                    {
                        newKategorie = segments[2];
                        _logger.LogInformation("✅ Kategorie extrahiert: {Kategorie}", newKategorie);
                    }
                    else
                    {
                        newKategorie = "Unbekannt";
                        _logger.LogWarning("⚠️ Zielpfad unvollständig: {Target}", req.Target);
                    }
                }
                catch (Exception ex)
                {
                    newKategorie = "Unbekannt";
                    _logger.LogError(ex, "❌ Fehler beim Extrahieren der Kategorie.");
                }

                // 🔹 Dokumentpfad und Basisdaten aktualisieren
                doc.ObjectPath = req.Target;
                doc.Dateipfad = $"https://storage.googleapis.com/{_storageService.Bucket}/{req.Target}";
                doc.Dateiname = Path.GetFileName(req.Target);
                doc.Kategorie = newKategorie;
                doc.HochgeladenAm = DateTime.UtcNow;
                doc.dtStatus = DokumentStatus.Neu;
                doc.IsUpdated = true;

                // 🔹 Metadaten-Objekt aktualisieren (falls vorhanden)
                if (doc.MetadatenObjekt != null)
                {
                    var meta = doc.MetadatenObjekt;

                    meta.Kategorie = newKategorie;
                    meta.Titel = meta.Titel ?? doc.Titel;
                    meta.Beschreibung = meta.Beschreibung ?? doc.Beschreibung;

                    // 🧠 Metadaten bleiben erhalten — NICHT überschreiben
                    // Nur Kategorie & Titel werden angepasst
                    _db.Metadaten.Update(meta);
                }
                else
                {
                    // 🔹 Falls keine Metadaten vorhanden, ein leeres Objekt erzeugen
                    var meta = new Metadaten
                    {
                        Kategorie = newKategorie,
                        Titel = doc.Titel ?? Path.GetFileNameWithoutExtension(doc.Dateiname),
                        Beschreibung = doc.Beschreibung ?? ""
                    };

                    doc.MetadatenObjekt = meta;
                    _db.Metadaten.Add(meta);
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("✅ Dokument erfolgreich verschoben: {Id}", doc.Id);

                return new JsonResult(new { success = true, message = "Dokument erfolgreich verschoben." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Fehler beim Verschieben der Datei.");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }


        public async Task<IActionResult> OnPostCreateExplorerAsync([FromBody] CreateExplorerRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.NewFolder))
                return new JsonResult(new { success = false, message = "Nom de dossier invalide." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var firma = user?.FirmenName?.Trim().ToLowerInvariant();

            _logger.LogInformation("CreateExplorer: Firmenname = '{Firma}'", firma);

            if (string.IsNullOrWhiteSpace(firma))
                return new JsonResult(new { success = false, message = "Firmenname non défini." });

            // ✅ Normaliser : trim, remplacer \ par /, enlever '/' et mettre en minuscules
            var cleanedPath = req.NewFolder
                .Trim()
                .Replace("\\", "/")
                .Trim('/')
                .ToLowerInvariant();

            if (cleanedPath.Contains(".."))
                return new JsonResult(new { success = false, message = "Chemin invalide." });

            var fullPath = $"dokumente/{firma}/{cleanedPath}";
            _logger.LogInformation("CreateExplorer: FullPath = {Path}", fullPath);

            try
            {
                // ✅ 1. Créer dossier dans Firebase
                await _storageService.CreateFolderAsync(fullPath);
                _logger.LogInformation("Dossier créé: {FullPath}", fullPath);

                // ✅ 2. Extraire le dernier segment (le vrai nom du dossier)
                var abteilungName = cleanedPath.Split('/').Last();

                // ✅ 3. Vérifier en base avec ToLowerInvariant pour éviter les doublons
                var exists = await _db.Abteilungen
                    .AnyAsync(a => a.Name.ToLower() == abteilungName.ToLower());

                if (!exists)
                {
                    _db.Abteilungen.Add(new Abteilung
                    {
                        Name = abteilungName  // déjà normalisé en minuscule
                    });
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Abteilung ajoutée: {Abteilung}", abteilungName);
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur création dossier {FullPath}", fullPath);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostCopyFileAsync([FromBody] MoveRequest req)
        {
            _logger.LogInformation("📄 CopyFile handler aufgerufen. Source: {Source}, Target: {Target}", req.Source, req.Target);

            if (string.IsNullOrWhiteSpace(req.Source) || string.IsNullOrWhiteSpace(req.Target))
            {
                _logger.LogError("⛔ Ungültige Anfrage – Quelle oder Ziel fehlt.");
                return BadRequest(new { success = false, message = "Quelle oder Ziel fehlt." });
            }

            try
            {
                // 1️⃣ Sicherstellen, dass TargetPath die neue Kategorie enthält
                if (!string.IsNullOrWhiteSpace(req.Kategorie))
                {
                    var fileName = Path.GetFileName(req.Target);
                    var segments = req.Target.Split('/', StringSplitOptions.RemoveEmptyEntries);

                    string firma = segments.Length > 1 ? segments[1] : "meinefirma";
                    string targetAbteilung = segments.Length > 2 ? segments[2] : "allgemein";

                    if (!req.Target.ToLower().Contains($"/{req.Kategorie.ToLower()}/"))
                    {
                        req.Target = $"dokumente/{firma}/{targetAbteilung}/{req.Kategorie.ToLower()}/{fileName}";
                        _logger.LogInformation("⚙️ TargetPath korrigiert → {Target}", req.Target);
                    }
                }

                // 2️⃣ Sicherstellen, dass Source und Target unterschiedlich sind
                if (req.Source.Equals(req.Target, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("⚠️ Quelle und Ziel sind identisch – Kopiervorgang abgebrochen.");
                    return new JsonResult(new { success = false, message = "Quelle und Ziel sind identisch." });
                }

                // 3️⃣ Datei in Firebase kopieren
                await _storageService.CopyFileOnlyAsync(req.Source, req.Target);
                _logger.LogInformation("📄 Datei kopiert (Original bleibt unverändert): {Source} → {Target}", req.Source, req.Target);

                // 4️⃣ Quelldokument isoliert abrufen
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Dokumente? sourceDoc;

                using (var isolatedDb = new ApplicationDbContext(_dbOptions))
                {
                    sourceDoc = await isolatedDb.Dokumente
                        .Include(d => d.MetadatenObjekt)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.ApplicationUserId == userId && d.ObjectPath == req.Source);
                }

                if (sourceDoc == null)
                {
                    _logger.LogWarning("❗ Quelldokument nicht gefunden: {Source}", req.Source);
                    return new JsonResult(new { success = false, message = "Quelldokument nicht gefunden." });
                }

                // 5️⃣ Abteilung & Kategorie analysieren
                var targetSegments = req.Target.Split('/', StringSplitOptions.RemoveEmptyEntries);
                string abteilungName = targetSegments.Length >= 3 ? targetSegments[2] : null;
                string newKategorie = req.Kategorie?.Trim()
                    ?? (targetSegments.Length >= 4 ? targetSegments[3] : (sourceDoc.Kategorie ?? "allgemein"));

                int? abteilungId = null;
                if (!string.IsNullOrWhiteSpace(abteilungName))
                {
                    var abteilung = await _db.Abteilungen.FirstOrDefaultAsync(a => a.Name.ToLower() == abteilungName.ToLower());
                    abteilungId = abteilung?.Id;
                }

                // 🧹 Détacher complètement l’original
                sourceDoc.MetadatenObjekt = null;
                sourceDoc.MetadatenId = null;
                _db.Entry(sourceDoc).State = EntityState.Detached;

                // 6️⃣ Neues Metadatenobjekt erzeugen (clone sans lien avec original)
                Metadaten? newMeta = null;
                if (sourceDoc.MetadatenObjekt == null)
                {
                    _logger.LogWarning("⚠️ Quelldokument hat kein MetadatenObjekt – Metadaten werden leer erstellt.");
                }

                var meta = sourceDoc.MetadatenObjekt ?? new Metadaten();
                newMeta = new Metadaten
                {
                    Titel = meta.Titel,
                    Beschreibung = meta.Beschreibung ?? $"Kopie von '{meta.Titel ?? sourceDoc.Dateiname}'",
                    Kategorie = newKategorie,
                    Stichworte = meta.Stichworte ?? meta.PdfSchluesselwoerter,
                    Rechnungsnummer = meta.Rechnungsnummer,
                    Kundennummer = meta.Kundennummer,
                    Rechnungsbetrag = meta.Rechnungsbetrag,
                    Nettobetrag = meta.Nettobetrag,
                    Steuerbetrag = meta.Steuerbetrag,
                    Gesamtpreis = meta.Gesamtpreis,
                    Rechnungsdatum = meta.Rechnungsdatum,
                    Lieferdatum = meta.Lieferdatum,
                    Faelligkeitsdatum = meta.Faelligkeitsdatum,
                    Zahlungsbedingungen = meta.Zahlungsbedingungen,
                    Lieferart = meta.Lieferart,
                    ArtikelAnzahl = meta.ArtikelAnzahl,
                    SteuerNr = meta.SteuerNr,
                    UIDNummer = meta.UIDNummer,
                    Email = meta.Email,
                    Telefon = meta.Telefon,
                    Telefax = meta.Telefax,
                    IBAN = meta.IBAN,
                    BIC = meta.BIC,
                    Bankverbindung = meta.Bankverbindung,
                    Adresse = meta.Adresse,
                    AbsenderAdresse = meta.AbsenderAdresse,
                    AnsprechPartner = meta.AnsprechPartner,
                    Zeitraum = meta.Zeitraum,
                    PdfAutor = meta.PdfAutor,
                    PdfBetreff = meta.PdfBetreff,
                    PdfSchluesselwoerter = meta.PdfSchluesselwoerter,
                    Website = meta.Website,
                    OCRText = meta.OCRText,
                    DokumentId = null // 🚫 keine Verknüpfung mit dem Original
                };

                await _db.Metadaten.AddAsync(newMeta);
                await _db.SaveChangesAsync();

                // 7️⃣ Neues Dokument anlegen
                var newDoc = new Dokumente
                {
                    Id = Guid.NewGuid(),
                    ApplicationUserId = userId,
                    KundeId = sourceDoc.KundeId,
                    Titel = sourceDoc.Titel,
                    Dateiname = Path.GetFileName(req.Target),
                    Dateipfad = $"https://storage.googleapis.com/{_storageService.Bucket}/{req.Target}",
                    ObjectPath = req.Target,
                    AbteilungId = abteilungId,
                    Kategorie = newKategorie,
                    HochgeladenAm = DateTime.UtcNow,
                    dtStatus = DokumentStatus.Fertig,
                    Beschreibung = sourceDoc.Beschreibung,
                    IsVersion = false,
                    IsUpdated = true,
                    MetadatenObjekt = newMeta,
                    MetadatenId = null
                };

                await _db.Dokumente.AddAsync(newDoc);
                await _db.SaveChangesAsync();

                // 🔗 Verknüpfung synchronisieren
                newMeta.DokumentId = newDoc.Id;
                newDoc.MetadatenId = newMeta.Id;
                _db.Update(newMeta);
                _db.Update(newDoc);
                await _db.SaveChangesAsync();

                _db.ChangeTracker.Clear(); // 🧹 alles clean

                _logger.LogInformation("✅ Dokument erfolgreich kopiert → Kategorie: {Kat} | Pfad: {Pfad}", newKategorie, req.Target);

                // 8️⃣ JSON Rückgabe
                return new JsonResult(new
                {
                    success = true,
                    message = "✅ Dokument erfolgreich kopiert.",
                    newFile = new
                    {
                        id = newDoc.Id,
                        name = newDoc.Dateiname,
                        abteilung = abteilungName,
                        kategorie = newKategorie,
                        path = newDoc.ObjectPath,
                        uploaded = newDoc.HochgeladenAm.ToString("dd.MM.yy HH:mm"),
                        status = newDoc.dtStatus.ToString(),
                        beschreibung = newDoc.Beschreibung ?? "",
                        titel = newMeta?.Titel ?? newDoc.Titel ?? Path.GetFileNameWithoutExtension(newDoc.Dateiname)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Fehler beim Kopieren der Datei.");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }



        [HttpPost]
        public async Task<IActionResult> OnPostGetBlobPropertiesAsync([FromBody] MoveRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Source))
                return BadRequest(new { success = false, message = "Chemin invalide." });

            try
            {
                var props = await _storageService.GetPropertiesAsync(req.Source);
                if (props == null)
                    return NotFound(new { success = false, message = "Fichier introuvable." });

                return new JsonResult(new
                {
                    success = true,
                    properties = new
                    {
                        nom = props.GetValueOrDefault("Nom"),
                        taille = props.GetValueOrDefault("Taille"),
                        type = props.GetValueOrDefault("ContentType"),
                        créé = props.GetValueOrDefault("Créé"),
                        modifié = props.GetValueOrDefault("Modifié"),
                        lien = props.GetValueOrDefault("Lien"),
                        accessTier = props.GetValueOrDefault("Tier")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération des propriétés.");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }



        private string ExtraireKategorieDepuisTarget(string targetPath)
        {
            if (string.IsNullOrWhiteSpace(targetPath)) return "divers";

            var parts = targetPath.Split('/');
            return parts.Length >= 3 ? parts[2] : "divers"; // Ex: dokumente/microplus/**<categorie>**
        }
        private static string EnsureTrailingSlash(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "";

            return path.EndsWith("/") ? path : path + "/";
        }


        public async Task<JsonResult> OnPostDeleteFolderAsync([FromBody] DeleteFolderRequest req)
        {
            Console.WriteLine($"📁 Reçu folderPath: '{req.folderPath}'");

            if (string.IsNullOrWhiteSpace(req.folderPath))
                return new JsonResult(new { success = false, message = "Pfad ist leer." });

            var folderPrefix = EnsureTrailingSlash(req.folderPath);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                // 🔥 1. Supprimer depuis Firebase Storage
                await _storageService.DeleteFolderAsync(folderPrefix);

                // 📄 2. Supprimer les documents liés depuis la base de données
                var docs = await _db.Dokumente
                    .Where(d => d.ApplicationUserId == userId && d.ObjectPath.StartsWith(folderPrefix))
                    .ToListAsync();
                _db.Dokumente.RemoveRange(docs);

                // 🕓 3. Supprimer les versions de documents
                var versions = await _db.DokumentVersionen
                    .Where(v => v.ApplicationUserId == userId && v.Dateipfad.StartsWith(folderPrefix))
                    .ToListAsync();
                _db.DokumentVersionen.RemoveRange(versions);

                await _db.SaveChangesAsync();

                Console.WriteLine($"🧹 {docs.Count} Dokumente supprimés.");
                Console.WriteLine($"🧹 {versions.Count} Versionen supprimées.");

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Erreur DeleteFolderAsync : " + ex.Message);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public static string ExtraireCheminRelatif(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return null;

            int idx = fullPath.IndexOf("/dokumente/");
            return (idx >= 0) ? fullPath.Substring(idx + 1) : null;
        }
        public async Task CopyFolderMetaAsync(string sourceFolder, string targetFolder, string? newName = null)
        {
            // 1️⃣ Zielbasis vorbereiten
            var baseTarget = targetFolder;
            if (!baseTarget.EndsWith("/")) baseTarget += "/";
            if (!string.IsNullOrWhiteSpace(newName))
                baseTarget += newName.TrimEnd('/') + "/";

            // 2️⃣ Alle Dokumente im Quellordner laden (inkl. Metadaten)
            var docs = await _db.Dokumente
                .Include(d => d.MetadatenObjekt)
                .Where(d => d.ObjectPath.StartsWith(sourceFolder))
                .ToListAsync();

            if (docs.Count == 0)
            {
                _logger.LogWarning("⚠️ Keine Dokumente im Quellordner {Folder} gefunden.", sourceFolder);
                return;
            }

            // 3️⃣ Mapping AltId -> NeuId für Versionen-Zuordnung
            var idMapping = new Dictionary<Guid, Guid>();

            foreach (var doc in docs)
            {
                // 🔹 Neuen Speicherpfad berechnen
                var relativePath = doc.ObjectPath.Substring(sourceFolder.Length).TrimStart('/');
                var newObjectPath = baseTarget + relativePath;
                var newId = Guid.NewGuid();
                idMapping[doc.Id] = newId;

                // 🔹 Neues Metadatenobjekt klonen
                Metadaten? newMeta = null;
                if (doc.MetadatenObjekt != null)
                {
                    var meta = doc.MetadatenObjekt;
                    newMeta = new Metadaten
                    {
                        Titel = meta.Titel,
                        Beschreibung = meta.Beschreibung,
                        Kategorie = meta.Kategorie,
                        Stichworte = meta.Stichworte,
                        Rechnungsnummer = meta.Rechnungsnummer,
                        Kundennummer = meta.Kundennummer,
                        Rechnungsbetrag = meta.Rechnungsbetrag,
                        Nettobetrag = meta.Nettobetrag,
                        Steuerbetrag = meta.Steuerbetrag,
                        Gesamtpreis = meta.Gesamtpreis,
                        Rechnungsdatum = meta.Rechnungsdatum,
                        Lieferdatum = meta.Lieferdatum,
                        Faelligkeitsdatum = meta.Faelligkeitsdatum,
                        Zahlungsbedingungen = meta.Zahlungsbedingungen,
                        Lieferart = meta.Lieferart,
                        ArtikelAnzahl = meta.ArtikelAnzahl,
                        SteuerNr = meta.SteuerNr,
                        UIDNummer = meta.UIDNummer,
                        Email = meta.Email,
                        Telefon = meta.Telefon,
                        Telefax = meta.Telefax,
                        IBAN = meta.IBAN,
                        BIC = meta.BIC,
                        Bankverbindung = meta.Bankverbindung,
                        Adresse = meta.Adresse,
                        AbsenderAdresse = meta.AbsenderAdresse,
                        AnsprechPartner = meta.AnsprechPartner,
                        Zeitraum = meta.Zeitraum,
                        PdfAutor = meta.PdfAutor,
                        PdfBetreff = meta.PdfBetreff,
                        PdfSchluesselwoerter = meta.PdfSchluesselwoerter,
                        Website = meta.Website,
                        OCRText = meta.OCRText
                    };
                    _db.Metadaten.Add(newMeta);
                }

                // 🔹 Neues Dokument erstellen
                var newDoc = new Dokumente
                {
                    Id = newId,
                    ApplicationUserId = doc.ApplicationUserId,
                    KundeId = doc.KundeId,
                    Kategorie = doc.Kategorie,
                    Beschreibung = doc.Beschreibung,
                    Titel = doc.Titel,
                    ObjectPath = newObjectPath,
                    Dateipfad = $"https://storage.googleapis.com/{_storageService.Bucket}/{newObjectPath}",
                    Dateiname = Path.GetFileName(newObjectPath),
                    HochgeladenAm = DateTime.UtcNow,
                    dtStatus = doc.dtStatus,
                    IsIndexed = doc.IsIndexed,
                    IsVersion = doc.IsVersion,
                    OriginalId = doc.OriginalId,
                    EstSigne = doc.EstSigne,
                    AbteilungId = doc.AbteilungId,
                    IsUpdated = true,
                    MetadatenObjekt = newMeta // 🔗 Verknüpfung herstellen
                };

                _db.Dokumente.Add(newDoc);
            }

            // 4️⃣ Versionen kopieren
            var versionen = await _db.DokumentVersionen
                .Where(v => docs.Select(d => d.Id).Contains(v.DokumentId))
                .ToListAsync();

            foreach (var ver in versionen)
            {
                if (!idMapping.TryGetValue(ver.DokumentId, out var newDocId))
                    continue; // sollte nicht vorkommen

                // 🔹 Pfad für die neue Version bestimmen
                var relativePath = ver.Dateipfad.StartsWith(sourceFolder)
                    ? ver.Dateipfad.Substring(sourceFolder.Length).TrimStart('/')
                    : ver.Dateipfad;

                var newVersionPath = baseTarget + relativePath;

                var newVer = new DokumentVersionen
                {
                    DokumentId = newDocId,
                    ApplicationUserId = ver.ApplicationUserId,
                    Dateiname = ver.Dateiname,
                    Dateipfad = newVersionPath,
                    HochgeladenAm = DateTime.UtcNow,
                    EstSigne = ver.EstSigne
                };
                _db.DokumentVersionen.Add(newVer);
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("✅ {Count} Dokumente mit Metadaten erfolgreich kopiert nach {Target}.", docs.Count, targetFolder);
        }



        public async Task<JsonResult> OnPostCopyFolderAsync([FromBody] CopyRequest req)
        {
            try
            {
                // 1. Ordner im Storage kopieren
                await _storageService.CopyFolderAsync(req.SourcePath, req.TargetPath);

                // 2. Metadaten/Datenbank-Einträge kopieren
                await CopyFolderMetaAsync(req.SourcePath, req.TargetPath, req.NewName);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }


        public async Task<JsonResult> OnPostMoveFolderAsync([FromBody] CopyRequest req)
        {
            try
            {
                await _storageService.MoveFolderAsync(req.SourcePath, req.TargetPath);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostArchiveFileAsync([FromBody] ArchiveRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Source))
                return BadRequest(new { success = false, message = "Pfad fehlt." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                var query = _db.Dokumente
                    .Include(d => d.Abteilung)
                    .Include(d => d.MetadatenObjekt)
                    .Include(d => d.ApplicationUser)
                    .AsQueryable();

                if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                    query = query.Where(d => d.ApplicationUserId == userId);

                var doc = await query.FirstOrDefaultAsync(d => d.ObjectPath == req.Source);
                if (doc == null)
                    return new JsonResult(new { success = false, message = "❌ Dokument nicht gefunden oder keine Berechtigung." });

                // ✅ Récupération fiable des infos depuis la DB
                var firma = doc.ApplicationUser?.FirmenName?.ToLowerInvariant() ?? "berba";
                var abteilungName = doc.Abteilung?.Name?.ToLowerInvariant() ?? "allgemein";
                var kategorie = doc.Kategorie?.ToLowerInvariant() ?? "allgemein";

                _logger.LogInformation("📦 Archivierung gestartet für {Pfad} ({Firma}/{Abteilung}/{Kategorie})", doc.ObjectPath, firma, abteilungName, kategorie);

                // 🔧 Nouveau chemin d’archivage
                var (newPath, abteilungId) = DocumentPathHelper.BuildFinalPath(
                    firma: firma,
                    fileName: $"{Path.GetFileNameWithoutExtension(doc.Dateiname)}_archiviert_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(doc.Dateiname)}",
                    kategorie: "Archiv",
                    abteilungId: doc.AbteilungId,
                    abteilungName: abteilungName
                );

                var metaEntity = await _db.Metadaten.FirstOrDefaultAsync(m => m.Id == doc.MetadatenId);

                // 🧩 Cas 1 : fichier normal (non chunké)
                if (!doc.IsChunked)
                {
                    using var stream = new MemoryStream();
                    await _storageService.DownloadToStreamAsync(req.Source, stream);
                    stream.Position = 0;

                    await _storageService.UploadWithMetadataAsync(
                        stream,
                        newPath,
                        "application/pdf",
                        metaEntity
                    );

                    await _storageService.DeleteAsync(req.Source);

                    doc.ObjectPath = newPath;
                    doc.Dateipfad = $"https://storage.googleapis.com/{_storageService.Bucket}/{newPath}";
                }
                else
                {
                    // 🧩 Cas 2 : fichier chunké
                    string chunkBase = doc.ObjectPath.Replace("chunked://", "").Trim('/');
                    string originalChunksPath = $"dokumente/{firma}/{abteilungName}/{kategorie}/chunks/{chunkBase}";
                    string archiveChunksPath = $"dokumente/{firma}/{abteilungName}/archiv/chunks/{chunkBase}";

                    _logger.LogInformation("🧩 Chunk-Archivierung: {Source} → {Target}", originalChunksPath, archiveChunksPath);

                    // 📥 Liste des chunks à déplacer
                    await foreach (var obj in _storageService.ListObjectsAsync(originalChunksPath + "/"))
                    {
                        try
                        {
                            string newChunkPath = obj.Name.Replace(originalChunksPath, archiveChunksPath);
                            await _storageService.CopyAsync(obj.Name, newChunkPath);

                            bool deleted = await _storageService.DeleteAsync(obj.Name);
                            if (!deleted)
                                _logger.LogWarning("⚠️ Chunk konnte nicht gelöscht werden: {Chunk}", obj.Name);
                            else
                                _logger.LogInformation("📦 Chunk verschoben: {Old} → {New}", obj.Name, newChunkPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("⚠️ Chunk-Archivierung fehlgeschlagen für {Chunk}: {Msg}", obj.Name, ex.Message);
                        }
                    }

                    // 🔁 Mise à jour du chemin logique (toujours chunked://)
                    doc.ObjectPath = $"chunked://{chunkBase}";
                    doc.Kategorie = "Archiv";
                    doc.Dateipfad = $"https://storage.googleapis.com/{_storageService.Bucket}/{archiveChunksPath}/chunk_0.bin"; // premier chunk pour prévisualisation
                }

                // 🗃️ Archiv-Eintrag erstellen
                var archivEntity = new Archive
                {
                    DokumentId = doc.Id,
                    ArchivName = doc.Dateiname,
                    ArchivPfad = doc.Dateipfad,
                    FileSizeBytes = doc.FileSizeBytes,
                    ArchivDatum = DateTime.UtcNow,
                    BenutzerId = userId,
                    Grund = "Manuelle Archivierung",
                    MetadatenJson = JsonSerializer.Serialize(metaEntity),
                    IstAktiv = false
                };

                _db.Archive.Add(archivEntity);

                // 🧾 Mise à jour du document
                doc.Kategorie = "Archiv";
                doc.DokumentStatus = DmsProjeckt.Data.Status.Archiviert;
                doc.dtStatus = DmsProjeckt.Data.DokumentStatus.Fertig;
                doc.IsIndexed = false;
                doc.IsUpdated = true;

                await _db.SaveChangesAsync();

                _logger.LogInformation("✅ Dokument erfolgreich archiviert (normal oder chunked).");
                return new JsonResult(new { success = true, message = "Dokument erfolgreich archiviert." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Archivierung fehlgeschlagen für {Source}", req.Source);
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }


        public async Task<IActionResult> OnPostToggleFavoritAsync([FromBody] FavoriteRequest req)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (req == null || req.DokumentId == Guid.Empty)
                return new JsonResult(new { success = false, message = "Ungültige Anfrage." });
            if (string.IsNullOrEmpty(userId))
                return new JsonResult(new { success = true, message = "User ist leer" });
            var fav = await _db.UserFavoritDokumente
                .FirstOrDefaultAsync(f => f.ApplicationUserId == userId && f.DokumentId == req.DokumentId);

            bool isNowFavorit;

            if (fav == null)
            {
                _db.UserFavoritDokumente.Add(new UserFavoritDokument
                {
                    ApplicationUserId = userId,
                    DokumentId = req.DokumentId,
                });
                isNowFavorit = true;
            }
            else
            {
                _db.UserFavoritDokumente.Remove(fav);
                isNowFavorit = false;
            }

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true, isFavorit = isNowFavorit });
        }

        public async Task<IActionResult> OnGetGetUsersFromCompanyAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            if (currentUser == null || string.IsNullOrEmpty(currentUser.FirmenName))
                return new JsonResult(new { error = "Kein Firmenname gefunden." });

            // Hole alle User mit gleichem Firmennamen (und schließe den aktuellen User aus)
            var users = await _db.Users
                .Where(u => u.FirmenName == currentUser.FirmenName && u.Id != currentUser.Id)
                .Select(u => new { u.Id, Name = u.Vorname + " " + u.Nachname, u.Email })
                .ToListAsync();

            return new JsonResult(users);
        }

        // Diese Methode wird via POST aufgerufen, wenn Dateien geteilt werden sollen
        public async Task<IActionResult> OnPostShareDocumentAsync([FromBody] ShareDocumentInput input)
        {
            var byUserId = _userManager.GetUserId(User);
            if (input == null) return BadRequest("Input ist NULL");
            if (input.UserIds == null) return BadRequest("UserIds ist NULL");
            if (input.DokumentId == null) return BadRequest("DokumentId ist NULL oder leer!");

            var dokument = await _db.Dokumente.FindAsync(input.DokumentId);
            if (dokument == null) return NotFound($"Dokument mit ID '{input.DokumentId}' nicht gefunden.");

            var user = await _db.Users.FindAsync(byUserId);
            if (user == null) return NotFound($"User mit ID '{byUserId}' nicht gefunden.");

            foreach (var userId in input.UserIds)
            {
                var alreadyExists = await _db.UserSharedDocuments
                    .AnyAsync(x => x.DokumentId == input.DokumentId && x.SharedToUserId == userId);

                if (!alreadyExists)
                {
                    _db.UserSharedDocuments.Add(new UserSharedDocument
                    {
                        DokumentId = input.DokumentId,
                        SharedToUserId = userId,
                        SharedAt = DateTime.Now,
                        SharedByUserId = byUserId
                    });
                }

                var notificationType = await _db.NotificationTypes
                    .FirstOrDefaultAsync(n => n.Name == "Doc shared");

                if (notificationType == null) continue; // Oder Fehler werfen

                var setting = await _db.UserNotificationSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.NotificationTypeId == notificationType.Id);

                if (setting == null || setting.Enabled)
                {
                    var notification = new Notification
                    {
                        Title = "Dokument geteilt",
                        Content = $"Dokument \"{dokument.Titel}\" wurde von {user.Vorname} {user.Nachname} mit dir geteilt.",
                        CreatedAt = DateTime.UtcNow,
                        NotificationTypeId = notificationType.Id
                    };
                    _db.Notifications.Add(notification);
                    await _db.SaveChangesAsync();

                    var userNotification = new UserNotification
                    {
                        UserId = userId,
                        NotificationId = notification.Id,
                        IsRead = false,
                        ReceivedAt = DateTime.UtcNow
                    };
                    _db.UserNotifications.Add(userNotification);
                    await _db.SaveChangesAsync();
                }
            }

            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<JsonResult> OnGetGetCommentsAsync(Guid docId)
        {
            var comments = await _db.Kommentare
                .Where(c => c.DokumentId == docId)
                .OrderBy(c => c.ErstelltAm)
                .ToListAsync();

            var html = string.Join("<br/>", comments.Select(c =>
                $"<small>{c.ErstelltAm:dd.MM.yyyy HH:mm} – {c.Text}</small>"));

            return new JsonResult(new { html });
        }

        [IgnoreAntiforgeryToken] // pour ignorer les erreurs CSRF lors du test, à sécuriser ensuite
        public async Task<JsonResult> OnPostAddCommentAsync(Guid dokumentId, string text)
        {
            _db.Kommentare.Add(new Kommentare
            {
                DokumentId = dokumentId,
                Text = text,
                ErstelltAm = DateTime.UtcNow,
                ApplicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                BenutzerId = User.Identity?.Name ?? "Unbekannt"
            });

            await _db.SaveChangesAsync();
            return new JsonResult(new { ok = true });
        }
        public async Task<JsonResult> OnPostDeleteFile([FromBody] DeleteFileRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.source))
                return new JsonResult(new { success = false, message = "Pfad ist leer." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                // 🔥 1. Datei aus Storage löschen (z.B. Firebase, Azure, etc)
                await _storageService.DeleteFileAsync(req.source);

                // 📄 2. Dokument aus Datenbank entfernen
                var dokument = await _db.Dokumente
                    .FirstOrDefaultAsync(d => d.ApplicationUserId == userId && d.ObjectPath == req.source);

                if (dokument != null)
                    _db.Dokumente.Remove(dokument);

                // 🕓 3. Alle Versionen dieses Dokuments löschen
                var versionen = await _db.DokumentVersionen
                    .Where(v => v.ApplicationUserId == userId && v.Dateipfad == req.source)
                    .ToListAsync();
                _db.DokumentVersionen.RemoveRange(versionen);

                await _db.SaveChangesAsync();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostRenameFolderAsync([FromBody] RenameFolderRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.SourcePath) || string.IsNullOrWhiteSpace(req.TargetPath))
                return new JsonResult(new { success = false, message = "Ungültige Anfrage." });

            try
            {
                // 1. FOLDER COPY (kopiert alle Dateien und Unterordner)
                await _storageService.CopyFolderAsync(req.SourcePath, req.TargetPath);

                // 2. FOLDER DELETE (löscht den alten Ordner samt Inhalt)
                await _storageService.DeleteFolderAsync(req.SourcePath);

                // 3. Optional: In DB alle Pfade anpassen (falls du Folderpfade in DB hast)
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var docsToUpdate = await _db.Dokumente
                    .Where(d => d.ApplicationUserId == userId && d.ObjectPath.StartsWith(req.SourcePath))
                    .ToListAsync();

                foreach (var doc in docsToUpdate)
                {
                    doc.ObjectPath = req.TargetPath + doc.ObjectPath.Substring(req.SourcePath.Length);
                    doc.Dateipfad = $"https://storage.googleapis.com/{_storageService.Bucket}/{doc.ObjectPath}";
                }

                await _db.SaveChangesAsync();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
        // ✅ Benutzer aus gleicher Firma holen (für Checkboxen im Modal)
        public async Task<IActionResult> OnGetGetSignableUsersAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            if (currentUser == null || string.IsNullOrEmpty(currentUser.FirmenName))
                return new JsonResult(new { error = "Kein Firmenname gefunden." });

            // Hole alle User mit gleichem Firmennamen (außer mich selbst)
            var users = await _db.Users
                .Where(u => u.FirmenName == currentUser.FirmenName && u.Id != currentUser.Id)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = (u.Vorname + " " + u.Nachname).Trim(),
                    Email = u.Email ?? ""
                })
                .ToListAsync();

            return new JsonResult(users);
        }

        // ✅ Signaturanfrage erstellen
        [HttpPost]
        public async Task<IActionResult> OnPostRequestSignatureAsync([FromBody] SignatureRequestDto dto)
        {
            var byUserId = _userManager.GetUserId(User);

            if (dto.FileId == null || dto.UserIds == null || !dto.UserIds.Any())
                return new JsonResult(new { success = false, message = "Ungültige Anfrage." });

            foreach (var userId in dto.UserIds)
            {
                var request = new SignatureRequest
                {
                    FileId = dto.FileId,
                    RequestedUserId = userId,
                    RequestedByUserId = byUserId,
                    RequestedAt = DateTime.UtcNow,
                    Status = "Pending"
                };
                _db.SignatureRequests.Add(request);

                var notificationType = await _db.NotificationTypes
                    .FirstOrDefaultAsync(n => n.Name == "SignRq");

                var setting = await _db.UserNotificationSettings
                    .FirstOrDefaultAsync(n => n.UserId == request.RequestedUserId && n.NotificationTypeId == notificationType.Id);

                var notificationTypeEmail = await _db.NotificationTypes
                    .FirstOrDefaultAsync(n => n.Name == "SignRqEm");

                var settingEmail = await _db.UserNotificationSettings
                    .FirstOrDefaultAsync(n => n.UserId == request.RequestedUserId && n.NotificationTypeId == notificationTypeEmail.Id);


                var doc = await _db.Dokumente
                    .FirstOrDefaultAsync(d => d.Id == dto.FileId);
                var byUser = await _userManager.FindByIdAsync(byUserId);
                if (setting == null || setting.Enabled)
                {

                    var notification = new Notification
                    {
                        Title = "Dokument signieren",
                        Content = $"Für das Dokument \"{doc.Dateiname}\" wurde von {byUser.Vorname} {byUser.Nachname} die Anfrage gestellt, es von Ihnen zu signieren.",
                        CreatedAt = DateTime.UtcNow,
                        NotificationTypeId = notificationType.Id,
                        ActionLink = $"/Signieren"
                    };
                    _db.Notifications.Add(notification);
                    await _db.SaveChangesAsync();

                    var userNotification = new UserNotification
                    {
                        UserId = request.RequestedUserId,
                        NotificationId = notification.Id,
                        IsRead = false,
                        ReceivedAt = DateTime.UtcNow
                    };
                    _db.UserNotifications.Add(userNotification);
                    await _db.SaveChangesAsync();

                }
                if (settingEmail == null || settingEmail.Enabled)
                {
                    var userTo = await _db.Users.FindAsync(request.RequestedUserId);

                    string subject = "Dokument signiert";
                    string body = $@"
                <p>Hallo {userTo.Vorname},</p>
                <p>Für das Dokument <b>""{doc.Dateiname}""</b> wurde von <b>{byUser.Vorname} {byUser.Nachname}</b> die Anfrage gestellt, es von Ihnen zu signieren.</p>
                            < p >< a href = '' > Ansehen </ a ></ p >
            
                            < p > Viele Grüße,< br /> Dein Team </ p > ";

                    await _emailService.SendEmailAsync(userTo.Email, subject, body);
                }
            }

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true, message = "Signaturanfrage erstellt." });
        }
        [HttpGet]
        public async Task<IActionResult> ChunkedPreview(Guid id)
        {
            var doc = await _db.Dokumente
                .Include(d => d.Abteilung)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null)
                return NotFound("Dokument nicht gefunden.");

            if (!doc.IsChunked)
                return RedirectToAction("Preview", new { id });

            // ⚙️ Reconstruit le fichier temporairement depuis les chunks
            var reconstructedPath = await _chunkService.ReconstructFileFromFirebaseAsync(doc.Id);


            if (string.IsNullOrWhiteSpace(reconstructedPath) || !System.IO.File.Exists(reconstructedPath))
                return Content("❌ Chunked-Datei konnte nicht rekonstruiert werden.");

            var fileStream = System.IO.File.OpenRead(reconstructedPath);
            return File(fileStream, "application/pdf", doc.Dateiname ?? "chunked_document.pdf");
        }


    }
    public class RenameFolderRequest
    {
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
    }
    public class RenameRequest
    {
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public string Passwort { get; set; }
    }

    public class CopyRequest
    {
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public string NewName { get; set; } // Optional, falls du den Ordner umbenennen willst
    }
    public class FolderItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
    }
    public class DeleteFolderRequest
    {
        [JsonPropertyName("folderPath")]
        public string folderPath { get; set; }
    }
    public class ArchiveRequest
    {
        public string Source { get; set; }

    }

    public class FavoriteRequest
    {
        public Guid DokumentId { get; set; }
    }
    public class ShareDocumentInput
    {
        public Guid DokumentId { get; set; }
        public List<string> UserIds { get; set; }
    }
    public class DeleteFileRequest
    {
        public string source { get; set; }
    }
    public class DokumentModel
    {
        public string Kategorie { get; set; }
        public string ObjectPath { get; set; }
        public string Status { get; set; }
        public string BenutzerId { get; set; }
        public DateTime HochgeladenAm { get; set; }
        public string Dateiname { get; set; }
        // Weitere Properties nach Bedarf
    }
    public class CreateExplorerRequest
    {
        public string NewFolder { get; set; }
    }


    public class MoveRequest
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public int? AbteilungId { get; set; }  // 🔹 ajouté
        public string Kategorie { get; set; }  // 🔹 ajouté
    }
    public class CopyFileRequest
    {
        public Guid DokumentId { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public int AbteilungId { get; set; }
        public string Kategorie { get; set; }
    }
    public class ExplorerFolder
    {
        public string Name { get; set; }          // Nom du dossier
        public string Path { get; set; }          // Chemin complet
        public List<DmsFile> Files { get; set; } = new();  // Fichiers dans ce dossier
        public List<ExplorerFolder> SubFolders { get; set; } = new(); // Sous-dossiers si besoin
        public bool IsAbteilung { get; set; }

        // 🔹 Permet d’aplatir l’arborescence facilement
        public IEnumerable<ExplorerFolder> Flatten()
        {
            yield return this;
            foreach (var sub in SubFolders.SelectMany(s => s.Flatten()))
                yield return sub;
        }

    }
    public static class ClaimsExtensions
    {
        public static bool HasAccess(this ClaimsPrincipal user, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // 🔑 Admins dürfen immer alles
            if (user.IsInRole("Admin") || user.IsInRole("SuperAdmin"))
                return true;

            // Normalisieren
            path = path.Trim().Replace("\\", "/");

            var claims = user.FindAll("FolderAccess").Select(c => c.Value);

            foreach (var claim in claims)
            {
                var normalizedClaim = claim.Trim().Replace("\\", "/");

                if (normalizedClaim.EndsWith("/*", StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = normalizedClaim[..^2]; // alles außer "/*"
                    
                    if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                else
                {
                    if (string.Equals(path, normalizedClaim, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                Console.WriteLine("Rechte", normalizedClaim);
            }

            return false;
        }
    }




    // Models/UserDto.cs
    public class UserDto
    {
        public string Id { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
    }

    // Models/SignatureRequestDto.cs
    public class SignatureRequestDto
    {
        public Guid FileId { get; set; } 
        public List<string> UserIds { get; set; } = new();
    }

}
