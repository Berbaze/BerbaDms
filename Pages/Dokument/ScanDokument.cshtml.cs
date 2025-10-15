using DmsProjeckt.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Pages.Dokument
{
    public class ScanDokumentModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ScanDokumentModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ✅ Liste des Abteilungen disponible seulement pour Admins
        public List<Abteilung> Abteilungen { get; set; } = new();

        // ✅ Pour afficher si besoin dans le Razor (par ex. debug, ou prefill)
        public int? UserAbteilungId { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
            {
                // Admin → peut choisir dans toutes les Abteilungen
                Abteilungen = await _db.Abteilungen
                    .OrderBy(a => a.Name)
                    .ToListAsync();
            }
            else if (user?.AbteilungId != null)
            {
                // User normal → limité à son Abteilung
                UserAbteilungId = user.AbteilungId;

                // (optionnel : charger son abteilung pour affichage)
                Abteilungen = await _db.Abteilungen
                    .Where(a => a.Id == user.AbteilungId)
                    .ToListAsync();
            }
        }

        public class MetadataModel
        {
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
            public string ObjectPath { get; set; }
        }
    }
}
