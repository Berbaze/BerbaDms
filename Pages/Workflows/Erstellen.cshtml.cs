using DmsProjeckt.Data;

using DmsProjeckt.Service;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Pages.Workflows
{
    public class ErstellenModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FirebaseStorageService _firebaseStorageService;
        private readonly AuditLogService _auditLogService;
        public ErstellenModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AuditLogService auditLogService, FirebaseStorageService firebaseStorageService)
        {
            _context = context;
            _userManager = userManager;
            _firebaseStorageService = firebaseStorageService;
            _auditLogService = auditLogService;
        }

        [BindProperty]
        public Workflow Workflow { get; set; }

        [BindProperty]
        public List<Step> Steps { get; set; } = new();

        public List<SelectListItem> UserOptions { get; set; }

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            UserOptions = await _context.Users
                .Where(u => u.Id != currentUser.Id)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.Vorname} {u.Nachname}"
                })
                .ToListAsync();


        }

        public async Task<IActionResult> OnPostAsync(List<IFormFile> Dateien)
        {
            var Dateipfad = string.Empty;
            Console.WriteLine("OnPostAsync triggered");

            UserOptions = await _context.Users
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.Email
                })
                .ToListAsync();

            Workflow.CreatedAt = DateTime.UtcNow;
            Workflow.LastModified = DateTime.UtcNow;
            Workflow.UserId = _userManager.GetUserId(User);

            // Step-Zuweisung
            for (int i = 0; i < Steps.Count; i++)
            {
                Steps[i].Order = i;
                Steps[i].Workflow = Workflow;
            }

            _context.Workflows.Add(Workflow);
            _context.Steps.AddRange(Steps);
            await _context.SaveChangesAsync(); // IDs vorhanden
            var workflowDateien = new List<Dokumente>();
            var user = await _userManager.GetUserAsync(User);
            var abteiltung = string.Empty;
            if (Dateien != null && Dateien.Any())
            {
                var kundeBenutzer = await _context.KundeBenutzer
                    .FirstOrDefaultAsync(k => k.ApplicationUserId == Workflow.UserId);

                foreach (var datei in Dateien)
                {
                    if (datei == null || datei.Length == 0)
                    {
                        TempData["Error"] = "❌ Leere Datei.";
                        return Page();
                    }

                    try
                    {
                        // === Original speichern ===
                        var kat = "workflow";
                        var originalPath = await _firebaseStorageService.UploadForUserAsync(
                            datei,
                            user.FirmenName,
                            abteiltung,
                            kat
                        );


                        var original = new Dokumente
                        {
                            Id = Guid.NewGuid(),
                            Titel = Path.GetFileNameWithoutExtension(datei.FileName),
                            Dateiname = datei.FileName,
                            Dateipfad = originalPath,
                            ObjectPath = originalPath.Replace($"https://storage.googleapis.com/{_firebaseStorageService.Bucket}/", ""),
                            HochgeladenAm = DateTime.UtcNow,
                            Kategorie = "Workflow",
                            ErkannteKategorie = kat,
                            KundeId = kundeBenutzer?.KundenId ?? 0,
                            ApplicationUserId = Workflow.UserId,
                            WorkflowId = Workflow.Id,
                            IsVersion = false
                        };

                        // === Erste Version speichern ===
                        string versionDir = Path.Combine("dokumente", user.FirmenName.ToLower(), "workflow", "versionen").Replace("\\", "/");
                        string versionPath = Path.Combine(versionDir, datei.FileName).Replace("\\", "/");

                        using (var stream = datei.OpenReadStream())
                        {
                            await _firebaseStorageService.UploadStreamAsync(stream, versionPath, datei.ContentType);
                        }

                        var version = new Dokumente
                        {
                            Id = Guid.NewGuid(),
                            Titel = Path.GetFileNameWithoutExtension(datei.FileName),
                            Dateiname = datei.FileName,
                            Dateipfad = versionPath,
                            ObjectPath = versionPath,
                            HochgeladenAm = DateTime.UtcNow,
                            Kategorie = "Workflow",
                            ErkannteKategorie = kat,
                            KundeId = kundeBenutzer?.KundenId ?? 0,
                            ApplicationUserId = Workflow.UserId,
                            WorkflowId = Workflow.Id,
                            IsVersion = true,
                            OriginalId = original.Id
                        };

                        _context.Dokumente.Add(original);
                        _context.Dokumente.Add(version);
                        workflowDateien.Add(original);
                        workflowDateien.Add(version);
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"❌ Upload zu Firebase fehlgeschlagen: {ex.Message}";
                        return Page();
                    }
                }

                await _context.SaveChangesAsync();
            }



            // 🔥 Nur der erste zugewiesene Step (der früheste mit User) erhält eine Aufgabe
            // 🔥 Für alle Steps mit UserId eine Aufgabe erstellen
            var aufgabenListe = new List<Aufgaben>();
            foreach (var step in Steps.Where(s => !string.IsNullOrWhiteSpace(s.UserId)))
            {
                var aufgabe = new Aufgaben
                {
                    Titel = step.Kategorie,
                    Beschreibung = step.Description,
                    FaelligBis = step.DueDate ?? DateTime.Today.AddDays(3),
                    Prioritaet = 1,
                    VonUser = Workflow.UserId,
                    FuerUser = step.UserId,
                    Erledigt = false,
                    Aktiv = (step.Order == 0), // ✅ Nur Schritt 0 ist aktiv
                    ErstelltAm = DateTime.UtcNow,
                    StepId = step.Id,
                    WorkflowId = Workflow.Id
                };
                aufgabenListe.Add(aufgabe);
            }

            _context.Aufgaben.AddRange(aufgabenListe);
            await _context.SaveChangesAsync();

            // Verknüpfe AufgabeId mit Step
            foreach (var aufgabe in aufgabenListe)
            {
                var step = Steps.FirstOrDefault(s => s.Id == aufgabe.StepId);
                if (step != null)
                {
                    step.TaskCreated = true;

                }
            }
            if (workflowDateien.Any())
            {
                await _auditLogService.LogActionOnlyAsync($"Workflow \"{Workflow.Title}\" ({Workflow.Id}) mit {workflowDateien.Count} Dokument erstellt",
                    Workflow.UserId);
            }
            else
            {
                await _auditLogService.LogActionOnlyAsync($"Workflow \"{Workflow.Title}\" ({Workflow.Id}) erstellt", Workflow.UserId);
            }
            _context.Steps.UpdateRange(Steps);
            await _context.SaveChangesAsync();

            // 1. Finde Step mit Order == 0 und UserId gesetzt
            var ersterStep = Steps.FirstOrDefault(s => s.Order == 0 && !string.IsNullOrWhiteSpace(s.UserId));
            if (ersterStep != null)
            {
                var notificationType = await _context.NotificationTypes
                    .FirstOrDefaultAsync(n => n.Name == "Workflowaufgabe");
                if (notificationType != null)
                {
                    // 2. Prüfe User-Einstellung für Benachrichtigung
                    var setting = await _context.UserNotificationSettings
                        .FirstOrDefaultAsync(s => s.UserId == ersterStep.UserId && s.NotificationTypeId == notificationType.Id);

                    if (setting == null || setting.Enabled)
                    {
                        // 3. Notification erstellen
                        var notification = new Notification
                        {
                            Title = "Neue Aufgabe zugewiesen",
                            Content = $"Du hast eine neue Aufgabe im Workflow \"{Workflow.Title}\" erhalten.",
                            CreatedAt = DateTime.UtcNow,
                            NotificationTypeId = notificationType.Id,
                            ActionLink = $"/Workflows/StepDetail/{ersterStep.WorkflowId}/{ersterStep.Id}"
                        };
                        _context.Notifications.Add(notification);
                        await _context.SaveChangesAsync();

                        // 4. UserNotification für den User anlegen
                        var userNotification = new UserNotification
                        {
                            UserId = ersterStep.UserId,
                            NotificationId = notification.Id,
                            IsRead = false,
                            ReceivedAt = DateTime.UtcNow
                            
                        };
                        _context.UserNotifications.Add(userNotification);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return RedirectToPage("Index");
        }
        public class UserOptionDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string ProfileImageUrl { get; set; }
            public string Abteilung { get; set; }
        }

        public class AbteilungDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        // 🔍 User-Suche
        public async Task<IActionResult> OnGetSearch(string term)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return new JsonResult(new { success = false });

            term = term?.Trim().ToLower() ?? "";

            // Nur Benutzer aus derselben Firma, außer dich selbst
            var users = await _context.Users
                .Where(u => u.FirmenName == currentUser.FirmenName && u.Id != currentUser.Id)
                .Where(u => string.IsNullOrEmpty(term) ||
                            u.Vorname.ToLower().Contains(term) ||
                            u.Nachname.ToLower().Contains(term) ||
                            u.Email.ToLower().Contains(term))
                .Select(u => new
                {
                    id = u.Id,
                    name = u.Vorname + " " + u.Nachname,
                    profileImageUrl = string.IsNullOrEmpty(u.ProfilbildUrl)
                        ? "/images/default-profile.png"
                        : u.ProfilbildUrl
                })
                .Take(10)
                .ToListAsync();

            // Abteilungen aus derselben Firma
            var abteilungen = await _context.Abteilungen
               
                .Where(a => string.IsNullOrEmpty(term) || a.Name.ToLower().Contains(term))
                .Select(a => new { id = a.Id, name = a.Name })
                .Take(10)
                .ToListAsync();

            return new JsonResult(new { success = true, users, abteilungen });
        }


        public async Task<JsonResult> OnGetUsersByAbteilungAsync(int abteilungId)
        {
            var users = await _context.Users
                .Where(u => u.AbteilungId == abteilungId)
                .Select(u => new {
                    id = u.Id,
                    name = u.Vorname + " " + u.Nachname,
                    profileImageUrl = !string.IsNullOrEmpty(u.ProfilbildUrl) ? u.ProfilbildUrl : "/images/default-profile.png"
                })
                .ToListAsync();

            return new JsonResult(new { success = true, users });
        }




    }
}
