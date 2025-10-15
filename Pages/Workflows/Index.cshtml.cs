using System.Security.Claims;
using DmsProjeckt.Data;
using DmsProjeckt.Service;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;


namespace DmsProjeckt.Pages.Workflows
{

    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;
        private readonly UserManager<ApplicationUser> _userManager;
        public IndexModel(ApplicationDbContext context, AuditLogService auditLogService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditLogService = auditLogService;
            _userManager = userManager;
        }

        public List<Workflow> Workflows { get; set; }
public string CurrentUserId { get; set; }
        [BindProperty]
        public int DeleteId { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            var workflowIds = await _context.Aufgaben
                .Where(a => (a.FuerUser == user.Id || a.VonUser == user.Id) && a.WorkflowId != null)
                .Select(a => a.WorkflowId.Value)
                .Distinct().ToListAsync();
            Workflows = await _context.Workflows
                .Where(w => workflowIds.Contains(w.Id))
                .Include(w => w.Steps)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
            CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = _userManager.GetUserId(User);

            var workflow = await _context.Workflows.FindAsync(DeleteId);
            if (workflow != null)
            {
                _context.Workflows.Remove(workflow);
                await _context.SaveChangesAsync();
            }

            await _auditLogService.LogActionOnlyAsync($"Workflow \"{workflow.Title}\" ({workflow.Id}) gelöscht", userId);

            // ❗Workflows neu laden, sonst ist Model.Workflows == null
            Workflows = await _context.Workflows
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return Page(); // Kein Redirect, da du Modal nutzt
        }

    }
}
