using System.Security.Claims;
using DmsProjeckt.Data;
using DmsProjeckt.Pages;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DmsProjeckt.Controllers
{
    [Authorize]
    [Route("Dashboard")]

    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("Load")]
        public async Task<IActionResult> Load()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);
                bool isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");

                var data = await _context.UserDashboardItem
                    .Include(u => u.DashboardItem)
                    .Where(u => u.UserId == userId)
                    .Select(u => new
                    {
                        id = u.DashboardItem.Nail,
                        title = u.DashboardItem.Title,
                        icon = u.DashboardItem.Icon,
                        x = u.X,
                        y = u.Y,
                        w = u.Width,
                        h = u.Height,
                        locked = u.Locked,
                        beschreibung = u.DashboardItem.Beschreibung,
                        favorit = u.Favorit
                    })
                    .ToListAsync();

                // Hier das Adminwidget rausfiltern, wenn kein Admin
                if (!isAdmin)
                {
                    data = data.Where(d => d.id != "admin").ToList();
                }

                return Json(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler in Load(): " + ex.ToString());
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("SaveLayout")]
        public async Task<IActionResult> SaveLayout([FromBody] List<UserWidgetDto> widgets)
        {
            try
            {
                Console.WriteLine("🔔 SaveLayout() aufgerufen");
                Console.WriteLine($"Empfangene Widgets: {widgets?.Count}");

                if (widgets == null || !widgets.Any())
                    return BadRequest(new { error = "Keine gültigen Widgets übergeben." });

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    Console.WriteLine("❌ Kein gültiger Benutzer.");
                    return Unauthorized();
                }

                var existing = _context.UserDashboardItem.Where(w => w.UserId == userId);
                _context.UserDashboardItem.RemoveRange(existing);
                await _context.SaveChangesAsync();

                foreach (var w in widgets)
                {
                    var key = w.id?.Trim();

                    if (string.IsNullOrWhiteSpace(key))
                    {
                        Console.WriteLine("❌ Widget hat leere oder null-ID – wird übersprungen.");
                        continue;
                    }

                    var dashboardItem = await _context.DashboardItem
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Nail.ToLower() == key);

                    Console.WriteLine("📛 dashboardItem = " + (dashboardItem == null ? "NULL" : dashboardItem.Id.ToString()));
                    if (dashboardItem == null)
                    {
                        Console.WriteLine($"⚠️ Kein DashboardItem mit Key '{key}' gefunden.");
                        continue;

                    }

                    _context.UserDashboardItem.Add(new UserDashboardItem
                    {
                        UserId = userId,
                        DashboardItemId = dashboardItem.Id,
                        X = w.x,
                        Y = w.y,
                        Width = w.w,
                        Height = w.h,
                        Locked = w.locked,
                        Favorit = w.favorit
                    });
                    Console.WriteLine($"✅ Widget gespeichert: {w.id} → DashboardItemId = {dashboardItem.Id}");
                    Console.WriteLine($"Favorit-Wert für {w.id}: {w.favorit}");

                }

                await _context.SaveChangesAsync();
                Console.WriteLine("✅ Alle Änderungen gespeichert.");
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Ausnahme in SaveLayout: " + ex);


                return StatusCode(500, new
                {
                    error = "Fehler beim Speichern",
                    details = ex.Message
                });
            }
        }

        [HttpGet("GetAvailable")]
        public async Task<IActionResult> GetAvailable()
        {
            var user = await _userManager.GetUserAsync(User);
            bool isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");

            var items = await _context.DashboardItem
                .Select(d => new
                {
                    id = d.Nail,
                    title = d.Title,
                    icon = d.Icon,
                    beschreibung = d.Beschreibung
                })
                .ToListAsync();

            // Admin-Widget nur für Admins
            if (!isAdmin)
            {
                items = items.Where(i => i.id != "admin").ToList();
            }
            return Json(items);
        }

        [Authorize]
        [HttpGet("Letzte")]
        public async Task<IActionResult> Letzte()
        {
            var userId = _userManager.GetUserId(User);
            var notizen = await _context.Notiz
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.LetzteBearbeitung)
                .Take(5)
                .Select(n => new
                {
                    id = n.Id,
                    title = n.Titel,
                    datum = n.LetzteBearbeitung.ToString("dd.MM.yyyy HH:mm")
                })
                .ToListAsync();

            return Json(notizen);
        }
        [HttpPost("Erstellen")]
        public async Task<IActionResult> Erstellen([FromBody] NotizInputModel model)
        {
            if (model == null)
                return BadRequest("Kein gültiges Modell erhalten.");
            var userId = _userManager.GetUserId(User);
            var notiz = new Notiz
            {
                Titel = model.Title,
                Inhalt = model.Content,
                LetzteBearbeitung = DateTime.Now,
                UserId = userId
            };

            _context.Notiz.Add(notiz);
            await _context.SaveChangesAsync();

            return Ok(new { id = notiz.Id });
        }

        [HttpGet("Notiz/{id}")]
        public async Task<IActionResult> GetNotiz(int id)
        {
            var userId = _userManager.GetUserId(User);
            var note = await _context.Notiz
                .Where(n => n.UserId == userId && n.Id == id)
                .Select(n => new { n.Id, title = n.Titel, content = n.Inhalt })
                .FirstOrDefaultAsync();

            if (note == null) return NotFound();
            return Json(note);
        }
        [HttpDelete("DeleteNotiz/{id}")]
        public async Task<IActionResult> DeleteNotiz(int id)
        {
            var userId = _userManager.GetUserId(User);
            var notiz = await _context.Notiz.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notiz == null) return NotFound();

            _context.Notiz.Remove(notiz);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpPost("Update")]
        public async Task<IActionResult> Update([FromBody] NotizInputModel model)
        {
            var userId = _userManager.GetUserId(User);

            Console.WriteLine($"🔧 Eingehender Update-Request: Id={model.Id}, Title={model.Title}");

            var note = await _context.Notiz
                .FirstOrDefaultAsync(n => n.Id == model.Id && n.UserId == userId);

            if (note == null)
            {
                Console.WriteLine($"❌ Notiz mit ID {model.Id} für Benutzer {userId} nicht gefunden.");
                return NotFound();
            }

            note.Titel = model.Title;
            note.Inhalt = model.Content;
            note.LetzteBearbeitung = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("AlleNotizen")]
        public async Task<IActionResult> AlleNotizen()
        {
            var userId = _userManager.GetUserId(User);
            var notizen = await _context.Notiz
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.LetzteBearbeitung)
                .Select(n => new
                {
                    id = n.Id,
                    title = n.Titel,
                    datum = n.LetzteBearbeitung.ToString("g")
                })
                .ToListAsync();

            return Json(notizen);
        }
        [HttpGet("Aufgaben")]
        public async Task<IActionResult> Aufgaben()
        {
            var userId = _userManager.GetUserId(User);
            Console.WriteLine("🔍 Aktuelle UserId im Dashboard: " + userId);
            var aufgaben = await _context.Aufgaben
                .Where(a => a.FuerUser == userId && !a.Erledigt && a.Aktiv)
                .OrderBy(a => a.FaelligBis)
                .Select(a => new {
                    titel = a.Titel,
                    faelligBis = a.FaelligBis,
                    prioritaet = a.Prioritaet
                })
                .ToListAsync();

            return Json(aufgaben);
        }
        [HttpGet("AuditLog")]
        public async Task<IActionResult> GetAuditLog()
        {

            var logs = await _context.AuditLogs
                .Include(a => a.Benutzer)
                .OrderByDescending(a => a.Zeitstempel)
                .Take(10)
                .Select(a => new AuditLogDto
                {
                    Aktion = a.Aktion,
                    BenutzerName = a.Benutzer.Vorname + " " + a.Benutzer.Nachname,
                    Zeitstempel = a.Zeitstempel
                })
                .ToListAsync();

            return new JsonResult(logs); // Jetzt ist logs eine echte List<AuditLogDto>
        }

        [HttpPost("ToggleFavorite/{id}")]
        public async Task<IActionResult> ToggleFavorite(string id)
        {
            var userId = _userManager.GetUserId(User);

            // DashboardItem anhand Nail suchen
            var dashboardItem = await _context.DashboardItem
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Nail.ToLower() == id.ToLower());

            if (dashboardItem == null)
                return NotFound(new { error = "Widget nicht gefunden." });

            // UserDashboardItem für User+Widget suchen
            var userWidget = await _context.UserDashboardItem
                .FirstOrDefaultAsync(u => u.UserId == userId && u.DashboardItemId == dashboardItem.Id);

            if (userWidget == null)
                return NotFound(new { error = "Widget nicht gefunden (UserWidget null)" });

            // **DB-Flag toggeln**
            userWidget.Favorit = !userWidget.Favorit;
            await _context.SaveChangesAsync();

            // Session-Liste NACH dem neuen Status anpassen
            var favoritesJson = HttpContext.Session.GetString("FavoriteWidgets") ?? "[]";
            var favorites = System.Text.Json.JsonSerializer.Deserialize<List<string>>(favoritesJson);

            if (userWidget.Favorit)
            {
                if (!favorites.Contains(id))
                    favorites.Add(id);
            }
            else
            {
                favorites.Remove(id);
            }
            HttpContext.Session.SetString("FavoriteWidgets", System.Text.Json.JsonSerializer.Serialize(favorites));

            return Json(new { isFavorite = userWidget.Favorit });
        }



        [HttpGet("GetFavoriten")]
        public async Task<IActionResult> GetFavoriten()
        {
            var userId = _userManager.GetUserId(User); // oder wie du UserId bekommst
            var favs = await _context.UserDashboardItem
                .Where(x => x.UserId == userId && x.Favorit == true)
                .Join(_context.DashboardItem, u => u.DashboardItemId, d => d.Id, (u, d) => new {
                    id = d.Nail,
                    title = d.Title,
                    icon = d.Icon,
                    beschreibung = d.Beschreibung,
                    actionLink = d.ActionLink
                })
                .ToListAsync();
            return Json(favs);
        }
        [HttpPost]
        public async Task<IActionResult> OnPostToggleFavoritAsync([FromBody] Guid dokumentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var dokument = await _context.Dokumente.FirstOrDefaultAsync(d => d.Id == dokumentId && d.ApplicationUserId == userId);

            if (dokument == null)
                return NotFound(new { success = false, message = "Dokument nicht gefunden." });

            dokument.IstFavorit = !dokument.IstFavorit; // Toggle
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, istFavorit = dokument.IstFavorit });
        }
        [HttpGet("GetWorkflows")]
        public async Task<IActionResult> GetWorkflows()
        {
            // Demo: Dummy-Daten – hier ggf. DB-Abfrage nach offenen Workflows für den angemeldeten User!
            var userId = _userManager.GetUserId(User);
            var step = await _context.Steps
                .Where(x => x.UserId == userId && x.Completed == false && x.TaskCreated == true)
                .ToListAsync();                

            return Json(step);
        }
        [HttpGet("GetFavoritenDokumente")]
        public async Task<IActionResult> GetFavoritenDokumente()
        {
            var userId = _userManager.GetUserId(User);

            var favoriten = await _context.UserFavoritDokumente
                .Include(u => u.Dokument)
                .Where(u => u.ApplicationUserId == userId)
                .Select(u => new
                {
                    titel = u.Dokument.Titel
                })
                .ToListAsync();

            var favNotes = await _context.UserFavoritNote
                .Include(f => f.Notiz)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.HinzugefuegtAm)
                .Select(f => new {
                    id = f.NotizId,
                    titel = f.Notiz.Titel,
                    datum = f.HinzugefuegtAm
                })
                .ToListAsync();
            return Json( new { favoriten, favNotes } );
        }
        
        [HttpGet("GetSharedDocs")]

        public async Task<IActionResult> GetSharedDocs()
        {
            var userId = _userManager.GetUserId(User);

            // Dokumente, die DU empfangen hast
            var receivedDocs = await _context.UserSharedDocuments
                .Where(x => x.SharedToUserId == userId)
                .Include(x => x.Dokument)
                .Include(x => x.SharedByUser)
                .OrderByDescending(x => x.SharedAt)
                .Select(x => new {
                    documentTitle = x.Dokument.Titel,
                    sharedBy = x.SharedByUser.Vorname + " " + x.SharedByUser.Nachname,
                    sharedAt = x.SharedAt
                })
                .ToListAsync();

            // Dokumente, die DU geteilt hast
            var sharedByYou = await _context.UserSharedDocuments
                .Where(x => x.SharedByUserId == userId)
                .Include(x => x.Dokument)
                .Include(x => x.SharedToUser)
                .OrderByDescending(x => x.SharedAt)
                .Select(x => new {
                    documentTitle = x.Dokument.Titel,
                    sharedTo = x.SharedToUser.Vorname + " " + x.SharedToUser.Nachname,
                    sharedAt = x.SharedAt
                })
                .ToListAsync();

            // Notizen, die DU empfangen hast
            var receivedNotes = await _context.UserSharedNotes
                .Where(x => x.SharedToUserId == userId)
                .Include(x => x.Notiz)
                .Include(x => x.SharedByUser)
                .OrderByDescending(x => x.SharedAt)
                .Select(x => new {
                    noteId = x.NotizId,
                    noteTitle = x.Notiz.Titel,
                    sharedBy = x.SharedByUser.Vorname + " " + x.SharedByUser.Nachname,
                    sharedAt = x.SharedAt
                })
                .ToListAsync();

            // Notizen, die DU geteilt hast
            var sharedNotesByYou = await _context.UserSharedNotes
                .Where(x => x.SharedByUserId == userId)
                .Include(x => x.Notiz)
                .Include(x => x.SharedToUser)
                .OrderByDescending(x => x.SharedAt)
                .Select(x => new {
                    noteId = x.NotizId,
                    noteTitle = x.Notiz.Titel,
                    sharedTo = x.SharedToUser.Vorname + " " + x.SharedToUser.Nachname,
                    sharedAt = x.SharedAt
                })
                .ToListAsync();

            return Json(new { receivedDocs, sharedByYou, receivedNotes, sharedNotesByYou });
        }
        [HttpGet("IsAdmin")]
        public async Task<IActionResult> IsAdmin()
        {
            var user = await _userManager.GetUserAsync(User);
           
            bool isAdmin = user != null && 
              (await _userManager.IsInRoleAsync(user, "Admin") 
               || await _userManager.IsInRoleAsync(user, "SuperAdmin"));

            return Json(new { isAdmin });
        }

    }
}
