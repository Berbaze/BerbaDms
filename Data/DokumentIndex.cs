using System.ComponentModel.DataAnnotations;

namespace DmsProjeckt.Data
{
    public class DokumentIndex
    {
        [Key]
        public Guid DokumentId { get; set; }  // Clé étrangère vers Dokumente

        public string? Titel { get; set; } = string.Empty;
        public string? Dateiname { get; set; } = string.Empty;
        public string? Beschreibung { get; set; } = string.Empty;
        public string? OCRText { get; set; } = string.Empty;
        public string? Kategorie { get; set; } = string.Empty;
        public string? ErkannteKategorie { get; set; }
        public string? Rechnungsnummer { get; set; }
        public string? Kundennummer { get; set; }
        public decimal? Rechnungsbetrag { get; set; }
        public decimal? Nettobetrag { get; set; }
        public decimal? Gesamtbetrag { get; set; }
        public decimal? Steuerbetrag { get; set; }
        public DateTime? Rechnungsdatum { get; set; }
        public DateTime? Lieferdatum { get; set; }
        public DateTime? Faelligkeitsdatum { get; set; }
        public string? Zahlungsbedingungen { get; set; }
        public string? lieferart { get; set; }
        public int? ArtikelAnzahl { get; set; }
        public string? Email { get; set; }
        public string? Telefon { get; set; }
        public string? Telefax { get; set; }
        public string? IBAN { get; set; }
        public string? BIC { get; set; }
        public string? Bankverbindung { get; set; }
        public string? SteuerNr { get; set; }
        public string? UIDNummer { get; set; }
        public string? Adresse { get; set; }
        public string? AbsenderAdresse { get; set; }
        public string? AnsprechPartner { get; set; }
        public string? Zeitraum { get; set; }
        public string? Website { get; set; }
        public string? Kundenname { get; set; }
        public string? FirmenName { get; set; }
        public string? Tags { get; set; } = string.Empty;
        public string? Metadaten { get; set; } = string.Empty;
        public string? Autor { get; set; }
        public string? Betreff { get; set; }
        public string? Schluesselwoerter { get; set; }
        public string? ObjectPath { get; set; }
    }
}
