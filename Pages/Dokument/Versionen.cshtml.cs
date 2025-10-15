using DmsProjeckt.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;


namespace DmsProjeckt.Pages.Dokument
{
    public class VersionenModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<VersionenModel> _logger;

        public VersionenModel(ApplicationDbContext db, ILogger<VersionenModel> logger)
        {
            _db = db;
            _logger = logger;
        }

        public List<DokumentVersionen> DokumentVersionen { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? dokumentId)
        {
            if (dokumentId == null)
            {
                _logger.LogWarning("⚠️ VersionenModel: dokumentId null");
                return BadRequest("DokumentId fehlt");
            }

            _logger.LogInformation("🔍 VersionenModel OnGet: dokumentId={Id}", dokumentId);

            DokumentVersionen = await _db.DokumentVersionen
                .Include(v => v.ApplicationUser)
                .Where(v => v.DokumentId == dokumentId)
                .OrderByDescending(v => v.HochgeladenAm)
                .ToListAsync();

            _logger.LogInformation("🔁 {Count} Versionen gefunden", DokumentVersionen.Count);
            return Page();
        }
    }
}
