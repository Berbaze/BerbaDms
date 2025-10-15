namespace DmsProjeckt.Data
{
    public class DokumentSucheFilter
    {
        public string? Query { get; set; } // 🔍 Recherche intelligente
        public string? Dateiname { get; set; }
        public string? Kategorie { get; set; }
        public string? BenutzerId { get; set; }
        public DateTime? Von { get; set; }
        public DateTime? Bis { get; set; }
        public string? Status { get; set; }
        public string? Rechnungsnummer { get; set; }
        public string? Kundennummer { get; set; }
        public string? PdfAutor { get; set; }
        public string? PdfBetreff { get; set; }
        public string? PdfSchluesselwoerter { get; set; }
        public string? OCRText { get; set; }
    }

}
