using DmsProjeckt.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace DmsProjeckt.Pages.Dokument
{
    public class SuchenModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public SuchenModel(ApplicationDbContext db) => _db = db;

        // WICHTIG: BindProperty für GET!
        [BindProperty(SupportsGet = true)]
        public FilterModel Filter { get; set; } = new();

        // Für die Ergebnistabelle
        public List<DokumentDto> Results { get; set; } = new();

        // Merkt sich den Modus für UI
        [BindProperty(SupportsGet = true)]
        public string CurrentSearchMode { get; set; } = "intelligent";

        // Filter-Klasse mit allen Feldern!
        public class FilterModel
        {
            public string Dateiname { get; set; }
            public string Kategorie { get; set; }
            public string Rechnungsnummer { get; set; }
            public string Kundennummer { get; set; }
            public string UIDNummer { get; set; }
            public string OCRText { get; set; }
            public string Beschreibung { get; set; }
        }

        // DTO für Tabelle
        public class DokumentDto
        {
            public Guid Id { get; set; }
            public string? Dateiname { get; set; }
            public DateTime HochgeladenAm { get; set; }
            public string? Kategorie { get; set; }
        }

        // ======= Standard-Laden beim ersten Öffnen oder Fallback =======
        public async Task OnGetAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                Results = new List<DokumentDto>();
                return;
            }

            var query = _db.Dokumente
                .Include(d => d.MetadatenObjekt)
                .Where(d => d.ApplicationUserId == userId);

            // 🔍 Filter auf Dokument oder Metadaten anwenden
            if (!string.IsNullOrWhiteSpace(Filter.Dateiname))
                query = query.Where(d => d.Dateiname.Contains(Filter.Dateiname));
            if (!string.IsNullOrWhiteSpace(Filter.Rechnungsnummer))
                query = query.Where(d => d.MetadatenObjekt.Rechnungsnummer.Contains(Filter.Rechnungsnummer));
            if (!string.IsNullOrWhiteSpace(Filter.UIDNummer))
                query = query.Where(d => d.MetadatenObjekt.UIDNummer.Contains(Filter.UIDNummer));
            if (!string.IsNullOrWhiteSpace(Filter.OCRText))
                query = query.Where(d => d.MetadatenObjekt.OCRText.Contains(Filter.OCRText));
            if (!string.IsNullOrWhiteSpace(Filter.Kundennummer))
                query = query.Where(d => d.MetadatenObjekt.Kundennummer.Contains(Filter.Kundennummer));
            if (!string.IsNullOrWhiteSpace(Filter.Kategorie))
                query = query.Where(d =>
                    (d.MetadatenObjekt.Kategorie ?? d.Kategorie).Contains(Filter.Kategorie));
            if (!string.IsNullOrWhiteSpace(Filter.Beschreibung))
                query = query.Where(d => d.MetadatenObjekt.Beschreibung.Contains(Filter.Beschreibung));

            // 🔽 Ergebnisliste aufbauen
            Results = await query
                .OrderByDescending(d => d.HochgeladenAm)
                .Select(d => new DokumentDto
                {
                    Dateiname = d.Dateiname,
                    HochgeladenAm = d.HochgeladenAm,
                    Kategorie = d.MetadatenObjekt.Kategorie ?? d.Kategorie
                })
                .ToListAsync();

            // 🔖 Suchverlauf speichern
            if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(Filter.Dateiname))
            {
                var searchHistory = new SearchHistory
                {
                    UserId = userId,
                    SearchTerm = Filter.Dateiname,
                    SearchedAt = DateTime.Now
                };
                _db.SearchHistory.Add(searchHistory);
                await _db.SaveChangesAsync();
            }
        }



        // ======= Intelligente Suche (AJAX / Live) =======
        public async Task<JsonResult> OnGetSearchLiveAsync(string query)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return new JsonResult(new List<DokumentDto>());

            if (string.IsNullOrWhiteSpace(query))
            {
                var allDocs = await _db.Dokumente
                    .Include(d => d.MetadatenObjekt)
                    .Where(d => d.ApplicationUserId == userId)
                    .OrderByDescending(d => d.HochgeladenAm)
                    .Select(d => new DokumentDto
                    {
                        Dateiname = d.Dateiname,
                        HochgeladenAm = d.HochgeladenAm,
                        Kategorie = d.MetadatenObjekt.Kategorie ?? d.Kategorie
                    })
                    .ToListAsync();
                return new JsonResult(allDocs);
            }

            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var docs = await _db.Dokumente
                .Include(d => d.MetadatenObjekt)
                .Where(d => d.ApplicationUserId == userId &&
                    words.All(w =>
                        (d.MetadatenObjekt.Titel ?? "").Contains(w) ||
                        (d.Dateiname ?? "").Contains(w) ||
                        (d.MetadatenObjekt.Kategorie ?? d.Kategorie ?? "").Contains(w) ||
                        (d.MetadatenObjekt.Beschreibung ?? "").Contains(w) ||
                        (d.MetadatenObjekt.Rechnungsnummer ?? "").Contains(w) ||
                        (d.MetadatenObjekt.Kundennummer ?? "").Contains(w) ||
                        (d.MetadatenObjekt.UIDNummer ?? "").Contains(w) ||
                        (d.MetadatenObjekt.OCRText ?? "").Contains(w)
                    )
                )
                .OrderByDescending(d => d.HochgeladenAm)
                .Select(d => new DokumentDto
                {
                    Dateiname = d.Dateiname,
                    HochgeladenAm = d.HochgeladenAm,
                    Kategorie = d.MetadatenObjekt.Kategorie ?? d.Kategorie
                })
                .ToListAsync();

            // 🔖 Suchverlauf speichern
            if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(query))
            {
                var searchHistory = new SearchHistory
                {
                    UserId = userId,
                    SearchTerm = query,
                    SearchedAt = DateTime.Now
                };
                _db.SearchHistory.Add(searchHistory);
                await _db.SaveChangesAsync();
            }

            return new JsonResult(docs);
        }


        // ======= Vorschläge für Autocomplete (AJAX) =======
        public async Task<JsonResult> OnGetSuggestAsync(string term)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(term) || string.IsNullOrWhiteSpace(userId))
                return new JsonResult(new List<string>());

            var suggestions = await _db.Dokumente
                .Include(d => d.MetadatenObjekt)
                .Where(d => d.ApplicationUserId == userId &&
                    (
                        (d.MetadatenObjekt.Titel ?? "").Contains(term) ||
                        (d.Dateiname ?? "").Contains(term) ||
                        (d.MetadatenObjekt.Kategorie ?? d.Kategorie ?? "").Contains(term) ||
                        (d.MetadatenObjekt.Beschreibung ?? "").Contains(term) ||
                        (d.MetadatenObjekt.Rechnungsnummer ?? "").Contains(term) ||
                        (d.MetadatenObjekt.Kundennummer ?? "").Contains(term) ||
                        (d.MetadatenObjekt.UIDNummer ?? "").Contains(term) ||
                        (d.MetadatenObjekt.OCRText ?? "").Contains(term)
                    )
                )
                .Select(d => new[] {
            d.MetadatenObjekt.Titel,
            d.Dateiname,
            d.MetadatenObjekt.Kategorie ?? d.Kategorie,
            d.MetadatenObjekt.Beschreibung,
            d.MetadatenObjekt.Rechnungsnummer,
            d.MetadatenObjekt.Kundennummer,
            d.MetadatenObjekt.UIDNummer,
            d.MetadatenObjekt.OCRText
                })
                .ToListAsync();

            var allSuggestions = suggestions
                .SelectMany(x => x)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Take(15)
                .ToList();

            return new JsonResult(allSuggestions);
        }


    }
}
