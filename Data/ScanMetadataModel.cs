namespace DmsProjeckt.Data
{
    public class ScanMetadataModel
    {
        public string? Titel { get; set; }
        public string? Beschreibung { get; set; }
        public string? Kategorie { get; set; }
        public string? AbteilungName { get; set; }

        public string? Rechnungsnummer { get; set; }
        public string? Kundennummer { get; set; }
        public string? Rechnungsbetrag { get; set; }
        public string? Nettobetrag { get; set; }
        public string? Gesamtpreis { get; set; }
        public string? Steuerbetrag { get; set; }
        public string? Rechnungsdatum { get; set; }
        public string? Lieferdatum { get; set; }
        public string? Faelligkeitsdatum { get; set; }
        public string? Zahlungsbedingungen { get; set; }
        public string? Lieferart { get; set; }
        public string? ArtikelAnzahl { get; set; }

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
    }

}

