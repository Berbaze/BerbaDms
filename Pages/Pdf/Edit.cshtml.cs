using DmsProjeckt.Data;
using DmsProjeckt.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Pages.Pdf
{

    [Authorize(Roles = "Admin,SuperAdmin")]
    public class EditModel : PageModel
    {
        private readonly FirebaseStorageService _firebaseService;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(FirebaseStorageService firebaseService, ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _firebaseService = firebaseService;
            _db = db;
            _userManager = userManager;
        }

        public List<DisplayPdf> Files { get; set; }

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);

            // 🔐 Redirection si l'utilisateur n'est pas connecté
            if (string.IsNullOrEmpty(userId))
            {
                RedirectToPage("/Account/Login");
                return;
            }

            // 📁 Firebase tree
            var firebaseTree = await _firebaseService.GetFolderTreeAsync("dokumente/");
            var firebaseFiles = firebaseTree
                .SelectMany(folder => folder.Files)
                .Where(f => f.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                .Select(f => new DisplayPdf
                {
                    Name = f.Name,
                    Path = f.Path,
                    Source = "Firebase"
                }).ToList();

            // 🛡️ Fichiers BDD
            var dbFiles = await _db.Dokumente
                .Where(d => d.ApplicationUserId == userId && !string.IsNullOrEmpty(d.Dateipfad) && d.Dateipfad.EndsWith(".pdf"))
                .Select(d => new DisplayPdf
                {
                    Name = d.Dateiname,
                    Path = d.Dateipfad,
                    Source = "BDD"
                })
                .ToListAsync();

            // 🎯 Choix : avec ou sans Firebase
            Files = dbFiles
                //.Concat(firebaseFiles) // active si tu veux combiner BDD + Firebase
                .DistinctBy(f => f.Path)
                .ToList();
        }
    }
}
