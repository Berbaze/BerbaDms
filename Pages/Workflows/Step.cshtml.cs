using DmsProjeckt.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Pages.Workflows
{
    public class StepModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public Step Step;
        public StepModel(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> OnGetAsync(int id)
        {
            Step = await _context.Steps
                .Include(s => s.Workflow)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (Step == null) return NotFound();

            switch (Step.Kategorie)
            {
                case "Upload":
                    return RedirectToPage("/Workflows/StepUpload", new { stepId = Step.Id, workflowId = Step.WorkflowId });

                case "Signieren":
                    // ?? Dokument für diesen Step finden
                    var dokument = await _context.Dokumente
                        .FirstOrDefaultAsync(d => d.StepId == Step.Id);

                    if (dokument == null)
                    {
                        // falls kein Dokument verknüpft ist
                        return RedirectToPage("/Workflows/StepDetail", new { stepId = Step.Id, workflowId = Step.WorkflowId });
                    }

                    // ?? Redirect zur Bearbeiten-Seite mit Dokument-ID
                    return RedirectToPage("/Dokument/Bearbeiten", new { id = dokument.Id, fromTask = true });

                default:
                    return RedirectToPage("/Workflows/StepDetail", new { stepId = Step.Id, workflowId = Step.WorkflowId });
            }
        }

    }
}
