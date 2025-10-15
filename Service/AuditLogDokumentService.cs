using DmsProjeckt.Data;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Services
{
    public class AuditLogDokumentService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogDokumentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task EnregistrerAsync(string aktion, string benutzerId, Guid dokumentId)
        {
            // Vérifie que le document existe
            var dokumentExiste = await _context.Dokumente.AnyAsync(d => d.Id == dokumentId);
            if (!dokumentExiste)
            {
                throw new ArgumentException($"Dokument mit Id '{dokumentId}' existiert nicht.");
            }

            var log = new AuditLogDokument
            {
                Aktion = aktion,
                BenutzerId = benutzerId,
                DokumentId = dokumentId,
                Zeitstempel = DateTime.Now
            };

            _context.AuditLogDokumente.Add(log);
            await _context.SaveChangesAsync();
        }


        public async Task<List<AuditLogDokument>> ObtenirHistoriquePourBenutzerAsync(string benutzerId)
        {
            return await _context.AuditLogDokumente
                .Where(x => x.BenutzerId == benutzerId)
                .OrderByDescending(x => x.Zeitstempel)
                .ToListAsync();
        }
        public async Task<List<AuditLogDokument>> ObtenirHistoriqueParDokumentAsync(Guid dokumentId)
        {
            return await _context.AuditLogDokumente
                .Where(x => x.DokumentId == dokumentId)
                .OrderByDescending(x => x.Zeitstempel)
                .ToListAsync();
        }
        public async Task<List<AuditLogDokument>> ObtenirTousLesLogsAvecDokumentAsync()
        {
            return await _context.AuditLogDokumente
                .Include(l => l.Dokument)  // Assure-toi que la navigation existe
                .OrderByDescending(l => l.Zeitstempel)
                .ToListAsync();
        }


    }
}
