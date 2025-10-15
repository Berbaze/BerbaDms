using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using System;
using DmsProjeckt.Data;
using Microsoft.EntityFrameworkCore;
namespace DmsProjeckt.Pages.Notiz
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        [BindProperty]
        public DmsProjeckt.Data.Notiz Note { get; set; }
        public List<DmsProjeckt.Data.Notiz> Notes { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            Notes = await _context.Notiz
        .Where(n => n.UserId == userId)
        .OrderByDescending(n => n.LetzteBearbeitung)
        .ToListAsync();
            if (Id.HasValue)
            {
                
                if (Note == null) return NotFound();
            }
            else
            {
                Note = new DmsProjeckt.Data.Notiz();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            if (!ModelState.IsValid) return Page();

            if (Note.Id == 0)
            {
                Note.UserId = userId;
                Note.LetzteBearbeitung = DateTime.Now;
                _context.Notiz.Add(Note);
            }
            else
            {
                var dbNote = await _context.Notiz.FirstOrDefaultAsync(n => n.Id == Note.Id && n.UserId == userId);
                if (dbNote == null) return NotFound();
                dbNote.Titel = Note.Titel;
                dbNote.Inhalt = Note.Inhalt;
                dbNote.LetzteBearbeitung = DateTime.Now;
            }
            await _context.SaveChangesAsync();
            return RedirectToPage("/Notiz/Index", new { id = Note.Id });
        }
    }
}
