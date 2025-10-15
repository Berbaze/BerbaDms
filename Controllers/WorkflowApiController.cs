using System.Text;
using DmsProjeckt.Data;
using DmsProjeckt.Service;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace DmsProjeckt.Controllers
{
    [Route("api/workflow")]
    [ApiController]
    public class WorkflowApiController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;
        private readonly EmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        public WorkflowApiController(ApplicationDbContext context, AuditLogService auditLogService, EmailService emailService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditLogService = auditLogService;
            _emailService = emailService;
            _userManager = userManager;
        }

        [HttpGet("step/{id}")]
        public async Task<IActionResult> GetStep(int id)
        {
            var step = await _context.Steps
                .Include(s => s.Aufgaben)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (step == null)
                return NotFound();

            var status = "❌ Überfällig";
            var now = DateTime.Now;

            if (step.Aufgaben?.Any(a => a.Erledigt) == true)

            {
                status = "🟢 Erledigt";
            }
            else if (step.DueDate.HasValue && step.DueDate.Value >= now)
            {
                status = "⏳ Offen";
            }

            var html = $@"
    <h5 class='text-primary fw-bold'>{step.Kategorie}</h5>
    <hr />
    <div>
        <p><strong>Beschreibung:</strong><br />{step.Description}</p>
        <p><strong>Fällig am:</strong> 📅 {step.DueDate?.ToString("dd.MM.yyyy") ?? "-"}</p>
        <p><strong>Benutzer:</strong> 👤 {step.UserId}</p>
        <p><strong>Status:</strong> {status}</p>
    </div>
";


            return Content(html, "text/html");
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetWorkflow(int id)
        {
            var workflow = await _context.Workflows
                .Include(w => w.Steps)
                .ThenInclude(s => s.Aufgaben)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workflow == null)
                return NotFound();

            var sb = new StringBuilder();
            sb.AppendLine($"<h5>{workflow.Title}</h5>");
            sb.AppendLine($"<p>{workflow.Description}</p>");
            sb.AppendLine("<hr><ul>");
            foreach (var s in workflow.Steps.OrderBy(s => s.Order))
            {
                var status = s.Aufgaben?.Any(a => a.Erledigt) == true ? "✅" : "⏳";
                sb.AppendLine($"<li><strong>{s.Kategorie}</strong> {status}<br>{s.Description}</li>");
            }
            sb.AppendLine("</ul>");

            return Content(sb.ToString(), "text/html");
        }
        [HttpGet("workflow/{workflowId}")]
        public async Task<IActionResult> GetWorkflowDetails(int workflowId)
        {
            try
            {
                var workflow = await _context.Workflows
        .Include(w => w.Steps)
            .ThenInclude(s => s.Aufgaben)
        .Include(w => w.Steps)
            .ThenInclude(s => s.AssignedToUser)
        // 👈 hier
        .FirstOrDefaultAsync(w => w.Id == workflowId);




                if (workflow == null) return NotFound();

                var html = $@"
        <div style='color: #e0e0e0; font-family: Inter, sans-serif;'>
            <h5 class='fw-bold text-light'>Workflow Details</h5>
            <hr style='border-color: #666;' />
            <p class='text-info fw-bold'>{workflow.Title}</p>";
                if (workflow.Dokumente != null && workflow.Dokumente.Any())
                {
                    html += @"<div class='mb-3'>
        <p class='fw-bold'>📎 Anhänge:</p>
        <ul style='padding-left: 20px;'>";
                    foreach (var d in workflow.Dokumente)
                    {
                        html += $@"<li>
            <a href='{d.SasUrl}' target='_blank' style='color: #90caf9; text-decoration: none;'>
                📄 {d.Dateiname}
            </a>
        </li>";
                    }
                    html += "</ul></div>";
                }

                html += "<p class='fw-bold'>Workflow-Schritte:</p>";


                foreach (var step in workflow.Steps.OrderBy(s => s.DueDate))
                {
                    var status = "<span style='color: #ffb300;'>⏳ Offen</span>";
                    var aufgabe = step.Aufgaben?.FirstOrDefault();
                    if (step.Aufgaben != null)
                    {
                        if (aufgabe.Erledigt)
                            status = "<span style='color: #4caf50;'>🟢 Erledigt</span>";
                        else if (step.DueDate.HasValue && step.DueDate.Value < DateTime.Now)
                            status = "<span style='color: #ff5252;'>❌ Überfällig</span>";
                    }

                    Console.WriteLine($"[API] Aufgabe.Status für Step {step.Id}: {(step.Aufgaben?.Any(a => a.Erledigt) == true ? "Erledigt" : "Offen oder fehlt")}");

                    html += $@"
            <div style='margin-bottom: 18px; padding: 10px; border-radius: 6px; background-color: #2c2c2c;'>
                <ul style='padding-left: 20px; margin: 0;'>
                    <li style='margin-bottom: 6px;'>
                        <div><strong>{step.Kategorie}</strong></div>
                        <div style='margin-top: 4px; color: #ccc;'>{step.Description}</div>
                        <div style='margin-top: 6px; font-size: 13px; line-height: 1.6;'>
    <div>📅 <strong>Fällig am:</strong> {step.DueDate?.ToString("dd.MM.yyyy, HH:mm") ?? "-"}</div>
    <div>👤 <strong>Zugewiesen an:</strong> {(step.AssignedToUser != null ? $"{step.AssignedToUser.Vorname} {step.AssignedToUser.Nachname}" : "Unbekannt")}</div>
    <div>📌 <strong>Status:</strong> {status}</div>
</div>
                    </li>
                </ul>
            </div>";
                }

                html += "</div>";

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Interner Fehler: {ex.Message}<br/>{ex.StackTrace}");
            }
        }
        [HttpPost("complete-step/{stepId}")]
        public async Task<IActionResult> CompleteStep(int stepId)
        {

            var step = await _context.Steps
                .Include(s => s.Workflow)
                .FirstOrDefaultAsync(s => s.Id == stepId);
            Console.WriteLine("Aufgabe mit StepId Complete:", stepId);
            if (step == null)
                return NotFound();

            var aufgabe = await _context.Aufgaben.FirstOrDefaultAsync(a => a.StepId == step.Id);

            if (aufgabe == null)
                return BadRequest("Keine Aufgabe zu diesem Schritt gefunden.");

            if (aufgabe.Erledigt)
                return BadRequest("Diese Aufgabe wurde bereits erledigt.");

            if (!aufgabe.Aktiv)
                return BadRequest("Diese Aufgabe ist noch nicht freigegeben.");

            // ✅ Aufgabe erledigen + Step als completed markieren
            aufgabe.Erledigt = true;
            step.Completed = true;

            Console.WriteLine($"✅ Aufgabe erledigt: StepId={step.Id}, Aktiv={aufgabe.Aktiv}");
            await _auditLogService.LogActionOnlyAsync(
                    $"Schritt {step.Order + 1} in Workflow \"{step.Workflow.Title}\" ({step.WorkflowId}) erledigt",
                    step.UserId);
            Console.WriteLine($"Schritt {aufgabe.StepNavigation.Order + 1} in Workflow \"{aufgabe.StepNavigation.Workflow.Title}\" ({aufgabe.StepNavigation.WorkflowId}) erledigt");
            // ✅ Nächsten Step finden
            var nextStep = await _context.Steps
    .Where(s => s.WorkflowId == step.WorkflowId && s.Order > step.Order)
    .OrderBy(s => s.Order)
    .FirstOrDefaultAsync();


            if (nextStep != null)
            {
                var nextTask = await _context.Aufgaben.FirstOrDefaultAsync(a => a.StepId == nextStep.Id);

                if (nextTask != null && !nextTask.Aktiv)
                {
                    nextTask.Aktiv = true;
                    Console.WriteLine($"🚀 Nächster Schritt aktiviert: StepId={nextStep.Id}");
                }
            }
            Console.WriteLine($"Aktueller Schritt: {step.Id}, Order: {step.Order}");
            Console.WriteLine($"Nächster Schritt: {nextStep?.Id}, Order: {nextStep?.Order}");

            var notificationType = await _context.NotificationTypes
         .FirstOrDefaultAsync(n => n.Name == "Workflowaufgabe");
            var notificationTypeEmail = await _context.NotificationTypes
                .FirstOrDefaultAsync(n => n.Name == "Workflowaufgabe Email");
            if (notificationType == null)
            {
                Console.WriteLine("❌ NotificationType 'Workflowaufgabe' fehlt!");
            }
            
            else
            {
                if (nextStep != null)
                {
                    var setting = await _context.UserNotificationSettings
                        .FirstOrDefaultAsync(s => s.UserId == nextStep.UserId && s.NotificationTypeId == notificationType.Id);

                    if (setting == null || setting.Enabled)
                    {
                        var notification = new Notification
                        {
                            Title = "Neue Aufgabe zugewiesen",
                            Content = $"Du hast eine neue Aufgabe im Workflow \"{step.Workflow.Title}\" erhalten.",
                            CreatedAt = DateTime.UtcNow,
                            NotificationTypeId = notificationType.Id,
                            ActionLink = $"/Workflows/StepDetail/{step.WorkflowId}/{stepId}"
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
                    var settingEmail = await _context.UserNotificationSettings
                         .FirstOrDefaultAsync(s => s.UserId == nextStep.UserId && s.NotificationTypeId == notificationTypeEmail.Id);
                    if(settingEmail == null || setting.Enabled)
                    {
                        var userTo = await _context.Users.FindAsync(nextStep.UserId);
                        string subject = "Neue Aufgabe im Workflow";
                        string body = $@"
<p>Hallo {userTo.Vorname},</p>
<p>Du hast eine neue Aufgabe im Workflow <b>""{step.Workflow.Title}""</b> erhalten.</p>
<p>
    <a href='Workflows/StepDetail/{step.WorkflowId}/{stepId}'>Zum Workflow</a>
</p>
<p>Viele Grüße,<br />Dein Team</p>
";
                        await _emailService.SendEmailAsync(userTo.Email, subject, body);
                    }
                }
                else
                {
                    var notificationTypee = await _context.NotificationTypes
                        .FirstOrDefaultAsync(n => n.Name == "Workflow done");
                    var setting = await _context.UserNotificationSettings
                        .FirstOrDefaultAsync(s => s.UserId == step.Workflow.UserId && s.NotificationTypeId == notificationTypee.Id);

                    var notificationTypeEmailWf = await _context.NotificationTypes
                        .FirstOrDefaultAsync(n => n.Name == "Workflow done Email");
                    var settingEmailWf = await _context.UserNotificationSettings
                        .FirstOrDefaultAsync(s => s.UserId == step.Workflow.UserId  && s.NotificationTypeId == notificationTypeEmailWf.Id);
                    if (setting == null || setting.Enabled)
                    {

                        var notification = new Notification
                        {
                            Title = "Workflow abgeschlossen",
                            Content = $"Der Workflow \"{step.Workflow.Title}\" wurde erfolgreich abgeschlossen.",
                            CreatedAt = DateTime.UtcNow,
                            NotificationTypeId = notificationTypee.Id,
                            ActionLink = $"/Workflows/StepDetail/{step.WorkflowId}/{stepId}"
                        };
                        _context.Notifications.Add(notification);
                        await _context.SaveChangesAsync();

                        var userNotification = new UserNotification
                        {
                            UserId = step.Workflow.UserId,
                            NotificationId = notification.Id,
                            IsRead = false,
                            ReceivedAt = DateTime.UtcNow
                        };
                        _context.UserNotifications.Add(userNotification);
                        await _context.SaveChangesAsync();

                    }
                    if(settingEmailWf == null || settingEmailWf.Enabled)
                    {
                        var userTo = await _context.Users.FindAsync(step.Workflow.Id);
                        string subject = "Workflow abgeschlossen";
                        string body = $@"
                <p>Hallo {userTo.Vorname},</p>
                <p>Dein Workflow <b>""{ step.Workflow.Title}""</b> wurde erfolgreich abgeschlossen.</p>
                            < p >< a href = 'Workflows/StepDetail/{step.WorkflowId}/{stepId}' > Workflow ansehen </ a ></ p >
            
                            < p > Viele Grüße,< br /> Dein Team </ p > ";

            await _emailService.SendEmailAsync(userTo.Email, subject, body);
                    }
                }
            }
            var erstelltType = await _context.NotificationTypes.FirstOrDefaultAsync(n => n.Name == "Workflowaufgabe");

            if (erstelltType != null)
            {
                // Finde alle "Neue Aufgabe zugewiesen"-Notifications für diese Aufgabe und diesen User, die noch nicht gelesen sind
                var aufgabenNotification = await _context.UserNotifications
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
                    await _context.SaveChangesAsync();
                }
            }
            var notificationType2 = await _context.NotificationTypes
                    .FirstOrDefaultAsync(n => n.Name == "Workflow erledigt");
            var setting2 = await _context.UserNotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == step.Workflow.UserId && s.NotificationTypeId == notificationType2.Id);

            var notificationTypeEmailWf2 = await _context.NotificationTypes
                .FirstOrDefaultAsync(n => n.Name == "Workflow erledigt Email");
            var settingsEmail2 = await _context.UserNotificationSettings
                .FirstOrDefaultAsync(s => s.UserId == step.Workflow.UserId && s.NotificationTypeId == notificationTypeEmailWf2.Id);
            if (setting2 == null || setting2.Enabled)
            {

                var notification = new Notification
                {
                    Title = "Aufgabe erledigt",
                    Content = $"Im von dir erstellten Workflow \"{step.Workflow.Title}\" wurde Aufgabe {step.Order + 1} erledigt.",
                    CreatedAt = DateTime.UtcNow,
                    NotificationTypeId = notificationType2.Id,
                    // ActionLink mit Route-Parametern
                    ActionLink = $"/Workflows/StepDetail/{step.WorkflowId}/{stepId}"

                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                var userNotification = new UserNotification
                {
                    UserId = step.Workflow.UserId,
                    NotificationId = notification.Id,
                    IsRead = false,
                    ReceivedAt = DateTime.UtcNow
                };
                _context.UserNotifications.Add(userNotification);
                await _context.SaveChangesAsync();

            }
            if(settingsEmail2 == null || settingsEmail2.Enabled)
            {
                var userTo = await _context.Users.FindAsync(step.Workflow.UserId);
                string subject = "Workflow-Aufgabe erledigt";
                string body = $@"
                <p>Hallo {userTo.Vorname},</p>
                <p>Im Workflow <b>""{ step.Workflow.Title}""</b> wurde Aufgabe <b>{step.Order + 1}</b> erledigt.</p>
                    < p >< a href = 'Workflows/StepDetail/{step.WorkflowId}/{stepId}' > Details ansehen </ a ></ p >
    
                    < p > Viele Grüße,< br /> Dein Team </ p > ";

            await _emailService.SendEmailAsync(userTo.Email, subject, body);
            }
        await _context.SaveChangesAsync();

            return Ok(new { success = true });
    }
    }
     }   



    

