using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DmsProjeckt.Data
{
    public class Metadaten
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Dokument))]
        public Guid? DokumentId { get; set; }   // ❗ nicht nullable für 1:1-Beziehung

        [JsonIgnore]
        public Dokumente Dokument { get; set; } = null!;

        public string? Titel { get; set; }
        public string? Beschreibung { get; set; }
        public string? Kategorie { get; set; }
        public string? Stichworte { get; set; }

        public string? Rechnungsnummer { get; set; }
        public string? Kundennummer { get; set; }
        public decimal? Rechnungsbetrag { get; set; }
        public decimal? Nettobetrag { get; set; }
        public decimal? Steuerbetrag { get; set; }
        public decimal? Gesamtpreis { get; set; }

        public DateTime? Rechnungsdatum { get; set; }
        public DateTime? Lieferdatum { get; set; }
        public DateTime? Faelligkeitsdatum { get; set; }

        public string? Zahlungsbedingungen { get; set; }
        public string? Lieferart { get; set; }
        public int? ArtikelAnzahl { get; set; }
        public string? SteuerNr { get; set; }
        public string? UIDNummer { get; set; }
        public string? Email { get; set; }
        public string? Telefon { get; set; }
        public string? Telefax { get; set; }
        public string? IBAN { get; set; }
        public string? BIC { get; set; }
        public string? Bankverbindung { get; set; }
        public string? Adresse { get; set; }
        public string? AbsenderAdresse { get; set; }
        public string? AnsprechPartner { get; set; }
        public string? Zeitraum { get; set; }
        public string? PdfAutor { get; set; }
        public string? PdfBetreff { get; set; }
        public string? PdfSchluesselwoerter { get; set; }
        public string? Website { get; set; }
        public string? OCRText { get; set; }
    }

}
