using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace DmsProjeckt.Pages.Notiz
{
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using System.Linq;
    using DmsProjeckt.Data;
    using Microsoft.AspNetCore.Identity;
    using DmsProjeckt.Service;

    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public List<Notiz> Notes { get; set; }
        [BindProperty]
        public Notiz SelectedNote { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;
        public List<int> FavoriteNoteIds { get; set; }
        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            Notes = await _context.Notiz
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.LetzteBearbeitung)
                .ToListAsync();
            FavoriteNoteIds = await _context.UserFavoritNote
    .Where(f => f.UserId == userId)
    .Select(f => f.NotizId)
    .ToListAsync();

            Console.WriteLine($"Geladene Notizen: {Notes.Count}");
            Console.WriteLine($"Gesuchte Id: {Id}");
            foreach(var n in Notes)
                Console.WriteLine($"Notiz: {n.Id} - {n.Titel}");

            if (Id.HasValue)
                SelectedNote = Notes.FirstOrDefault(n => n.Id == Id);
            else
                SelectedNote = Notes.FirstOrDefault();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            Notes = await _context.Notiz
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.LetzteBearbeitung)
                .ToListAsync();

            if (SelectedNote?.Id > 0)
            {
                var note = await _context.Notiz.FirstOrDefaultAsync(n => n.Id == SelectedNote.Id && n.UserId == userId);
                if (note != null)
                {
                    note.Titel = SelectedNote.Titel;
                    note.Inhalt = SelectedNote.Inhalt;
                    note.LetzteBearbeitung = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            return new EmptyResult(); // AJAX erwartet keinen Redirect!
        }
        public async Task<IActionResult> OnPostNewNoteAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            var note = new Notiz
            {
                UserId = userId,
                Titel = "Neue Notiz",
                Inhalt = "",
                LetzteBearbeitung = DateTime.Now
            };
            _context.Notiz.Add(note);
            await _context.SaveChangesAsync();
            // Nach dem Erstellen, auf die neue Notiz springen:
            return RedirectToPage(new { id = note.Id });
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostDeleteNoteAsync([FromBody] int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            var note = await _context.Notiz.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (note != null)
            {
                _context.Notiz.Remove(note);
                await _context.SaveChangesAsync();
            }
            return new JsonResult(new { success = true});
        }

        [Authorize]
        public async Task<IActionResult> OnGetGetUsersFromCompanyAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            if (currentUser == null || string.IsNullOrEmpty(currentUser.FirmenName))
                return new JsonResult(new { error = "Kein Firmenname gefunden." });

            var users = await _context.Users
                .Where(u => u.FirmenName == currentUser.FirmenName && u.Id != currentUser.Id)
                .Select(u => new { u.Id, Name = u.Vorname + " " + u.Nachname, u.Email })
                .ToListAsync();

            return new JsonResult(users);
        }

        [Authorize]
        public async Task<IActionResult> OnPostShareNoteAsync([FromBody] ShareNoteInput input)
        {
            var byUserId = _userManager.GetUserId(User);
            if (input == null || input.UserIds == null || input.NoteId == 0)
                return BadRequest("Ungültige Eingabe.");

            var note = await _context.Notiz.FindAsync(input.NoteId);
            if (note == null) return NotFound($"Notiz mit ID '{input.NoteId}' nicht gefunden.");

            var user = await _context.Users.FindAsync(byUserId);
            if (user == null) return NotFound($"User mit ID '{byUserId}' nicht gefunden.");

            foreach (var userId in input.UserIds)
            {
                var alreadyExists = await _context.UserSharedNotes
                    .AnyAsync(x => x.NotizId == input.NoteId && x.SharedToUserId == userId.ToString());

                if (!alreadyExists)
                {
                    _context.UserSharedNotes.Add(new UserSharedNote
                    {
                        NotizId = input.NoteId,
                        SharedToUserId = userId.ToString(),
                        SharedAt = DateTime.Now,
                        SharedByUserId = byUserId.ToString()
                    });
                }
                var notificationType = await _context.NotificationTypes
                    .FirstOrDefaultAsync(n => n.Name == "Note shared");
                var setting = await _context.UserNotificationSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.NotificationTypeId == notificationType.Id);

                var notificationTypeEmail = await _context.NotificationTypes
                    .FirstOrDefaultAsync(n => n.Name == "Note shared email");
                var settingEmail = await _context.UserNotificationSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.NotificationTypeId == notificationTypeEmail.Id);
                if(setting == null || setting.Enabled)
                {
                    var notification = new Notification
                    {
                        Title = "Notiz geteilt",
                        Content = "Eine Notiz wurde mit Ihnen geteilt.",
                        CreatedAt = DateTime.UtcNow,
                        NotificationTypeId = notificationType.Id,
                        ActionLink = "/GeteilteDokumente"
                    };
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    var userNotification = new UserNotification
                    {
                        UserId = userId,
                        NotificationId = notification.Id,
                        IsRead = false,
                        ReceivedAt = DateTime.UtcNow
                    };
                    _context.UserNotifications.Add(userNotification);
                    await _context.SaveChangesAsync();
                }
                if(settingEmail == null || settingEmail.Enabled)
                {
                    var userTo = await _context.Users.FindAsync(userId);
                    var subject = "Notiz geteilt";
                    string body = $@"
                                    <p>Hallo {userTo.Vorname},</p>
                                    <p>Mit Ihnen wurde die Notiz <b>""{note.Titel}""</b> geteilt</p>
                                    <p><a href='GeteilteDokumente'>Dokument ansehen</a></p>
                                    <p>Viele Grüße,<br/>Dein Team</p>";

                    await _emailService.SendEmailAsync(userTo.Email, subject, body);
                }
                // Optional: Benachrichtigung wie bei Dokumenten
                // ...
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
        [Authorize]
        [Authorize]
        public async Task<IActionResult> OnPostToggleFavoriteNoteAsync([FromBody] int noteId)
        {
            var userId = _userManager.GetUserId(User);

            // Prüfen ob die Notiz existiert und dem User gehört
            var noteExists = await _context.Notiz.AnyAsync(n => n.Id == noteId && n.UserId == userId);
            if (!noteExists)
            {
                return BadRequest($"Notiz {noteId} existiert nicht oder gehört nicht dir.");
            }

            var existing = await _context.UserFavoritNote
                .FirstOrDefaultAsync(f => f.NotizId == noteId && f.UserId == userId);

            if (existing != null)
            {
                _context.UserFavoritNote.Remove(existing);
                await _context.SaveChangesAsync();
                return new JsonResult(new { isFavorite = false });
            }
            else
            {
                var fav = new UserFavoritNote
                {
                    NotizId = noteId,
                    UserId = userId
                };
                _context.UserFavoritNote.Add(fav);
                await _context.SaveChangesAsync();
                return new JsonResult(new { isFavorite = true });
            }
        }


    }
    public class ShareNoteInput
    {
        public int NoteId { get; set; }
        public List<string> UserIds { get; set; }
    }
}
