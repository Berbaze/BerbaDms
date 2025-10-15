using System.Security.Claims;
using DmsProjeckt.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DmsProjeckt.Helpers
{
    public static class DocumentPathHelper
    {
        // ✅ Récupération Abteilung selon rôle
        public static async Task<Abteilung?> ResolveAbteilungAsync(
            ClaimsPrincipal userPrincipal,
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            int? abteilungId = null)
        {
            var userId = userManager.GetUserId(userPrincipal);
            var user = await db.Users.FindAsync(userId);

            if (user == null)
                return null;

            // Cas user normal → il est lié à UNE abteilung
            if (!userPrincipal.IsInRole("Admin") && !userPrincipal.IsInRole("SuperAdmin"))
            {
                if (user.AbteilungId == null)
                    return null;

                return await db.Abteilungen.FindAsync(user.AbteilungId);
            }

            // Cas admin/superadmin → doit choisir
            if (abteilungId == null || abteilungId == 0)
                return null;

            return await db.Abteilungen.FindAsync(abteilungId);
        }

        public static (string finalPath, int? abteilungId) BuildFinalPath(
       string firma,
       string fileName,
       string kategorie,
       int? abteilungId,
       string abteilungName = null)
        {
            string Normalize(string input, string fallback = "allgemein")
            {
                if (string.IsNullOrWhiteSpace(input)) return fallback.ToLowerInvariant();
                return input.Trim().ToLowerInvariant()
                            .Replace(" ", "")
                            .Replace("-", "")
                            .Replace("_", "");
            }

            string firm = Normalize(firma, "firma");
            string kat = Normalize(kategorie, "allgemein");

            // 🔹 Wenn abteilungName null → abteilungId prüfen
            string abt = string.IsNullOrWhiteSpace(abteilungName)
                ? (abteilungId.HasValue ? $"abteilung_{abteilungId}" : "allgemein")
                : Normalize(abteilungName, "allgemein");

            var globalKategorien = new[] { "grossdoc" };
            var abteilungKategorien = new[] { "archiv", "versionen", "unterlagensigniert", "gescannte-dokumente" };

            if (globalKategorien.Contains(kat))
            {
                return ($"dokumente/{firm}/{kat}/{fileName}", abteilungId);
            }

            if (abteilungKategorien.Contains(kat))
            {
                return ($"dokumente/{firm}/{abt}/{kat}/{fileName}", abteilungId);
            }

            return ($"dokumente/{firm}/{abt}/{kat}/{fileName}", abteilungId);
        }




        // ✅ Copie/merge des métadonnées
        public static void ApplyMetadataToDocument(
      Dokumente doc,
      Dokumente parent,
      string? metadatenJson,
      string? signedByUser = null)
        {
            // 🧠 Metadatenquelle: das Parent-Metadatenobjekt
            var parentMeta = parent.MetadatenObjekt ?? new Metadaten();

            // 🔹 Neues Metadatenobjekt für das Ziel
            var newMeta = new Metadaten
            {
                Titel = parentMeta.Titel,
                Beschreibung = parentMeta.Beschreibung,
                Kategorie = parentMeta.Kategorie,
                Stichworte = parentMeta.Stichworte,

                Rechnungsnummer = parentMeta.Rechnungsnummer,
                Kundennummer = parentMeta.Kundennummer,
                Rechnungsbetrag = parentMeta.Rechnungsbetrag,
                Nettobetrag = parentMeta.Nettobetrag,
                Gesamtpreis = parentMeta.Gesamtpreis,
                Steuerbetrag = parentMeta.Steuerbetrag,

                Rechnungsdatum = parentMeta.Rechnungsdatum,
                Lieferdatum = parentMeta.Lieferdatum,
                Faelligkeitsdatum = parentMeta.Faelligkeitsdatum,

                Zahlungsbedingungen = parentMeta.Zahlungsbedingungen,
                Lieferart = parentMeta.Lieferart,
                ArtikelAnzahl = parentMeta.ArtikelAnzahl,

                Email = parentMeta.Email,
                Telefon = parentMeta.Telefon,
                Telefax = parentMeta.Telefax,

                IBAN = parentMeta.IBAN,
                BIC = parentMeta.BIC,
                Bankverbindung = parentMeta.Bankverbindung,
                SteuerNr = parentMeta.SteuerNr,
                UIDNummer = parentMeta.UIDNummer,

                Adresse = parentMeta.Adresse,
                AbsenderAdresse = parentMeta.AbsenderAdresse,
                AnsprechPartner = parentMeta.AnsprechPartner,
                Zeitraum = parentMeta.Zeitraum,

                PdfAutor = parentMeta.PdfAutor,
                PdfBetreff = parentMeta.PdfBetreff,
                PdfSchluesselwoerter = parentMeta.PdfSchluesselwoerter,
                Website = parentMeta.Website,
                OCRText = parentMeta.OCRText
            };

            // 🧾 Wenn zusätzliches JSON übergeben wurde, anwenden
            if (!string.IsNullOrWhiteSpace(metadatenJson))
            {
                try
                {
                    var jsonMeta = System.Text.Json.JsonSerializer
                        .Deserialize<Dictionary<string, string>>(metadatenJson);

                    if (jsonMeta != null)
                    {
                        foreach (var kv in jsonMeta)
                        {
                            switch (kv.Key.ToLower())
                            {
                                case "beschreibung": newMeta.Beschreibung = kv.Value; break;
                                case "titel": newMeta.Titel = kv.Value; break;
                                case "rechnungsnummer": newMeta.Rechnungsnummer = kv.Value; break;
                                case "kundennummer": newMeta.Kundennummer = kv.Value; break;
                                case "email": newMeta.Email = kv.Value; break;
                                case "telefon": newMeta.Telefon = kv.Value; break;
                                case "iban": newMeta.IBAN = kv.Value; break;
                                case "bic": newMeta.BIC = kv.Value; break;
                                case "adresse": newMeta.Adresse = kv.Value; break;
                                case "website": newMeta.Website = kv.Value; break;
                                case "ocrtext": newMeta.OCRText = kv.Value; break;
                                    // du kannst hier beliebig erweitern
                            }
                        }
                    }
                }
                catch
                {
                    // Kein Problem – JSON ist optional
                }
            }

            // 🖊️ Signatur-Info hinzufügen (nicht in DB-Spalten, nur informativ)
            if (!string.IsNullOrWhiteSpace(signedByUser))
            {
                var signInfo = $"Signiert von {signedByUser} am {DateTime.Now:dd.MM.yyyy HH:mm}";
                if (!string.IsNullOrWhiteSpace(newMeta.Beschreibung))
                    newMeta.Beschreibung += $"\n\n{signInfo}";
                else
                    newMeta.Beschreibung = signInfo;
            }

            // ✅ Verknüpfung herstellen
            doc.MetadatenObjekt = newMeta;

            // 📦 Optionale JSON-Darstellung für Versionierung/Export
            var metaDict = new Dictionary<string, string?>
            {
                ["Titel"] = newMeta.Titel,
                ["Beschreibung"] = newMeta.Beschreibung,
                ["Rechnungsnummer"] = newMeta.Rechnungsnummer,
                ["Kundennummer"] = newMeta.Kundennummer,
                ["Email"] = newMeta.Email,
                ["Telefon"] = newMeta.Telefon,
                ["IBAN"] = newMeta.IBAN,
                ["BIC"] = newMeta.BIC,
                ["Adresse"] = newMeta.Adresse,
                ["Website"] = newMeta.Website,
                ["OCRText"] = newMeta.OCRText
            };

            //doc.Metadaten = System.Text.Json.JsonSerializer.Serialize(metaDict);
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return path.Trim()
                       .Replace("\\", "/")
                       .ToLowerInvariant();
        }
        public static DmsFile MapToDmsFile(Dokumente d)
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d));

            var meta = d.MetadatenObjekt ?? new Metadaten();

            var file = new DmsFile
            {
                Id = d.Id.ToString(),
                GuidId = d.Id,
                Name = d.Dateiname,
                Path = d.Dateipfad,
                Kategorie = meta.Kategorie ?? d.Kategorie ?? "Allgemein",
                Beschreibung = meta.Beschreibung,
                Titel = meta.Titel,
                Rechnungsnummer = meta.Rechnungsnummer,
                Kundennummer = meta.Kundennummer,
                Rechnungsbetrag = meta.Rechnungsbetrag,
                Nettobetrag = meta.Nettobetrag,
                Gesamtpreis = meta.Gesamtpreis,
                Steuerbetrag = meta.Steuerbetrag,
                Rechnungsdatum = meta.Rechnungsdatum,
                Lieferdatum = meta.Lieferdatum,
                Faelligkeitsdatum = meta.Faelligkeitsdatum,
                Zahlungsbedingungen = meta.Zahlungsbedingungen,
                Lieferart = meta.Lieferart,
                ArtikelAnzahl = meta.ArtikelAnzahl,
                Email = meta.Email,
                Telefon = meta.Telefon,
                Telefax = meta.Telefax,
                IBAN = meta.IBAN,
                BIC = meta.BIC,
                Bankverbindung = meta.Bankverbindung,
                SteuerNr = meta.SteuerNr,
                UIDNummer = meta.UIDNummer,
                Adresse = meta.Adresse,
                AbsenderAdresse = meta.AbsenderAdresse,
                AnsprechPartner = meta.AnsprechPartner,
                Zeitraum = meta.Zeitraum,
                PdfAutor = meta.PdfAutor,
                PdfBetreff = meta.PdfBetreff,
                PdfSchluesselwoerter = meta.PdfSchluesselwoerter,
                Website = meta.Website,
                OCRText = meta.OCRText,

                HochgeladenAm = d.HochgeladenAm,
                SasUrl = d.SasUrl,
                ObjectPath = d.ObjectPath,
                Status = d.dtStatus.ToString(),
                IsIndexed = d.IsIndexed,
                IsVersion = d.IsVersion,
                EstSigne = d.EstSigne ?? false,

                AbteilungId = d.AbteilungId,
                AbteilungName = d.Abteilung?.Name ?? "Unbekannt",
            };

            // 📦 Optional: JSON der Metadaten generieren (z. B. für API oder UI)
            var metaDict = new Dictionary<string, object?>
            {
                ["Titel"] = meta.Titel,
                ["Beschreibung"] = meta.Beschreibung,
                ["Kategorie"] = meta.Kategorie,
                ["Rechnungsnummer"] = meta.Rechnungsnummer,
                ["Kundennummer"] = meta.Kundennummer,
                ["Rechnungsbetrag"] = meta.Rechnungsbetrag,
                ["Nettobetrag"] = meta.Nettobetrag,
                ["Gesamtpreis"] = meta.Gesamtpreis,
                ["Steuerbetrag"] = meta.Steuerbetrag,
                ["Rechnungsdatum"] = meta.Rechnungsdatum,
                ["Faelligkeitsdatum"] = meta.Faelligkeitsdatum,
                ["Email"] = meta.Email,
                ["Telefon"] = meta.Telefon,
                ["IBAN"] = meta.IBAN,
                ["BIC"] = meta.BIC,
                ["Bankverbindung"] = meta.Bankverbindung,
                ["SteuerNr"] = meta.SteuerNr,
                ["UIDNummer"] = meta.UIDNummer,
                ["Adresse"] = meta.Adresse,
                ["AbsenderAdresse"] = meta.AbsenderAdresse,
                ["AnsprechPartner"] = meta.AnsprechPartner,
                ["Zeitraum"] = meta.Zeitraum,
                ["PdfAutor"] = meta.PdfAutor,
                ["PdfBetreff"] = meta.PdfBetreff,
                ["PdfSchluesselwoerter"] = meta.PdfSchluesselwoerter,
                ["Website"] = meta.Website,
                ["OCRText"] = meta.OCRText
            };

            file.MetadataJson = System.Text.Json.JsonSerializer.Serialize(metaDict);

            return file;
        }


    }

}

