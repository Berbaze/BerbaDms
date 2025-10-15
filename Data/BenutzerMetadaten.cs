using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DmsProjeckt.Data
{
    public class BenutzerMetadaten
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid DokumentId { get; set; }

        [MaxLength(100)]
        public string? Key { get; set; }

        [MaxLength(500)]
        public string? Value { get; set; }

        public DateTime ErzeugtAm { get; set; } = DateTime.UtcNow;

        // 🔗 Navigation property
        [ForeignKey(nameof(DokumentId))]
        public virtual Dokumente? Dokument { get; set; }
    }
}
