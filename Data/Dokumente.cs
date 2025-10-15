using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DmsProjeckt.Data
{
    public class Dokumente
    {
        [Key]
        public Guid Id { get; set; }

        public string? Dateipfad { get; set; } = string.Empty;
        public string? Beschreibung { get; set; } = string.Empty;
        public string? ErkannteKategorie { get; set; }

        // 🔗 Beziehungen
        public int? KundeId { get; set; }
        [ForeignKey(nameof(KundeId))]
        public Kunden? Kunde { get; set; }

        [ForeignKey(nameof(ApplicationUser))]
        public string? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }

        public Status DokumentStatus { get; set; } = Status.Aktiv;

        public DmsProjeckt.Data.DokumentStatus dtStatus { get; set; } = DmsProjeckt.Data.DokumentStatus.Neu;

        public string? ObjectPath { get; set; }
        public int? AufgabeId { get; set; }
        [ForeignKey(nameof(AufgabeId))]
        public Aufgaben? Aufgabe { get; set; }

        public ICollection<DokumentVersionen> Versionen { get; set; } = new List<DokumentVersionen>();

        public bool? EstSigne { get; set; } = false;
        public int? WorkflowId { get; set; }

        public bool? IsIndexed { get; set; }
        public bool? IstFavorit { get; set; } = false;

        public long? FileSizeBytes { get; set; }
        public bool IsUpdated { get; set; } = false;
        public int? StepId { get; set; }

        public Guid? OriginalId { get; set; }
        public bool IsVersion { get; set; } = false;

        public int? AbteilungId { get; set; }
        public Abteilung? Abteilung { get; set; }

        // 📎 Verbindung zu anderen Tabellen
        public ICollection<DokumentTags>? DokumentTags { get; set; }
        public ICollection<BenutzerMetadaten>? BenutzerMetadaten { get; set; }
        public ICollection<Step>? Steps { get; set; }
        public ICollection<Workflow>? Workflows { get; set; }
        public ICollection<Archive>? Archive { get; set; }

        // 🧩 Anzeige & UI-Felder
        [NotMapped]
        public string? SasUrl { get; set; }

        [NotMapped]
        public string? Unterschrieben { get; set; }

        [NotMapped]
        public string? RowCssClass { get; set; }

        // 🧾 Hauptfelder
        [JsonPropertyName("titel")]
        public string? Titel { get; set; }

        [JsonPropertyName("dateiname")]
        public string? Dateiname { get; set; }

        [JsonPropertyName("hochgeladenAm")]
        public DateTime HochgeladenAm { get; set; }

        [JsonPropertyName("kategorie")]
        public string? Kategorie { get; set; }

        // 🔗 Verbindung zu den neuen Metadaten
        public int? MetadatenId { get; set; }

        [ForeignKey(nameof(MetadatenId))]
        public Metadaten? MetadatenObjekt { get; set; }
        public string? FileHash { get; set; }
        public DateTime? LetzteAenderung { get; set; }
        public bool IsChunked { get; set; } = false; // ✅ indique si le fichier est stocké par blocs

        public ICollection<DokumentChunk>? Chunks { get; set; } // ✅ relation 1 → plusieurs
        [MaxLength(50)]
        public string? StorageLocation { get; set; } = "firebase"; // "firebase" | "chunked" | "local"


    }

    // 📌 Status-Enums
    public enum Status
    {
        Aktiv,
        Archiviert,
        InBearbeitung,
        Gesperrt,
        Gelöscht,
        InitOrdner
    }

    public enum DokumentStatus
    {
        Neu = 0,
        InBearbeitung = 1,
        Fertig = 2,
        Fehlerhaft = 3,
        Pending,
        Analyzed,
        Saved,
        Error
    }
}
