using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DmsProjeckt.Data
{
    public class DmsFile
    {
        [Key]
        public string Id { get; set; }
        public Guid? GuidId { get; set; } 

        // Basisdaten
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? Kategorie { get; set; }
        public DateTime? HochgeladenAm { get; set; }
        public string? SasUrl { get; set; }
        public string? ObjectPath { get; set; }

        // Metadaten (alle, die du brauchst)
        public string? Beschreibung { get; set; }
        public string? Titel { get; set; }
        public string? Rechnungsnummer { get; set; }
        public string? Kundennummer { get; set; }
        public decimal? Rechnungsbetrag { get; set; }
        public decimal? Nettobetrag { get; set; }
        public decimal? Gesamtpreis { get; set; }
        public decimal? Steuerbetrag { get; set; }
        public DateTime? Rechnungsdatum { get; set; }
        public DateTime? Lieferdatum { get; set; }
        public DateTime? Faelligkeitsdatum { get; set; }
        public string? Zahlungsbedingungen { get; set; }
        public string? Lieferart { get; set; }
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
        public string? PdfAutor { get; set; }
        public string? PdfBetreff { get; set; }
        public string? PdfSchluesselwoerter { get; set; }
        public string? Website { get; set; }
        public string? OCRText { get; set; }
        public string? Status { get; set; }
        public bool? IsIndexed { get; set; }
        public bool IsVersion { get; set; } = false;
        public bool EstSigne { get; set; } = false;
        public Guid OriginalId { get; set; } = Guid.NewGuid(); // Einzigartige ID für die Datei

        // 🧠 NEU: Verknüpfung zu den Metadaten
        public Metadaten? MetadatenObjekt { get; set; }
        // Alias-Property für Razor-Kompatibilität (Explorer / _DokumentTableRow)
        public string? MetadataJson { get; set; }
        // Erweiterbar je nach Bedarf!
        public int? AbteilungId { get; set; }
        public string AbteilungName { get; set; }
        public string? Benutzer  { get; set; }
        public string? ApplicationUserName { get; set; }
        public string? BenutzerVorname { get; set; }
        public string? BenutzerNachname { get; set; }
        public bool IsChunked { get; set; } = false;
        [NotMapped]
        public string BenutzerVollerName => $"{BenutzerVorname} {BenutzerNachname}".Trim();



    }
}
