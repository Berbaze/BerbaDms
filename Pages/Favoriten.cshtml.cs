using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DmsProjeckt.Data;
using Microsoft.AspNetCore.Identity;

namespace DmsProjeckt.Pages
{
    [Authorize]
    public class FavoritenModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public List<FavDocDto> FavoriteDocs { get; set; } = new();
        public List<FavNoteDto> FavoriteNotes { get; set; } = new();

        public FavoritenModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);

            FavoriteDocs = await _context.UserFavoritDokumente
                .Include(f => f.Dokument)
                .Where(f => f.ApplicationUserId == userId)
                .OrderByDescending(f => f.AngelegtAm)
                .Select(f => new FavDocDto
                {
                    Id = f.Dokument.Id,
                    Titel = f.Dokument.Titel,
                    HinzugefuegtAm = f.AngelegtAm
                })
                .ToListAsync();

            FavoriteNotes = await _context.UserFavoritNote
                .Include(f => f.Notiz)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.HinzugefuegtAm)
                .Select(f => new FavNoteDto
                {
                    Id = f.Notiz.Id,
                    Titel = f.Notiz.Titel,
                    HinzugefuegtAm = f.HinzugefuegtAm
                })
                .ToListAsync();
        }
    }

    public class FavDocDto
    {
        public Guid Id { get; set; }
        public string Titel { get; set; }
        public DateTime HinzugefuegtAm { get; set; }
    }

    public class FavNoteDto
    {
        public int Id { get; set; }
        public string Titel { get; set; }
        public DateTime HinzugefuegtAm { get; set; }
    }
}
