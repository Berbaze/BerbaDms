using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DmsProjeckt.Data;

namespace DmsProjeckt.Pages
{
    [IgnoreAntiforgeryToken]
    public class SignierenModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public SignierenModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public class PendingSignatureVm
        {
            public int Id { get; set; }
            public string DokumentName { get; set; } = "";
            public string RequestedByName { get; set; } = "";
            public DateTime RequestedAt { get; set; }
            public Guid DokumentId { get; set; }
            public string ObjectPath { get; set; }
        }

        public List<PendingSignatureVm> PendingRequests { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);

            PendingRequests = await _db.SignatureRequests
     .Where(r => r.RequestedUserId == userId && r.Status == "Pending")
     .Join(_db.Users,
         r => r.RequestedByUserId,
         u => u.Id,
         (r, u) => new { Request = r, ByUser = u })
     .Join(_db.Dokumente,
         ru => ru.Request.FileId,   // jetzt Guid
         d => d.Id,
         (ru, d) => new PendingSignatureVm
         {
             Id = ru.Request.Id,
             DokumentId = d.Id,
             DokumentName = d.Titel ?? d.Dateiname,
             RequestedByName = $"{ru.ByUser.Vorname} {ru.ByUser.Nachname}",
             RequestedAt = ru.Request.RequestedAt ,
             ObjectPath = d.ObjectPath,
         })
     .OrderByDescending(x => x.RequestedAt)
     .ToListAsync();

        }
    }
}
