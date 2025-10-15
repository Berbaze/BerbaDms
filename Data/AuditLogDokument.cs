using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DmsProjeckt.Data
{
    public class AuditLogDokument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid DokumentId { get; set; }

        [Required]
        [StringLength(100)]
        public string BenutzerId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Aktion { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Details { get; set; }

        [Required]
        public DateTime Zeitstempel { get; set; } = DateTime.Now;

        // Optionnel : Navigation property si vous avez une relation
        [ForeignKey("DokumentId")]
        public Dokumente? Dokument { get; set; }
    }
}
