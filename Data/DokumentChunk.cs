using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DmsProjeckt.Data
{
    public class DokumentChunk
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid DokumentId { get; set; }

        [ForeignKey(nameof(DokumentId))]
        public Dokumente Dokument { get; set; } = null!;

        [Required]
        public int Index { get; set; }  // Position du chunk (0,1,2,...)

        [Required, MaxLength(128)]
        public string Hash { get; set; } = string.Empty; // SHA256 du chunk

        [Required]
        public long Size { get; set; }

        [Required, MaxLength(500)]
        public string FirebasePath { get; set; } = string.Empty; // 🔹 URL/path Firebase

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔄 Relation vers les versions qui l’utilisent
        public ICollection<DokumentVersionChunk>? VersionChunks { get; set; }
    }
}
