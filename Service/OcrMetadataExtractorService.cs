using System.Globalization;
using System.Text.RegularExpressions;

namespace DmsProjeckt.Service
{
    public class OcrMetadataExtractorService
    {
        public class OcrMetadataResult
        {
            public string Rechnungsnummer { get; set; } = "";
            public string Rechnungsbetrag { get; set; } = "";
            public string Rechnungsdatum { get; set; } = "";
            public string Lieferdatum { get; set; } = "";
            public string Kundennummer { get; set; } = "";
            public string Kategorie { get; set; } = "";
            public string Titel { get; set; } = "";
            public string Autor { get; set; } = "";
            public string Betreff { get; set; } = "";
            public string Stichworte { get; set; } = "";
            public string Zahlungsbedingungen { get; set; } = "";
            public string Lieferart { get; set; } = "";
            public string Steuerbetrag { get; set; } = "";
            public string ArtikelAnzahl { get; set; } = "";
            public string Email { get; set; } = "";
            public string Telefon { get; set; } = "";
            public string Telefax { get; set; } = "";
            public string IBAN { get; set; } = "";
            public string BIC { get; set; } = "";
            public string Bankverbindung { get; set; } = "";
            public string Zeitraum { get; set; } = "";
            public string SteuerNr { get; set; } = "";
            public string Gesamtpreis { get; set; } = "";
            public string AnsprechPartner { get; set; } = "";
            public string Adresse { get; set; } = "";
            public string Website { get; set; } = "";
            public string Nettobetrag { get; set; } = "";
            public string? PdfAutor { get; set; } = "";
            public string? PdfBetreff { get; set; } = "";
            public string? Schluesselwoerter { get; set; } = "";


        }

        private static string NormalizeDecimal(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            // Entferne Tausenderpunkte und ersetze Komma mit Punkt
            return input.Replace(".", "").Replace(",", ".").Trim();
        }



        public static OcrMetadataResult Extract(string ocrText)
        {
            var result = new OcrMetadataResult();
            var cleanedText = Regex.Replace(ocrText ?? string.Empty, "\\s{2,}", " ").Trim();
            var lowerText = cleanedText.ToLowerInvariant();

            // 📩 Données classiques
            result.Email = MatchValue(cleanedText, @"[\w\.-]+@[\w\.-]+\.\w+", 0);
            result.Telefon = MatchValue(cleanedText, @"(?i)(Telefon|Tel\.?)\s*[:\-]?\s*([\+0-9\s\/\-]{6,})", 2);
            result.Telefax = MatchValue(cleanedText, @"(?i)(Fax|Telefax)\s*[:\-]?\s*([\+0-9\s\/\-]{6,})", 2);
            result.Zeitraum = MatchValue(cleanedText, @"(?i)(LEISTUNGSZEITRAUM|ZEITRAUM)[:\s]+([^\n\r]+)", 2) ??
                              MatchValue(cleanedText, "Liefer-/Leistungszeitraum[:\\s]*([^\n\r]+)", 1);
            result.SteuerNr = MatchValue(cleanedText, @"Steuer[-\s]?Nr[\s:]*([0-9/]+)", 1);
            result.Lieferart = MatchValue(cleanedText, @"Lieferart[\s:]*([^\n]+)", 1);
            result.ArtikelAnzahl = Regex.Matches(cleanedText, @"\b(Menge|Stk)[\s:]*\d+", RegexOptions.IgnoreCase).Count.ToString();
            result.Stichworte = string.Join(", ", DetectKeywords(cleanedText));
            result.Website = MatchValue(cleanedText, @"(?i)www\.[\w\-\.]+", 0);

            // 📄 Ajout des champs PDF spécifiques
            result.Autor = MatchValue(cleanedText, @"(?i)Autor\s*[:\-]?\s*(.*?)(?=\s{2,}|$)", 1);
            result.Betreff = MatchValue(cleanedText, @"(?i)Betreff\s*[:\-]?\s*(.*?)(?=\s{2,}|$)", 1);
            result.Schluesselwoerter = MatchValue(cleanedText, @"(?i)Mots[- ]?cl[eé]s?\s*[:\-]?\s*(.*?)(?=\s{2,}|$)", 1);
      

            // 🧠 Catégorie auto
            result.Kategorie = lowerText.Contains("gebühren") ? "gebühren" :
                               lowerText.Contains("rechnung") ? "rechnungen" :
                               lowerText.Contains("vertrag") ? "verträge" :
                               lowerText.Contains("projekt") ? "projekt_a" :
                               "korrespondenz";

            // 🧹 Nettoyage final
            foreach (var prop in typeof(OcrMetadataResult).GetProperties())
            {
                if (prop.PropertyType == typeof(string))
                {
                    var val = (string)prop.GetValue(result);
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        prop.SetValue(result, val.Replace("?", "").Trim());
                    }
                }
            }

            return result;
        }



        private static string MatchBlock(string input, string marker, int lines)
        {
            var start = input.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return null;
            var after = input.Substring(start).Split('\n').Skip(1).Take(lines).ToArray();
            return string.Join(", ", after.Select(s => s.Trim()));
        }


        private static string MatchValue(string text, string pattern, int group)
        {
            var match = Regex.Match(text, pattern);
            return match.Success ? match.Groups[group].Value.Trim() : null;
        }


        private static List<string> DetectKeywords(string text)
        {
            var keywords = new[]
            {
            "Gebührenrechnung", "Postfach", "Telefon", "Telefax", "Email",
            "AnsprechPartner", "Adresse", "Website",
            "Partnerschaftsregister", "Gesetz", "Zeitraum", "Umsatzsteuer", "IBAN", "BIC" , "Autor" ,"Betreff" , "PdfSchluesselwoerter"
        };
            return keywords.Where(k => text.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0).Distinct().ToList();
        }

        private static string ExtractDate(string text, params string[] labels)
        {
            foreach (var label in labels)
            {
                var pattern = $@"{Regex.Escape(label)}\s*[:\-]?\s*(\d{{2}}[./-]\d{{2}}[./-]\d{{4}})";
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var raw = match.Groups[1].Value.Trim();
                    if (DateTime.TryParseExact(raw, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                        return dt.ToString("dd.MM.yyyy");
                    return raw;
                }
            }
            return "";
        }


    }
}

