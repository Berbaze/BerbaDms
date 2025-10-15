using System;
using DmsProjeckt.Data;
using DmsProjeckt.Service;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Pages.Tests
{
    public class AufgabenModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        [BindProperty]
        public Aufgaben NeueAufgabe { get; set; } = new();
        public List<Aufgaben> AlleAufgaben { get; set; } = new();
        public List<ApplicationUser> BenutzerListe { get; set; } = new();
        public List<Aufgaben> AufgabenVonMir { get; set; } = new();
        private readonly AuditLogService _auditLogService;
        private readonly EmailService _emailService;
        private readonly FirebaseStorageService _storageService;
        public AufgabenModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AuditLogService auditLogService, EmailService emailService, FirebaseStorageService storageService)
        {
            _context = context;
            _userManager = userManager;
            _auditLogService = auditLogService;
            _emailService = emailService;
            _storageService = storageService;
        }
        public Dokumente VorgewaehltesDokument { get; set; }
        public Guid? VorgewaehltesDokumentId { get; set; }
        public async Task OnGetAsync(Guid? dokumentId)
        {
            var user = await _userManager.GetUserAsync(User);
            var kundenNr = user.AdminId;





            BenutzerListe = await _userManager.Users
                .Where(u => u.AdminId == kundenNr && u.Id != user.Id)
                .ToListAsync();

            AlleAufgaben = await _context.Aufgaben
    .Where(a => a.FuerUser != null && a.FuerUser == user.Id && a.Aktiv)
    .Include(a => a.VonUserNavigation)
    .Include(a => a.FuerUserNavigation)
    .Include(a => a.Dateien)
    .Include(a => a.Workflow)
        
    .ToListAsync();


            AufgabenVonMir = await _context.Aufgaben
                .Where(a => a.VonUser == user.Id)
                .Include(a => a.VonUserNavigation)
                .Include(a => a.FuerUserNavigation)
                .Include(a => a.Dateien)
                .Include(a => a.Workflow)
                    
                .ToListAsync();
            if (dokumentId.HasValue)
            {
                VorgewaehltesDokument = await _context.Dokumente
                    .FirstOrDefaultAsync(d => d.Id == dokumentId.Value && d.KundeId == kundenNr);
                VorgewaehltesDokumentId = dokumentId.Value;

                // ⚡ Modal direkt öffnen
                ViewData["OpenAufgabeModal"] = true;
            }

            NeueAufgabe.FaelligBis = DateTime.UtcNow; // 👈 Standardwert setzen


        }

        [BindProperty]
        public DateTime FaelligDatum { get; set; }

        [BindProperty]
        public TimeSpan FaelligUhrzeit { get; set; }

        public async Task<IActionResult> OnPostErstellenAsync(List<IFormFile> Dateien, Guid? DokumentId)
        {
            Console.WriteLine("ONPOSTERSTELLEN TATSÄCHLICH AUFGERUFEN");
            var user = await _userManager.GetUserAsync(User);
            NeueAufgabe.VonUser = user.Id;
            NeueAufgabe.UserId = user.Id;
            NeueAufgabe.Aktiv = true;
            NeueAufgabe.FaelligBis = FaelligDatum.Date + FaelligUhrzeit;
            if (NeueAufgabe.FaelligBis == default)
                NeueAufgabe.FaelligBis = DateTime.UtcNow;
            var Dateipfad = string.Empty;
            _context.Aufgaben.Add(NeueAufgabe);
            await _context.SaveChangesAsync();
            
            if(DokumentId.HasValue)
            {
                var dokument = await _context.Dokumente.FirstOrDefaultAsync(d => d.Id == DokumentId.Value);
                    if(dokument != null)
                {
                    dokument.AufgabeId = NeueAufgabe.Id;
                    await _context.SaveChangesAsync();
                }
            }
            if (Dateien != null && Dateien.Any())
            {
                var kundeBenutzer = await _context.KundeBenutzer
                    .FirstOrDefaultAsync(k => k.ApplicationUserId == user.Id);

                var kat = "Aufgabe";
                var abteilung = string.Empty;

                foreach (var datei in Dateien)
                {
                    if (datei == null || datei.Length == 0)
                    {
                        TempData["Error"] = "❌ Keine Datei vorhanden.";
                        return Page();
                    }

                    string dateipfad;
                    try
                    {
                        using var stream = datei.OpenReadStream();

                        dateipfad = await _storageService.UploadForUserAsync(
                            stream,
                            datei.FileName,
                            user.FirmenName,
                            abteilung,
                            kat,
                            false
                        );

                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"❌ Upload zu Firebase fehlgeschlagen: {ex.Message}";
                        return Page();
                    }

                    // 1️⃣ Originaldokument speichern (ohne Aufgabe)
                    var originalDokument = new Dokumente
                    {
                        Titel = datei.FileName,
                        Dateiname = datei.FileName,
                        Dateipfad = dateipfad,
                        HochgeladenAm = DateTime.UtcNow,
                        Kategorie = kat,
                        KundeId = kundeBenutzer?.KundenId ?? 0,
                        ApplicationUserId = user.Id,
                        AufgabeId = null, // Original ist NICHT an Aufgabe gebunden
                        OriginalId = null,
                        ObjectPath = dateipfad.Replace($"https://storage.googleapis.com/{_storageService.Bucket}/", "")
                    };

                    _context.Dokumente.Add(originalDokument);
                    await _context.SaveChangesAsync(); // Damit OriginalDokument eine Id hat!
                    kat = "versionen";
                    dateipfad = "";
                    try
                    {
                        using var stream = datei.OpenReadStream();

                        dateipfad = await _storageService.UploadForUserAsync(
                            stream,
                            datei.FileName,
                            user.FirmenName,
                            abteilung,
                            kat,
                            false
                        );

                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"❌ Upload zu Firebase fehlgeschlagen: {ex.Message}";
                        return Page();
                    }
                    // 2️⃣ Erste Version speichern (mit Aufgabe + OriginalId)
                    var ersteVersion = new Dokumente
                    {
                        Titel = datei.FileName + "_1",
                        Dateiname = datei.FileName ,
                        Dateipfad = dateipfad,
                        HochgeladenAm = DateTime.UtcNow,
                        Kategorie = kat,
                        KundeId = kundeBenutzer?.KundenId ?? 0,
                        ApplicationUserId = user.Id,
                        AufgabeId = NeueAufgabe.Id,
                        IsVersion = true,
                        OriginalId = originalDokument.Id, // Verknüpfung zum Original
                        ObjectPath = dateipfad.Replace($"https://storage.googleapis.com/{_storageService.Bucket}/", "")
                    };

                    _context.Dokumente.Add(ersteVersion);
                    await _context.SaveChangesAsync();
                }
            }

            var notifType = await _context.NotificationTypes.FirstOrDefaultAsync(n => n.Name == "Erstellt");
            var notifTypeEmail = await _context.NotificationTypes.FirstOrDefaultAsync(n => n.Name == "ErstelltEmail");

            if (notifType == null) return Page(); // defensiv

            // Einstellungen prüfen
            var setting = await _context.UserNotificationSettings
                .FirstOrDefaultAsync(s => s.UserId == NeueAufgabe.FuerUser && s.NotificationTypeId == notifType.Id);

            var settingEmail = await _context.UserNotificationSettings
                .FirstOrDefaultAsync(s => s.UserId == NeueAufgabe.FuerUser && s.NotificationTypeId == notifTypeEmail.Id);

            // 1. InApp Notification
            if (setting == null || setting.Enabled)
            {
                var notification = new Notification
                {
                    Title = "Neue Aufgabe zugewiesen",
                    Content = "Du hast eine neue Aufgabe erhalten.",
                    CreatedAt = DateTime.UtcNow,
                    NotificationTypeId = notifType.Id,
                    ActionLink = "/Tests/Aufgaben"
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                var userNotification = new UserNotification
                {
                    UserId = NeueAufgabe.FuerUser,
                    NotificationId = notification.Id,
                    IsRead = false,
                    ReceivedAt = DateTime.UtcNow
                };
                _context.UserNotifications.Add(userNotification);
                await _context.SaveChangesAsync();
            }

            // 2. E-Mail Notification
            if ((settingEmail == null || settingEmail.Enabled) && notifTypeEmail != null)
            {
                user = await _context.Users.FindAsync(NeueAufgabe.FuerUser);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    string subject = "Neue Aufgabe zugewiesen";
                    string body = $"Hallo {user.Vorname},<br>du hast eine neue Aufgabe erhalten.<br><a href=\"https://localhost:7074/Tests/Aufgaben\">Zur Aufgabe</a>";
                    await _emailService.SendEmailAsync(user.Email, subject, body);
                }
                Console.WriteLine("Email versendet");
            }
            Console.WriteLine($"Erhaltene Dateien: {Dateien?.Count ?? 0}");
            await _auditLogService.LogActionOnlyAsync($"Aufgabe \"{NeueAufgabe.Titel}\" ({NeueAufgabe.Id}) erstellt", user.Id);
            return RedirectToPage();
        }




        public async Task<IActionResult> OnPostErledigt([FromForm] int id)
        {


            var userId = _userManager.GetUserId(User);
            var aufgabe = await _context.Aufgaben
                .Include(a => a.StepNavigation)
                .ThenInclude(s => s.Workflow)
                .FirstOrDefaultAsync(a => a.Id == id);
            Console.WriteLine("🔥 OnPostErledigt ausgelöst mit ID: " + id);

            Console.WriteLine($"👉 Erledigt-Handler aufgerufen mit id={id}");





            if (aufgabe == null)
            {
                Console.WriteLine("❌ Aufgabe nicht gefunden!");
                return new BadRequestResult();
            }

            if (aufgabe.FuerUser != userId)
            {
                Console.WriteLine("❌ Zugriff verweigert – nicht dein Task!");
                return new BadRequestResult();
            }

            aufgabe.Erledigt = true;
            if (aufgabe.StepId == null)
            {
                await _auditLogService.LogActionOnlyAsync($"Aufgabe \"{aufgabe.Titel}\" ({aufgabe.Id}) erledigt", userId);
                var notificationType = await _context.NotificationTypes
                    .FirstOrDefaultAsync(n => n.Name == "Erledigt");
                var setting = await _context.UserNotificationSettings
                .FirstOrDefaultAsync(s => s.UserId == aufgabe.VonUser && s.NotificationTypeId == notificationType.Id);

                var notificationTypeEmail = await _context.NotificationTypes
                    .FirstOrDefaultAsync(n => n.Name == "ErledigtEmail");
                var settingsEmail = await _context.UserNotificationSettings
                    .FirstOrDefaultAsync(s => s.UserId == aufgabe.VonUser && s.NotificationTypeId == notificationTypeEmail.Id);
                if (setting == null || setting.Enabled)
                {

                    var notification = new Notification
                    {
                        Title = "Aufgabe erledigt",
                        Content = "Eine von dir erstellte Aufgabe wurde erledigt.",
                        CreatedAt = DateTime.UtcNow,
                        NotificationTypeId = notificationType.Id,
                        ActionLink = "/Tests/Aufgaben"
                    };
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    var userNotification = new UserNotification
                    {
                        UserId = aufgabe.VonUser,
                        NotificationId = notification.Id,
                        IsRead = false,
                        ReceivedAt = DateTime.UtcNow
                    };
                    _context.UserNotifications.Add(userNotification);
                    await _context.SaveChangesAsync();
                }
                if(settingsEmail == null || settingsEmail.Enabled)
                {
                    var userTo = await _context.Users.FindAsync(aufgabe.VonUser);
                    string subject = "Aufgabe erledigt";
                    string body = $@"
                <p>Hallo {userTo.Vorname},</p>
                <p>Die Aufgabe <b>""{aufgabe.Titel}""</b> erledigt.</p>
                < p >< a href = 'Tests/Aufgaben' > Details ansehen </ a ></ p >
                < p > Viele Grüße,< br /> Dein Team </ p > ";

            await _emailService.SendEmailAsync(userTo.Email, subject, body);
                }
                var erstelltType = await _context.NotificationTypes.FirstOrDefaultAsync(n => n.Name == "Erstellt");

                if (erstelltType != null)
                {
                    // Finde alle "Neue Aufgabe zugewiesen"-Notifications für diese Aufgabe und diesen User, die noch nicht gelesen sind
                    var userNotifications = await _context.UserNotifications
                        .Include(un => un.Notification)
                        .Where(un =>
                            un.UserId == aufgabe.FuerUser &&
                            !un.IsRead &&
                            un.Notification.NotificationTypeId == erstelltType.Id)
                        .OrderByDescending(un => un.ReceivedAt)
                        .ToListAsync();

                    // Da du vermutlich pro Aufgabe/Benutzer eine Notification hast, reicht meist FirstOrDefault
                    var ungelesen = userNotifications.FirstOrDefault();
                    if (ungelesen != null)
                    {
                        ungelesen.IsRead = true;
                        await _context.SaveChangesAsync();
                    }
                }
                Console.WriteLine($"Aufgabe erledigt {aufgabe.Titel}");
            }
            else
            {
                await _auditLogService.LogActionOnlyAsync($"Schritt {aufgabe.StepNavigation.Order + 1} in Workflow \"{aufgabe.StepNavigation.Workflow.Title}\" ({aufgabe.StepNavigation.WorkflowId}) erledigt", aufgabe.FuerUser);
                Console.WriteLine("Log versucht");
            }
            // usw...
          

            if (aufgabe.StepNavigation != null)
            {
                var currentStep = aufgabe.StepNavigation;
                currentStep.Completed = true;

                var nextStep = await _context.Steps
                    .Where(s => s.WorkflowId == currentStep.WorkflowId &&
                                s.Order == currentStep.Order + 1)
                    .FirstOrDefaultAsync();

                if (nextStep != null && !nextStep.TaskCreated && !string.IsNullOrWhiteSpace(nextStep.UserId))
                {
                    var neueAufgabe = new Aufgaben
                    {
                        Titel = nextStep.Title,
                        Beschreibung = nextStep.Description,
                        FaelligBis = nextStep.DueDate ?? DateTime.Today.AddDays(3),
                        Prioritaet = 1,
                        VonUser = aufgabe.VonUser,
                        FuerUser = nextStep.UserId,
                        Erledigt = false,
                        ErstelltAm = DateTime.Now,
                        StepId = nextStep.Id
                    };
                    var notificationType = await _context.NotificationTypes
         .FirstOrDefaultAsync(n => n.Name == "Workflowaufgabe");
                    if (notificationType == null)
                    {
                        Console.WriteLine("❌ NotificationType 'Workflowaufgabe' fehlt!");
                    }
                    else
                    {
                        var setting = await _context.UserNotificationSettings
                            .FirstOrDefaultAsync(s => s.UserId == neueAufgabe.FuerUser && s.NotificationTypeId == notificationType.Id);

                        if (setting == null || setting.Enabled)
                        {
                            var notification = new Notification
                            {
                                Title = "Neue Aufgabe zugewiesen",
                                Content = $"Du hast eine neue Aufgabe im Workflow \"{currentStep.Workflow.Title}\" erhalten.",
                                CreatedAt = DateTime.UtcNow,
                                NotificationTypeId = notificationType.Id
                            };
                            _context.Notifications.Add(notification);
                            await _context.SaveChangesAsync();

                            var userNotification = new UserNotification
                            {
                                UserId = nextStep.UserId,
                                NotificationId = notification.Id,
                                IsRead = false,
                                ReceivedAt = DateTime.UtcNow
                            };
                            _context.UserNotifications.Add(userNotification);
                            await _context.SaveChangesAsync();
                        }
                    }
                    _context.Aufgaben.Add(neueAufgabe);
                    nextStep.TaskCreated = true;
                }
               var notificationType2 = await _context.NotificationTypes
                    .FirstOrDefaultAsync(n => n.Name == "Workflow erledigt");
                var setting2 = await _context.UserNotificationSettings
                .FirstOrDefaultAsync(s => s.UserId == currentStep.UserId && s.NotificationTypeId == notificationType2.Id);
                if (setting2 == null || setting2.Enabled)
                {

                    var notification = new Notification
                    {
                        Title = "Aufgabe erledigt",
                        Content = $"Im von dir erstellten Workflow \"{ currentStep.Workflow.Title }\" wurde Aufgabe {currentStep.Order +1 } erledigt.",
                        CreatedAt = DateTime.UtcNow,
                        NotificationTypeId = notificationType2.Id
                    };
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    var userNotification = new UserNotification
                    {
                        UserId = currentStep.UserId,
                        NotificationId = notification.Id,
                        IsRead = false,
                        ReceivedAt = DateTime.UtcNow
                    };
                    _context.UserNotifications.Add(userNotification);
                    await _context.SaveChangesAsync();
                }

            }
            await _context.SaveChangesAsync();
            return new OkResult();
        }
        [BindProperty]
        public int Id { get; set; }

        public IActionResult OnPostLoeschen(int id)
        {
            var aufgabe = _context.Aufgaben
                .Include(a => a.Dateien) // falls Navigation vorhanden
                .FirstOrDefault(a => a.Id == id);

            if (aufgabe != null)
            {
                // Referenz bei allen Dokumenten entfernen
                var dokumente = _context.Dokumente.Where(d => d.AufgabeId == id).ToList();
                foreach (var d in dokumente)
                {
                    d.AufgabeId = null;
                }

                _context.Aufgaben.Remove(aufgabe);
                _context.SaveChanges();
            }

            return RedirectToPage();
        }




    }
}
