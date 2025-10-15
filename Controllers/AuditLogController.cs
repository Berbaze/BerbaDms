using DmsProjeckt.Data;
using DmsProjeckt.Services;
using Microsoft.AspNetCore.Mvc;

namespace DmsProjeckt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogController : ControllerBase
    {
        private readonly AuditLogDokumentService _auditLogService;

        public AuditLogController(AuditLogDokumentService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet("{dokumentId}")]
        public async Task<IActionResult> Get(Guid dokumentId)
        {
            var result = await _auditLogService.ObtenirHistoriqueParDokumentAsync(dokumentId);
            return Ok(result);
        }
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var logs = await _auditLogService.ObtenirTousLesLogsAvecDokumentAsync();
            var result = logs.Select(log => new
            {
                log.Zeitstempel,
                log.Aktion,
                log.BenutzerId,
                log.DokumentId,
                Dateiname = log.Dokument?.Dateiname,
                Kategorie = log.Dokument?.Kategorie
            });

            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AuditLogDokument log)
        {
            if (log == null || log.DokumentId == Guid.Empty || string.IsNullOrWhiteSpace(log.BenutzerId))
                return BadRequest("Ungültige Audit-Daten.");

            await _auditLogService.EnregistrerAsync(log.Aktion, log.BenutzerId, log.DokumentId);
            return Ok(new { success = true });
        }


    }
}
