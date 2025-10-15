using DmsProjeckt.Data;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Service
{
    public class DokumentIndexService
    {
        private readonly ApplicationDbContext _context;

        public DokumentIndexService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DokumentIndex>> GetAllIndexedAsync()
        {
            return await _context.DokumentIndex
                .OrderByDescending(x => x.Rechnungsdatum)
                .ToListAsync();
        }
        public async Task<List<DokumentIndex>> GetIndexedForUserAsync(string applicationUserId)
        {
            return await (
                from idx in _context.DokumentIndex
                join doc in _context.Dokumente on idx.DokumentId equals doc.Id
                where doc.ApplicationUserId == applicationUserId
                select idx
            )
            .OrderByDescending(x => x.Rechnungsdatum)
            .ToListAsync();
        }

    }
}
