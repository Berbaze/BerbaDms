using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DmsProjeckt.Data
{
    public class Archive
    {
        [Key]
        public Guid Id { get; set; }

        // 🔗 Verknüpfung zum Original-Dokument
        public Guid DokumentId { get; set; }
        [ForeignKey(nameof(DokumentId))]
        public Dokumente Dokument { get; set; }

        // 📄 Dateiinformationen
        public string? ArchivName { get; set; } // z.B. "Rechnung_2024_archiviert_20251006.pdf"
        public string? ArchivPfad { get; set; } // z.B. "dokumente/software/finanzen/archiv/"
        public long? FileSizeBytes { get; set; }

        // 🕓 Metadaten zum Archivvorgang
        public DateTime ArchivDatum { get; set; } = DateTime.UtcNow;
        public string? BenutzerId { get; set; } // Wer hat archiviert?
        public string? Grund { get; set; }       // Warum archiviert?

        // 💾 Optional: Backup der Metadaten zum Archiv-Zeitpunkt
        public string? MetadatenJson { get; set; }

        public bool IstAktiv { get; set; } = true;
    }
}
