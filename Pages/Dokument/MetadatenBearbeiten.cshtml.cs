using DmsProjeckt.Data;
using DmsProjeckt.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Pages.Dokument
{
    public class MetadatenBearbeitenModel : PageModel
    {

        private readonly ApplicationDbContext _db;
        private readonly ILogger<MetadatenBearbeitenModel> _logger;
        private readonly FirebaseStorageService _firebaseStorageService ;

        public MetadatenBearbeitenModel(ApplicationDbContext db, ILogger<MetadatenBearbeitenModel> logger, FirebaseStorageService firebaseStorageService)
        {
            _db = db;
            _logger = logger;
            _firebaseStorageService = firebaseStorageService;
        }

        [BindProperty]
        public Dokumente Dokument { get; set; }
        [BindProperty]
        public string PendingSignaturesJson { get; set; }
        [BindProperty]
        public Metadaten Metadaten { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            // 🧩 1️⃣ Dokument + Abteilung + Metadaten laden
            var dokument = await _db.Dokumente
                .Include(d => d.Abteilung)
                .Include(d => d.MetadatenObjekt)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dokument == null)
                return NotFound();

            Dokument = dokument;

            // 🧠 2️⃣ Wenn Metadaten noch fehlen → leeres Objekt anlegen
            if (dokument.MetadatenObjekt == null)
            {
                var neueMetadaten = new Metadaten
                {
                    Titel = dokument.Titel,
                    Kategorie = dokument.Kategorie,
                    Beschreibung = dokument.Beschreibung
                };

                _db.Metadaten.Add(neueMetadaten);
                await _db.SaveChangesAsync();

                dokument.MetadatenId = neueMetadaten.Id;
                _db.Dokumente.Update(dokument);
                await _db.SaveChangesAsync();

                dokument.MetadatenObjekt = neueMetadaten;

                Console.WriteLine($"🆕 Neuer Metadatensatz erstellt (Id={neueMetadaten.Id}) für Dokument {dokument.Id}");
            }

            // 🧾 3️⃣ Jetzt haben wir sicher ein Metadaten-Objekt → an View übergeben
            var meta = dokument.MetadatenObjekt;

            // Optional: direkt als Property für Razor
            ViewData["Metadaten"] = meta;

            // 🧩 Debug
            Console.WriteLine($"📄 Dokument geladen: {dokument.Dateiname} ({dokument.Id})");
            Console.WriteLine($"🧠 Metadaten: {meta.Id} | {meta.Titel ?? "Kein Titel"} | {meta.Kategorie ?? "Unbekannt"}");

            return Page();
        }


        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            _logger.LogInformation("🔎 OnPostAsync gestartet für Dokument {Id}", id);

            // 1️⃣ Dokument + Abhängigkeiten laden
            var original = await _db.Dokumente
                .Include(d => d.Abteilung)
                .Include(d => d.MetadatenObjekt)
                .Include(d => d.ApplicationUser)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (original == null)
            {
                _logger.LogWarning("❌ Dokument mit Id {Id} nicht gefunden", id);
                return NotFound();
            }

            // 2️⃣ Metadatenobjekt sicherstellen
            var meta = original.MetadatenObjekt;
            if (meta == null)
            {
                meta = new Metadaten
                {
                    Titel = Dokument.Titel,
                    Kategorie = Dokument.Kategorie,
                    Beschreibung = Dokument.Beschreibung
                };
                _db.Metadaten.Add(meta);
                await _db.SaveChangesAsync();

                original.MetadatenId = meta.Id;
                _db.Dokumente.Update(original);
                await _db.SaveChangesAsync();

                _logger.LogInformation("🆕 Neues Metadaten-Objekt erstellt (Id={MetaId}) für Dokument {Id}", meta.Id, id);
            }

            // 3️⃣ Alle Metadaten-Felder aktualisieren
            // 3️⃣ Alle Metadaten-Felder aus POST (Metadaten-Objekt) aktualisieren
            _logger.LogInformation("📄 Aktualisiere Metadaten für Dokument {Id}", id);

            meta.Kategorie = Metadaten.Kategorie;
            meta.Beschreibung = Metadaten.Beschreibung;
            meta.Titel = Metadaten.Titel;
            meta.Rechnungsnummer = Metadaten.Rechnungsnummer;
            meta.Kundennummer = Metadaten.Kundennummer;
            meta.Rechnungsbetrag = Metadaten.Rechnungsbetrag;
            meta.Nettobetrag = Metadaten.Nettobetrag;
            meta.Gesamtpreis = Metadaten.Gesamtpreis;
            meta.Steuerbetrag = Metadaten.Steuerbetrag;
            meta.Rechnungsdatum = Metadaten.Rechnungsdatum;
            meta.Lieferdatum = Metadaten.Lieferdatum;
            meta.Faelligkeitsdatum = Metadaten.Faelligkeitsdatum;
            meta.Zahlungsbedingungen = Metadaten.Zahlungsbedingungen;
            meta.Lieferart = Metadaten.Lieferart;
            meta.ArtikelAnzahl = Metadaten.ArtikelAnzahl;

            meta.Email = Metadaten.Email;
            meta.Telefon = Metadaten.Telefon;
            meta.Telefax = Metadaten.Telefax;
            meta.IBAN = Metadaten.IBAN;
            meta.BIC = Metadaten.BIC;
            meta.Bankverbindung = Metadaten.Bankverbindung;

            meta.SteuerNr = Metadaten.SteuerNr;
            meta.UIDNummer = Metadaten.UIDNummer;

            meta.Adresse = Metadaten.Adresse;
            meta.AbsenderAdresse = Metadaten.AbsenderAdresse;
            meta.AnsprechPartner = Metadaten.AnsprechPartner;
            meta.Zeitraum = Metadaten.Zeitraum;

            meta.PdfAutor = Metadaten.PdfAutor;
            meta.PdfBetreff = Metadaten.PdfBetreff;
            meta.PdfSchluesselwoerter = Metadaten.PdfSchluesselwoerter;
            meta.Website = Metadaten.Website;
            meta.OCRText = Metadaten.OCRText;

            try
            {
                // 🖊 Signaturverarbeitung (wie bisher)
                if (!string.IsNullOrWhiteSpace(PendingSignaturesJson))
                {
                    var sigs = System.Text.Json.JsonSerializer.Deserialize<List<SignaturePayload>>(PendingSignaturesJson);
                    _logger.LogInformation("🖊 PendingSignatures geladen: {Count}", sigs?.Count ?? 0);

                    if (sigs?.Any() == true)
                    {
                        using var inputStream = new MemoryStream();
                        await _firebaseStorageService.DownloadToStreamAsync(original.ObjectPath, inputStream);
                        inputStream.Position = 0;

                        _logger.LogInformation("📥 PDF aus Firebase geladen, Größe: {Size} Bytes", inputStream.Length);

                        using var outputStream = new MemoryStream();
                        var writerProps = new iText.Kernel.Pdf.WriterProperties();
                        var pdfWriter = new iText.Kernel.Pdf.PdfWriter(outputStream, writerProps);
                        pdfWriter.SetCloseStream(false);

                        using (var pdfDoc = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(inputStream), pdfWriter))
                        using (var doc = new iText.Layout.Document(pdfDoc))
                        {
                            foreach (var sig in sigs)
                            {
                                var base64Data = sig.ImageBase64.Contains(",")
                                    ? sig.ImageBase64.Split(',')[1]
                                    : sig.ImageBase64;

                                byte[] imgBytes = Convert.FromBase64String(base64Data);

                                _logger.LogInformation("➕ Füge Signatur hinzu: Seite={Page}, X={X}, Y={Y}, W={W}, H={H}",
                                    sig.PageNumber, sig.X, sig.Y, sig.Width, sig.Height);

                                var img = new iText.Layout.Element.Image(
                                    iText.IO.Image.ImageDataFactory.Create(imgBytes)
                                );
                                img.ScaleToFit(sig.Width, sig.Height);

                                var page = pdfDoc.GetPage(sig.PageNumber);
                                var pageHeight = page.GetPageSize().GetHeight();
                                float adjustedY = pageHeight - sig.Y - sig.Height;

                                img.SetFixedPosition(sig.PageNumber, sig.X, adjustedY, sig.Width);
                                doc.Add(img);
                            }
                        }

                        outputStream.Position = 0;

                        // 👉 Neue Version speichern
                        var versionPath = original.ObjectPath.Replace("rechnungen/", "versionen/");
                        var fileName = Path.GetFileNameWithoutExtension(original.Dateiname);
                        var newFileName = $"{fileName}_signed_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                        var newObjectPath = Path.Combine(Path.GetDirectoryName(versionPath)!, newFileName).Replace("\\", "/");

                        _logger.LogInformation("📤 Lade neue Version hoch: {Path}", newObjectPath);

                        await _firebaseStorageService.UploadStreamAsync(outputStream, newObjectPath, "application/pdf");

                        var versionDoc = new DokumentVersionen
                        {
                            DokumentId = original.Id,
                            Dateiname = newFileName,
                            Dateipfad = Path.GetDirectoryName(newObjectPath)?.Replace("\\", "/"),
                            ObjectPath = newObjectPath,
                            AbteilungId = original.AbteilungId,
                            ApplicationUserId = original.ApplicationUserId,
                            HochgeladenAm = DateTime.UtcNow,
                            EstSigne = true,
                            IsVersion = true,
                            VersionsLabel = $"Signierte Version {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                            MetadataJson = System.Text.Json.JsonSerializer.Serialize(meta)
                        };

                        await _db.DokumentVersionen.AddAsync(versionDoc);
                        _logger.LogInformation("🆕 Version in DokumentVersionen angelegt: {Id}", versionDoc.Id);
                    }
                }

                // 🧾 Änderungen speichern
                _db.Metadaten.Update(meta);
                _db.Dokumente.Update(original);
                await _db.SaveChangesAsync();

                TempData["Success"] = "✅ Metadaten & neue Version erfolgreich gespeichert.";
                _logger.LogInformation("✔️ Speichern erfolgreich für Dokument {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Fehler beim Speichern von Metadaten/Version für Dokument {DokumentId}", id);
                TempData["Error"] = $"❌ Fehler beim Speichern: {ex.Message}";
            }

            return RedirectToPage("/Dokument/Index");
        }



    }
    public class SignaturePayload
    {
        public Guid FileId { get; set; }
        public int PageNumber { get; set; }
        public string ImageBase64 { get; set; }
        public float X { get; set; }   // war int
        public float Y { get; set; }   // war int
        public float Width { get; set; }   // war int
        public float Height { get; set; }  // war int
    }


}
