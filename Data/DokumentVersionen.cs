using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DmsProjeckt.Data
{
    public class DokumentVersionen
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid DokumentId { get; set; }

        [ForeignKey("DokumentId")]
        public Dokumente Dokument { get; set; } = null!;

        [Required]
        public string Dateiname { get; set; } = string.Empty;

        [Required]
        public string Dateipfad { get; set; } = string.Empty;

        [Required]
        public DateTime HochgeladenAm { get; set; } = DateTime.UtcNow;


        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        [ForeignKey("ApplicationUserId")]
        public ApplicationUser ApplicationUser { get; set; } = null!;
        public string ObjectPath { get; set; }
        [Required]
        public string VersionsLabel { get; set; } = string.Empty;

        public string? MetadataJson { get; set; } // Für Metadaten als JSON
        public bool IsDeleted { get; set; } = false; // Soft-Delete
        public bool EstSigne { get; set; } = false; // Ob die Version signiert ist
        public bool IsVersion { get; set; } = false;

        // 🔹 Ajout
        public int? AbteilungId { get; set; }
        public Abteilung? Abteilung { get; set; }

        public Guid? OriginalId { get; set; }
        public ICollection<DokumentVersionChunk>? VersionChunks { get; set; }

        // Pour l’affichage (facultatif mais utile)
        [NotMapped]
        public string? VersionDisplay => $"Version {VersionsLabel} erstellt von {ApplicationUser?.Vorname} {ApplicationUser?.Nachname} am {HochgeladenAm:dd.MM.yyyy HH:mm}";

    }
}
