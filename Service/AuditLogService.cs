using DmsProjeckt.Data;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Service
{
    public class AuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string aktion, string benutzerId, Guid dokumentId)
        {
            var log = new AuditLog
            {
                Aktion = aktion,
                BenutzerId = benutzerId,
                DokumentId = dokumentId,
                Zeitstempel = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        public async Task LogActionOnlyAsync(string aktion, string benutzerId)
        {
            var log = new AuditLog
            {
                Aktion = aktion,
                BenutzerId = benutzerId,
                Zeitstempel = DateTime.UtcNow
                // DokumentId bleibt null
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
