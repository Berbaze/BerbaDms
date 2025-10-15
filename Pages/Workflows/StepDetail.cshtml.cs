using System.Security.Claims;
using DmsProjeckt.Data;
using DmsProjeckt.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class StepDetailModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuditLogService _auditLogService;
    private readonly FirebaseStorageService _firebaseStorage;
    public StepDetailModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, AuditLogService auditLogService, FirebaseStorageService firebaseStorage)
    {
        _db = db;
        _userManager = userManager;
        _auditLogService = auditLogService;
        _firebaseStorage = firebaseStorage;
    }

    [BindProperty]
    public StepDetailViewModel VM { get; set; } = new();

    [BindProperty]
    public string Kommentar { get; set; }

    [BindProperty]
    public int StepId { get; set; }
    public string CurrentUserId { get; set; }
    public int? OffenesKommentarStepId { get; set; }
    public async Task<IActionResult> OnGetAsync(int workflowId, int stepId)
    {
        // 🔹 Aktuellen Benutzer laden
        CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 🔹 Workflow + Steps laden
        var workflow = await _db.Workflows
            .Include(w => w.Steps.OrderBy(s => s.Order))
            .ThenInclude(s => s.AssignedToUser)
            .Include(w => w.CreatedByUser)
            .FirstOrDefaultAsync(w => w.Id == workflowId);

        if (workflow == null)
            return NotFound();

        // 🔹 Steps sortieren
        var steps = workflow.Steps.OrderBy(s => s.Order).ToList();
        var stepIds = steps.Select(s => s.Id).ToList();

        // 🔹 Workflow-Dokumente (nur Versionen, keine Originale)
        var alledokumente = await _db.Dokumente
            .Where(d => d.WorkflowId == workflowId && d.IsVersion)
            .ToListAsync();

        // 🔹 Step-Dokumente (mit StepId vorhanden)
        var dokumente = await _db.Dokumente
            .Where(d => d.WorkflowId == workflowId && d.StepId != null && stepIds.Contains((int)d.StepId))
            .ToListAsync();

        // ✅ Dictionary<int, List<Dokumente>> mit Null-Handling
        var dokDict = dokumente
            .GroupBy(d => d.StepId ?? 0) // Null = 0 = workflowweite Dokumente
            .ToDictionary(g => g.Key, g => g.ToList());

        // 🔹 Step-Kommentare inkl. Benutzer laden
        var kommentare = await _db.StepKommentare
            .Where(k => stepIds.Contains(k.StepId))
            .Include(k => k.User)
            .ToListAsync();

        // ✅ Dictionary<int, List<StepKommentar>>
        var kommDict = kommentare
            .GroupBy(k => k.StepId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 🔹 ViewModel befüllen
        VM.Workflow = workflow;
        VM.Steps = steps;
        VM.StepDokumente = dokDict;
        VM.StepKommentare = kommDict;
        VM.AktuellerStepId = stepId;
        VM.AktuellerUserId = _userManager.GetUserId(User);
        VM.Dokumente = alledokumente;

        return Page();
    }

    // Kommentar speichern
    public async Task<IActionResult> OnPostAsync(int workflowId, int stepId)
    {
        if (string.IsNullOrWhiteSpace(Kommentar))
            return RedirectToPage(new { workflowId, stepId });
        OffenesKommentarStepId = stepId;
        var user = await _userManager.GetUserAsync(User);
        var kommentar = new StepKommentar
        {
            StepId = stepId,
            UserId = user.Id,
            UserName = user.Vorname + " " + user.Nachname,
            Text = Kommentar,
            CreatedAt = DateTime.UtcNow
        };
        _db.StepKommentare.Add(kommentar);
        await _db.SaveChangesAsync();

        return RedirectToPage(new { workflowId, stepId });
    }

    // Step erledigen
    public async Task<IActionResult> OnPostErledigenAsync(int workflowId, int stepId)
    {
        var step = await _db.Steps.FindAsync(stepId);
        if (step == null) return NotFound();
        var workflow = await _db.Workflows.FindAsync(workflowId);
        step.Completed = true;
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (step.UserId != currentUserId) return Forbid();
        // Die zugehörige Aufgabe (optional) auch erledigen
        var aufgabe = await _db.Aufgaben.FirstOrDefaultAsync(a => a.StepId == stepId);
        if (aufgabe != null)
        {
            aufgabe.Erledigt = true;
            
        }

        // Optional: Nächste Aufgabe aktivieren
        var nextStep = await _db.Steps
            .Where(s => s.WorkflowId == step.WorkflowId && s.Order == step.Order + 1)
            .FirstOrDefaultAsync();

        if (nextStep != null && !nextStep.TaskCreated && !string.IsNullOrEmpty(nextStep.UserId))
        {
            var neueAufgabe = new Aufgaben
            {
                Titel = nextStep.Title,
                Beschreibung = nextStep.Description,
                FaelligBis = nextStep.DueDate ?? DateTime.Today.AddDays(3),
                Prioritaet = 1,
                VonUser = aufgabe?.VonUser,
                FuerUser = nextStep.UserId,
                Erledigt = false,
                ErstelltAm = DateTime.Now,
                StepId = nextStep.Id,
                Aktiv = true
            };

            _db.Aufgaben.Add(neueAufgabe);
            nextStep.TaskCreated = true;
        }
        else if (nextStep != null && nextStep.TaskCreated && !string.IsNullOrWhiteSpace(nextStep.UserId))
        {
            var nextAufgabe = await _db.Aufgaben
                .Where(a => a.StepId == nextStep.Id && a.WorkflowId == nextStep.WorkflowId && a.Aktiv == false && a.Erledigt == false)
                .FirstOrDefaultAsync();

            nextAufgabe.Aktiv = true;
            _db.Update(nextAufgabe);
            
        }
        var notificationType = await _db.NotificationTypes
         .FirstOrDefaultAsync(n => n.Name == "Workflowaufgabe");
        if (notificationType == null)
        {
            Console.WriteLine("❌ NotificationType 'Workflowaufgabe' fehlt!");
        }
        else
        {
            if (nextStep != null)
            {
                var setting = await _db.UserNotificationSettings
                    .FirstOrDefaultAsync(s => s.UserId == nextStep.UserId && s.NotificationTypeId == notificationType.Id);

                if (setting == null || setting.Enabled)
                {
                    var notification = new Notification
                    {
                        Title = "Neue Aufgabe zugewiesen",
                        Content = $"Du hast eine neue Aufgabe im Workflow \"{workflow.Title}\" erhalten.",
                        CreatedAt = DateTime.UtcNow,
                        NotificationTypeId = notificationType.Id
                    };
                    _db.Notifications.Add(notification);
                    await _db.SaveChangesAsync();

                    var userNotification = new UserNotification
                    {
                        UserId = nextStep.UserId,
                        NotificationId = notification.Id,
                        IsRead = false,
                        ReceivedAt = DateTime.UtcNow
                    };
                    _db.UserNotifications.Add(userNotification);
                    await _db.SaveChangesAsync();
                }
            }
           
        }
        var erstelltType = await _db.NotificationTypes.FirstOrDefaultAsync(n => n.Name == "Workflowaufgabe");

        if (erstelltType != null)
        {
            // Finde alle "Neue Aufgabe zugewiesen"-Notifications für diese Aufgabe und diesen User, die noch nicht gelesen sind
            var aufgabenNotification = await _db.UserNotifications
.Include(un => un.Notification)
.Where(un =>
 un.UserId == step.UserId &&
 !un.IsRead &&
 un.Notification.NotificationTypeId == erstelltType.Id)
.OrderBy(un => un.ReceivedAt)   // ÄLTESTE zuerst!
.FirstOrDefaultAsync();

            // Optional: Noch genauer nach Step filtern, falls im Content eindeutig
            if (aufgabenNotification != null)
            {
                aufgabenNotification.IsRead = true;
                await _db.SaveChangesAsync();
            }
        }
        var notificationType2 = await _db.NotificationTypes
                .FirstOrDefaultAsync(n => n.Name == "Workflow erledigt");
        var setting2 = await _db.UserNotificationSettings
        .FirstOrDefaultAsync(s => s.UserId == workflow.UserId && s.NotificationTypeId == notificationType2.Id);
        if (setting2 == null || setting2.Enabled)
        {

            var notification = new Notification
            {
                Title = "Aufgabe erledigt",
                Content = $"Im von dir erstellten Workflow \"{workflow.Title}\" wurde Aufgabe {step.Order + 1} erledigt.",
                CreatedAt = DateTime.UtcNow,
                NotificationTypeId = notificationType2.Id
            };
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            var userNotification = new UserNotification
            {
                UserId = workflow.UserId,
                NotificationId = notification.Id,
                IsRead = false,
                ReceivedAt = DateTime.UtcNow
            };
            _db.UserNotifications.Add(userNotification);
            await _db.SaveChangesAsync();

        }
        await _db.SaveChangesAsync();
        if(nextStep == null)
        {
            var notificationTypee = await _db.NotificationTypes
                   .FirstOrDefaultAsync(n => n.Name == "Workflow done");
            var setting = await _db.UserNotificationSettings
                .FirstOrDefaultAsync(s => s.UserId == workflow.UserId && s.NotificationTypeId == notificationTypee.Id);
            if (setting == null || setting.Enabled)
            {

                var notification = new Notification
                {
                    Title = "Workflow abgeschlossen",
                    Content = $"Der Workflow \"{workflow.Title}\" wurde erfolgreich abgeschlossen.",
                    CreatedAt = DateTime.UtcNow,
                    NotificationTypeId = notificationTypee.Id
                };
                _db.Notifications.Add(notification);
                await _db.SaveChangesAsync();

                var userNotification = new UserNotification
                {
                    UserId = workflow.UserId,
                    NotificationId = notification.Id,
                    IsRead = false,
                    ReceivedAt = DateTime.UtcNow
                };
                _db.UserNotifications.Add(userNotification);
                await _db.SaveChangesAsync();

            }
        }
        // Nach erledigen weiterleiten – z.B. zu Aufgabenübersicht
        return RedirectToPage("/Workflows/Index");
    }
    public async Task<IActionResult> OnPostUploadAsync(List<IFormFile> Dateien, int StepId)
    {
        var step = await _db.Steps
            .Include(s => s.Workflow)
            .FirstOrDefaultAsync(s => s.Id == StepId);

        if (step == null) return NotFound();
        var workflow = step.Workflow;
        var user = await _userManager.GetUserAsync(User);

        // KundenId bestimmen (deine bestehende Logik hier)
        int kundeId = await ErmittleKundenIdAsync(user);

        var abteilung = string.Empty;
        var dokumente = new List<Dokumente>();

        if (Dateien != null && Dateien.Any())
        {
            foreach (var datei in Dateien)
            {
                if (datei == null || datei.Length == 0) continue;

                var kat = step.Kategorie ?? "workflow";

                // === 1. Original speichern (ohne Step, nur einmalig) ===
                var originalPath = await _firebaseStorage.UploadForUserAsync(datei, user.FirmenName, abteilung, kat);

                var original = new Dokumente
                {
                    Id = Guid.NewGuid(),
                    Titel = Path.GetFileNameWithoutExtension(datei.FileName),
                    Dateiname = datei.FileName,
                    Dateipfad = originalPath,
                    ObjectPath = originalPath.Replace($"https://storage.googleapis.com/{_firebaseStorage.Bucket}/", ""),
                    HochgeladenAm = DateTime.UtcNow,
                    Kategorie = "Workflow",
                    ErkannteKategorie = kat,
                    KundeId = kundeId,
                    ApplicationUserId = user.Id,
                    WorkflowId = workflow.Id,
                    StepId = null, // 👈 Kein Step → Workflow-weit
                    IsVersion = false
                };

                // === 2. Version speichern (Workflow-weit sichtbar) ===
                string versionDir = Path.Combine("dokumente", user.FirmenName.ToLower(), "workflow", "versionen").Replace("\\", "/");
                string versionPath = Path.Combine(versionDir, datei.FileName).Replace("\\", "/");

                using (var stream = datei.OpenReadStream())
                {
                    await _firebaseStorage.UploadStreamAsync(stream, versionPath, datei.ContentType);
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
                    KundeId = kundeId,
                    ApplicationUserId = user.Id,
                    WorkflowId = workflow.Id,
                    StepId = null, // 👈 Workflow-weit gültig
                    IsVersion = true,
                    OriginalId = original.Id
                };

                _db.Dokumente.Add(original);
                _db.Dokumente.Add(version);
                dokumente.Add(original);
                dokumente.Add(version);
            }

            await _db.SaveChangesAsync();
        }

        if (dokumente.Any())
        {
            await _auditLogService.LogActionOnlyAsync(
                $"Dokument(e) für Workflow \"{workflow.Title}\" hochgeladen",
                user.Id
            );
        }

        return RedirectToPage(new { workflowId = workflow.Id, stepId = step.Id });
    }
    private async Task<int> ErmittleKundenIdAsync(ApplicationUser user)
    {
        // 1. Prüfen, ob der User selbst eine KundenId hat
        var kundeBenutzer = await _db.KundeBenutzer
            .FirstOrDefaultAsync(k => k.ApplicationUserId == user.Id);

        if (kundeBenutzer != null)
        {
            return kundeBenutzer.KundenId;
        }

        // 2. Falls nicht → über CreatedByAdminId den Admin suchen
        if (string.IsNullOrEmpty(user.CreatedByAdminId))
            throw new Exception("❌ Weder KundenId noch CreatedByAdminId beim User gesetzt.");

        var adminKunde = await _db.KundeBenutzer
            .FirstOrDefaultAsync(k => k.ApplicationUserId == user.CreatedByAdminId);

        if (adminKunde == null)
            throw new Exception("❌ Admin hat keine KundenId.");

        return adminKunde.KundenId;
    }


    // ViewModel
    public class StepDetailViewModel
    {
        public Workflow Workflow { get; set; }
        public List<Step> Steps { get; set; } = new();
        public Dictionary<int, List<Dokumente>> StepDokumente { get; set; } = new();
        public Dictionary<int, List<StepKommentar>> StepKommentare { get; set; } = new();
        public int AktuellerStepId { get; set; }
        public string AktuellerUserId { get; set; }
        public List<Dokumente> Dokumente { get; set; }
    }
}
