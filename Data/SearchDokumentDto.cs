using System.Text.Json.Serialization;

namespace DmsProjeckt.Data
{
    public class SearchDokumentDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("titel")]
        public string Titel { get; set; } = string.Empty;

        [JsonPropertyName("Autor")]
        public string? Autor { get; set; }

        [JsonPropertyName("Betreff")]
        public string? Betreff { get; set; }

        [JsonPropertyName("Schluesselwoerter")]
        public string? Schluesselwoerter { get; set; }

        [JsonPropertyName("beschreibung")]
        public string Beschreibung { get; set; } = string.Empty;

        [JsonPropertyName("ocrText")]
        public string OCRText { get; set; } = string.Empty;

        [JsonPropertyName("kategorie")]
        public string Kategorie { get; set; } = string.Empty;

        [JsonPropertyName("erkannteKategorie")]
        public string? ErkannteKategorie { get; set; }

        [JsonPropertyName("rechnungsnummer")]
        public string? Rechnungsnummer { get; set; }

        [JsonPropertyName("kundennummer")]
        public string? Kundennummer { get; set; }

        [JsonPropertyName("rechnungsbetrag")]
        public double? Rechnungsbetrag { get; set; }

        [JsonPropertyName("nettobetrag")]
        public double? Nettobetrag { get; set; }

        [JsonPropertyName("gesamtbetrag")]
        public double? Gesamtbetrag { get; set; }

        [JsonPropertyName("steuerbetrag")]
        public double? Steuerbetrag { get; set; }

        [JsonPropertyName("rechnungsdatum")]
        public DateTime? Rechnungsdatum { get; set; }

        [JsonPropertyName("lieferdatum")]
        public DateTime? Lieferdatum { get; set; }

        [JsonPropertyName("faelligkeitsdatum")]
        public DateTime? Faelligkeitsdatum { get; set; }

        [JsonPropertyName("zahlungsbedingungen")]
        public string? Zahlungsbedingungen { get; set; }

        [JsonPropertyName("lieferart")]
        public string? lieferart { get; set; }

        [JsonPropertyName("artikelanzahl")]
        public int? ArtikelAnzahl { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("telefon")]
        public string? Telefon { get; set; }

        [JsonPropertyName("telefax")]
        public string? Telefax { get; set; }

        [JsonPropertyName("iban")]
        public string? IBAN { get; set; }

        [JsonPropertyName("bic")]
        public string? BIC { get; set; }

        [JsonPropertyName("bankverbindung")]
        public string? Bankverbindung { get; set; }

        [JsonPropertyName("steuernr")]
        public string? SteuerNr { get; set; }

        [JsonPropertyName("uidnummer")]
        public string? UIDNummer { get; set; }

        [JsonPropertyName("adresse")]
        public string? Adresse { get; set; }

        [JsonPropertyName("absenderadresse")]
        public string? AbsenderAdresse { get; set; }

        [JsonPropertyName("ansprechpartner")]
        public string? AnsprechPartner { get; set; }

        [JsonPropertyName("zeitraum")]
        public string? Zeitraum { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("kundename")]
        public string? Kundenname { get; set; }

        [JsonPropertyName("firmenname")]
        public string? FirmenName { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("metadaten")]
        public List<string> Metadaten { get; set; } = new();


    }
}
