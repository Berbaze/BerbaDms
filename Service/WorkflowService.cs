using DmsProjeckt.Data;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Service
{
    public class WorkflowService
    {
        private readonly ApplicationDbContext _db;
        public WorkflowService(ApplicationDbContext db) { _db = db; }

        public async Task<List<Step>> GetOpenStepsForUserAsync(string userId)
        {
            return await _db.Steps
                .Where(s => s.UserId == userId && !s.Completed)
                .OrderBy(s => s.DueDate)
                .ToListAsync();
        }
    }

}