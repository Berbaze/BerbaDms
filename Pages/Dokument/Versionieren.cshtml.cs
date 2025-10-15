using DmsProjeckt.Data;
using DmsProjeckt.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Pages.Dokument
{
    public class VersionierenModel : PageModel
    {

        private readonly ApplicationDbContext _db;
        private readonly VersionierungsService _versionierungsService;

        public VersionierenModel(ApplicationDbContext db, VersionierungsService versionierungsService)
        {
            _db = db;
            _versionierungsService = versionierungsService;
        }

        [BindProperty]
        public Dokumente Dokument { get; set; }
        [BindProperty]
        public Metadaten Metadaten { get; set; } = new();
        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var original = await _db.Dokumente.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
            if (original == null) return NotFound();

            Dokument = original;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 🔹 1️⃣ Sicherheitscheck: existiert das Dokument wirklich?
            var dokument = await _db.Dokumente
                .Include(d => d.MetadatenObjekt)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dokument == null)
                return NotFound();

            // 🔹 2️⃣ Sicherstellen, dass ein Metadatenobjekt existiert
            var meta = dokument.MetadatenObjekt;
            if (meta == null)
            {
                meta = new Metadaten
                {
                    Titel = Metadaten.Titel,
                    Kategorie = Metadaten.Kategorie,
                    Beschreibung = Metadaten.Beschreibung
                };

                _db.Metadaten.Add(meta);
                await _db.SaveChangesAsync();

                dokument.MetadatenId = meta.Id;
                _db.Dokumente.Update(dokument);
                await _db.SaveChangesAsync();

                dokument.MetadatenObjekt = meta;

                Console.WriteLine($"🆕 Neues Metadatenobjekt erstellt (Id={meta.Id}) für Dokument {dokument.Id}");
            }

            // 🔹 3️⃣ Metadaten aus POST übernehmen
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

            // 🔹 4️⃣ Änderungen speichern
            _db.Metadaten.Update(meta);
            await _db.SaveChangesAsync();

            // 🔹 5️⃣ Versionierung aufrufen mit Metadaten (nicht mehr Dokument)
            await _versionierungsService.SpeichereVersionAsync(id, userId, null, meta);

            TempData["Success"] = "✅ Version erfolgreich gespeichert.";
            return RedirectToPage("/Dokument/Index");
        }


    }
}
