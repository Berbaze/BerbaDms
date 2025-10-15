using DmsProjeckt.Data;

namespace DmsProjeckt.Service
{
    public class AdminAuditService
    {
        private readonly ApplicationDbContext _db;

        public AdminAuditService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(string adminId, string action, string targetUserId, string? details = null)
        {
            var log = new AuditLogAdmin
            {
                AdminId = adminId,
                Action = action,
                TargetUserId = targetUserId,
                Timestamp = DateTime.UtcNow,
                Details = details
            };

            _db.AuditLogAdmins.Add(log);
            await _db.SaveChangesAsync();
        }
    }
}
