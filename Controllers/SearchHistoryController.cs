using DmsProjeckt.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DmsProjeckt.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SearchHistoryController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public SearchHistoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        // 1. Verlauf auslesen
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var items = await _db.SearchHistory
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SearchedAt)
                .Take(50)
                .Select(s => new
                {
                    id = s.Id,
                    searchTerm = s.SearchTerm,
                    searchedAt = s.SearchedAt,
                    dokumentName = s.Dokument != null ? s.Dokument.Dateiname : null
                })
                .ToListAsync();
            return Ok(items);
        }


        // 2. Verlauf löschen (alle)
        [HttpPost("ClearAll")]
        public async Task<IActionResult> ClearAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var items = _db.SearchHistory.Where(s => s.UserId == userId);
            _db.SearchHistory.RemoveRange(items);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // 3. Einzelnen Eintrag löschen
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var item = await _db.SearchHistory.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (item == null) return NotFound();
            _db.SearchHistory.Remove(item);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // 4. NEU! Eintrag hinzufügen (wichtig!)
        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] SearchHistoryDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(dto.SearchTerm))
                return BadRequest("Benutzer oder Suchbegriff fehlt.");

            var entry = new SearchHistory
            {
                UserId = userId,
                SearchTerm = dto.SearchTerm,
                SearchedAt = DateTime.UtcNow,
                DokumentId = dto.DokumentId
            };

            // Neu: Wenn DokumentName vorhanden, versuche die ID aufzulösen, falls nicht gesetzt
            if (dto.DokumentId == null && !string.IsNullOrEmpty(dto.DokumentName))
            {
                var dokument = await _db.Dokumente
                    .Where(d => d.Dateiname == dto.DokumentName && d.ApplicationUserId == userId)
                    .FirstOrDefaultAsync();
                if (dokument != null)
                {
                    entry.DokumentId = dokument.Id;
                }
            }

            _db.SearchHistory.Add(entry);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }

        // Einfache DTO-Klasse für das Hinzufügen von Verlauf
        public class SearchHistoryDto
    {
        public string? SearchTerm { get; set; }
        public Guid? DokumentId { get; set; }
        public string? DokumentName { get; set; }
    }
}
