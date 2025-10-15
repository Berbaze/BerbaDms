using DmsProjeckt.Data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Pages
{
    public class SharedNoteDto
    {
        public int NoteId { get; set; }
        public string NoteTitle { get; set; }
        public string SharedUserName { get; set; }
        public DateTime SharedAt { get; set; }
        public string ObjectPath => $"/Notiz/Index?id={NoteId}";
    }

    public class GeteilteDokumenteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GeteilteDokumenteModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<SharedDocumentDto> DocumentsSharedByMe { get; set; } = new();
        public List<SharedDocumentDto> DocumentsSharedWithMe { get; set; } = new();

        public List<SharedNoteDto> NotesSharedByMe { get; set; } = new();
        public List<SharedNoteDto> NotesSharedWithMe { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Dokumente, die ich geteilt habe
            DocumentsSharedByMe = await _context.UserSharedDocuments
                .Where(x => x.SharedByUserId == userId)
                .Include(x => x.Dokument)
                .Include(x => x.SharedToUser)
                .Select(x => new SharedDocumentDto
                {
                    DokumentId = x.Id,
                    DokumentTitle = x.Dokument.Titel, // Passe Feldnamen ggf. an
                    SharedByUserName = x.SharedToUser.UserName,
                    SharedAt = x.SharedAt,
                    ObjectPath = x.Dokument.ObjectPath
                })
                .ToListAsync();

            // 2. Dokumente, die mit mir geteilt wurden
            DocumentsSharedWithMe = await _context.UserSharedDocuments
                .Where(x => x.SharedToUserId == userId)
                .Include(x => x.Dokument)
                .Include(x => x.SharedByUser)
                .Select(x => new SharedDocumentDto
                {
                    DokumentId = x.Id,
                    DokumentTitle = x.Dokument.Titel, // Passe Feldnamen ggf. an
                    SharedByUserName = x.SharedToUser.UserName,
                    SharedAt = x.SharedAt,
                    ObjectPath = x.Dokument.ObjectPath
                })
                .ToListAsync();

            // Notizen, die ich geteilt habe
            NotesSharedByMe = await _context.UserSharedNotes
                .Where(x => x.SharedByUserId == userId)
                .Include(x => x.Notiz)
                .Include(x => x.SharedToUser)
                .OrderByDescending(x => x.SharedAt)
                .Select(x => new SharedNoteDto
                {
                    NoteId = x.NotizId,
                    NoteTitle = x.Notiz.Titel,
                    SharedUserName = x.SharedToUser.Vorname + " " + x.SharedToUser.Nachname,
                    SharedAt = x.SharedAt
                })
                .ToListAsync();

            // Notizen, die mit mir geteilt wurden
            NotesSharedWithMe = await _context.UserSharedNotes
                .Where(x => x.SharedToUserId == userId)
                .Include(x => x.Notiz)
                .Include(x => x.SharedByUser)
                .OrderByDescending(x => x.SharedAt)
                .Select(x => new SharedNoteDto
                {
                    NoteId = x.NotizId,
                    NoteTitle = x.Notiz.Titel,
                    SharedUserName = x.SharedByUser.Vorname + " " + x.SharedByUser.Nachname,
                    SharedAt = x.SharedAt
                })
                .ToListAsync();
        }
    }
}
