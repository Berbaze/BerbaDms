using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using DmsProjeckt.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using DmsProjeckt.Service;

namespace DmsProjeckt.Pages.Workflows
{
    public class BearbeitenModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLogService;
        public BearbeitenModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AuditLogService auditLogService)
        {
            _context = context;
            _userManager = userManager;
            _auditLogService = auditLogService;
        }
        public List<Workflow> Workflows { get; set; }
        [BindProperty]
        public Workflow Workflow { get; set; }

        [BindProperty]
        public List<Step> Steps { get; set; }

        [BindProperty]
        public string? DeletedStepIds { get; set; }

        public List<SelectListItem> UserOptions { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Workflow = await _context.Workflows
                .Include(w => w.Steps.OrderBy(s => s.Order))
                .FirstOrDefaultAsync(w => w.Id == id);

            if (Workflow == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            UserOptions = await _context.Users
                .Where(u => u.Id != currentUser.Id)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.Vorname} {u.Nachname}"
                })
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
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

            foreach (var s in Steps)
            {
                Console.WriteLine($"STEP Id={s.Id}, User={s.UserId}, Title={s.Title}, Kategorie={s.Kategorie}");
            }
            // für Debug
            if (!ModelState.IsValid)
            {
                foreach (var entry in ModelState)
                {
                    foreach (var error in entry.Value.Errors)
                    {
                        Console.WriteLine($"[ModelState] {entry.Key}: {error.ErrorMessage}");
                    }
                }
                return Page();
            }


            Console.WriteLine("🚨 OnPostAsync wurde aufgerufen");
            // Bestehende Steps aus der DB holen
            var existing = await _context.Workflows
                .Include(w => w.Steps)
                .FirstOrDefaultAsync(w => w.Id == Workflow.Id);

            if (existing == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            existing.UserId = userId;
            existing.Title = Workflow.Title;
            existing.LastModified = DateTime.UtcNow;

            // Alte Schritte löschen, die nicht mehr da sind
            if (!string.IsNullOrWhiteSpace(DeletedStepIds))
            {
                var idsToDelete = DeletedStepIds.Split(',')
                    .Select(id => int.TryParse(id, out var x) ? x : (int?)null)
                    .Where(x => x.HasValue)
                    .Select(x => x.Value)
                    .ToList();

                var toRemove = existing.Steps.Where(s => idsToDelete.Contains(s.Id)).ToList();
                _context.Steps.RemoveRange(toRemove);
            }

            // Schritte aktualisieren
            foreach (var incoming in Steps.OrderBy(s => s.Order))
            {
                var existingStep = existing.Steps.FirstOrDefault(s => s.Id == incoming.Id);

                if (existingStep != null)
                {
                    // Update vorhandener Step
                    existingStep.UserId = incoming.UserId;
                    existingStep.Kategorie = incoming.Kategorie;
                    existingStep.Description = incoming.Description;
                    existingStep.DueDate = incoming.DueDate;
                    existingStep.Order = incoming.Order;
                    existingStep.Title = incoming.Title;

                    var aufgabe = await _context.Aufgaben.FirstOrDefaultAsync(a => a.StepId == existingStep.Id);
                    if (aufgabe != null)
                    {
                        aufgabe.Titel = incoming.Kategorie;
                        aufgabe.Beschreibung = incoming.Description;
                        aufgabe.FaelligBis = (DateTime)incoming.DueDate;
                        aufgabe.FuerUser = incoming.UserId;
                        aufgabe.Erledigt = incoming.Completed;
                    }
                }
                else
                {
                    // Neuer Step
                    incoming.WorkflowId = existing.Id;
                    existing.Steps.Add(incoming);
                }
            }

            await _auditLogService.LogActionOnlyAsync($"Workflow \"{existing.Title}\" ({existing.Id}) bearbeitet", userId);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
}
