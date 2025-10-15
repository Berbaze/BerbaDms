using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DmsProjeckt.Data
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Aktion { get; set; } = string.Empty;

        [Required]
        public string BenutzerId { get; set; } = string.Empty;

        [ForeignKey(nameof(BenutzerId))]
        public ApplicationUser? Benutzer { get; set; }

        public Guid? DokumentId { get; set; }

        [ForeignKey(nameof(DokumentId))]
        public Dokumente? Dokument { get; set; }

        [Required]
        public DateTime Zeitstempel { get; set; } = DateTime.Now;
    }
}
