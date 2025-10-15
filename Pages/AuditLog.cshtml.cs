using System.Security.AccessControl;
using DmsProjeckt.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Pages
{
    public class AuditLogModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public AuditLogModel(ApplicationDbContext context)
        {
            _context = context;
        }
        [BindProperty(SupportsGet = true)]
        public string? KategorieFilter { get; set; }

        public List<string> Kategorien { get; set; } = new List<string>();
        public List<AuditLogDto> AuditLogEntries { get; set; } = new List<AuditLogDto>();
        
        public async Task OnGetAsync()
        {
            // Alle Logs holen
            var logs = await _context.AuditLogs
                .Include(a => a.Benutzer)
                .OrderByDescending(a => a.Zeitstempel)
                .Select(a => new AuditLogDto
                {
                    Aktion = a.Aktion,
                    BenutzerId = a.BenutzerId,
                    BenutzerName = a.Benutzer != null ? a.Benutzer.Vorname + " " + a.Benutzer.Nachname : "",
                    BenutzerEmail = a.Benutzer != null ? a.Benutzer.Email : "",
                    Zeitstempel = a.Zeitstempel
                })
                .ToListAsync();

            // Kategorien extrahieren (erstes Wort vor Leerzeichen)
            // Kategorien extrahieren (angepasst!)
            Kategorien = logs
                .Select(l => ExtractKategorie(l.Aktion))
                .Distinct()
                .OrderBy(k => k)
                .ToList();


            // Filtern, falls Filter gesetzt
            if (!string.IsNullOrEmpty(KategorieFilter))
            {
                logs = logs
                    .Where(l => ExtractKategorie(l.Aktion) == KategorieFilter)
                    .ToList();
            }

            AuditLogEntries = logs.ToList();
        }
        private static string ExtractKategorie(string aktion)
        {
            if (string.IsNullOrWhiteSpace(aktion))
                return "(Unbekannt)";

            // Schritt X in Workflow ... zählt als "Workflow"
            if (aktion.StartsWith("Schritt") && aktion.Contains("Workflow"))
                return "Workflow";

            // Sonst wie gehabt: Erstes Wort als Kategorie
            return aktion.Split(' ')[0];
        }

    }
}
