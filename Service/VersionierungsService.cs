using DmsProjeckt.Data;
using DmsProjeckt.Helpers;
using Google.Apis.Storage.v1;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DmsProjeckt.Service
{
    public class VersionierungsService
    {
        private readonly ApplicationDbContext _db;
        private readonly FirebaseStorageService _storage;

        public VersionierungsService(ApplicationDbContext db, FirebaseStorageService storage)
        {
            _db = db;
            _storage = storage;
        }

        public async Task SpeichereVersionAsync(
         Guid dokumentId,
         string userId,
         string? customLabel = null,
         object? meta = null)
        {
            // 🔹 Original laden inkl. Abteilung
            var original = await _db.Dokumente
                .Include(d => d.Abteilung)   // 👈 wichtig für Abteilung.Name
                .FirstOrDefaultAsync(d => d.Id == dokumentId);

            if (original == null || string.IsNullOrWhiteSpace(original.ObjectPath))
                throw new InvalidOperationException("❌ Original-Dokument invalide oder ObjectPath leer.");

            // 🔹 Prüfen ob Admin / SuperAdmin
            bool isAdmin = await (
                from ur in _db.UserRoles
                join r in _db.Roles on ur.RoleId equals r.Id
                where ur.UserId == userId && (r.Name == "Admin" || r.Name == "SuperAdmin")
                select ur
            ).AnyAsync();

            if (!isAdmin)
            {
                // ⚡ Nur Besitzer oder gleiche Abteilung darf versionieren
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    throw new UnauthorizedAccessException("❌ Benutzer nicht gefunden.");

                if (original.ApplicationUserId != userId && original.AbteilungId != user.AbteilungId)
                    throw new UnauthorizedAccessException("❌ Sie dürfen dieses Dokument nicht versionieren.");
            }

            // 🔹 Anzahl vorhandener Versionen zählen
            var existingCount = await _db.DokumentVersionen
                .CountAsync(v => v.DokumentId == original.Id);

            // 🔹 Version Label setzen
            var label = string.IsNullOrWhiteSpace(customLabel)
                ? $"v{existingCount + 1}"
                : customLabel.Trim();

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            // 🔹 Version in Abteilung/Versionen ablegen
            // ✅ Versionen sollen direkt unter der Abteilung liegen
            var (destinationPath, abteilungId) = DocumentPathHelper.BuildFinalPath(
                firma: original.ObjectPath?.Split('/')[1] ?? "unbekannt",
                fileName: $"{timestamp}_{original.Dateiname}",
                kategorie: "versionen",   // 👈 immer globaler Ordner "versionen"
                abteilungId: original.AbteilungId,
                abteilungName: original.Abteilung?.Name
            );



            // 🔹 Datei nach Firebase kopieren
            var sourcePath = original.ObjectPath.Replace('\\', '/');
            await _storage.CopyAsync(sourcePath, destinationPath);

            // 🔹 Metadaten aufbauen
            // 🔹 Metadaten aufbauen
            string metadataJson;

            if (meta is Dokumente d)
            {
                var m = d.MetadatenObjekt; // 🔗 Metadatenobjekt
                metadataJson = JsonSerializer.Serialize(new
                {
                    d.Id,
                    d.Dateiname,
                    Kategorie = m?.Kategorie ?? d.Kategorie,
                    Beschreibung = m?.Beschreibung ?? d.Beschreibung,
                    Titel = m?.Titel ?? d.Titel,

                    // 🧾 Rechnungsdaten
                    Rechnungsnummer = m?.Rechnungsnummer,
                    Kundennummer = m?.Kundennummer,
                    Rechnungsbetrag = m?.Rechnungsbetrag,
                    Nettobetrag = m?.Nettobetrag,
                    Gesamtpreis = m?.Gesamtpreis,
                    Steuerbetrag = m?.Steuerbetrag,
                    Rechnungsdatum = m?.Rechnungsdatum,
                    Lieferdatum = m?.Lieferdatum,
                    Faelligkeitsdatum = m?.Faelligkeitsdatum,
                    Zahlungsbedingungen = m?.Zahlungsbedingungen,
                    Lieferart = m?.Lieferart,
                    ArtikelAnzahl = m?.ArtikelAnzahl,

                    // ☎️ Kontakt & Kommunikation
                    Email = m?.Email,
                    Telefon = m?.Telefon,
                    Telefax = m?.Telefax,

                    // 🏦 Finanzdaten
                    IBAN = m?.IBAN,
                    BIC = m?.BIC,
                    Bankverbindung = m?.Bankverbindung,

                    // 💰 Steuer / UID
                    SteuerNr = m?.SteuerNr,
                    UIDNummer = m?.UIDNummer,

                    // 🏠 Adressen
                    Adresse = m?.Adresse,
                    AbsenderAdresse = m?.AbsenderAdresse,
                    AnsprechPartner = m?.AnsprechPartner,

                    // 📅 Zeitraum & PDF
                    Zeitraum = m?.Zeitraum,
                    PdfAutor = m?.PdfAutor,
                    PdfBetreff = m?.PdfBetreff,
                    PdfSchluesselwoerter = m?.PdfSchluesselwoerter,
                    Website = m?.Website,
                    OCRText = m?.OCRText,

                    // 🔗 Systeminfos
                    ObjectPath = d.ObjectPath
                });
            }
            else if (original.MetadatenObjekt != null)
            {
                var m = original.MetadatenObjekt;
                metadataJson = JsonSerializer.Serialize(new
                {
                    original.Id,
                    original.Dateiname,
                    Kategorie = m.Kategorie ?? original.Kategorie,
                    Beschreibung = m.Beschreibung ?? original.Beschreibung,
                    Titel = m.Titel ?? original.Titel,
                    m.Rechnungsnummer,
                    m.Kundennummer,
                    m.Rechnungsbetrag,
                    m.Nettobetrag,
                    m.Gesamtpreis,
                    m.Steuerbetrag,
                    m.Rechnungsdatum,
                    m.Lieferdatum,
                    m.Faelligkeitsdatum,
                    m.Zahlungsbedingungen,
                    m.Lieferart,
                    m.ArtikelAnzahl,
                    m.Email,
                    m.Telefon,
                    m.Telefax,
                    m.IBAN,
                    m.BIC,
                    m.Bankverbindung,
                    m.SteuerNr,
                    m.UIDNummer,
                    m.Adresse,
                    m.AbsenderAdresse,
                    m.AnsprechPartner,
                    m.Zeitraum,
                    m.PdfAutor,
                    m.PdfBetreff,
                    m.PdfSchluesselwoerter,
                    m.Website,
                    m.OCRText,
                    original.ObjectPath
                });
            }
            else
            {
                // 🧩 Fallback: Komplettes Original-Objekt (falls keine Metadaten vorhanden)
                metadataJson = JsonSerializer.Serialize(original, new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    WriteIndented = false
                });
            }


            // 🔹 Version in DB eintragen
            var version = new DokumentVersionen
            {
                DokumentId = original.Id,
                Dateiname = original.Dateiname,
                Dateipfad = $"https://storage.googleapis.com/{_storage.Bucket}/{destinationPath}",
                ObjectPath = destinationPath,
                ApplicationUserId = userId,
                HochgeladenAm = DateTime.UtcNow,
                VersionsLabel = label,
                MetadataJson = metadataJson,
                AbteilungId = abteilungId, // gleiche Abteilung wie Original
                IsVersion = true
            };

            _db.DokumentVersionen.Add(version);
            await _db.SaveChangesAsync();

            Console.WriteLine($"✅ Neue Version gespeichert: {label} für Dokument {original.Id}");
        }

        public async Task<List<DokumentVersionen>> HoleVersionenZumOriginalAsync(Dokumente dokument)
        {
            if (dokument == null || string.IsNullOrWhiteSpace(dokument.Dateiname))
                return new List<DokumentVersionen>();

            return await _db.DokumentVersionen
                .Where(v => v.Dateiname == dokument.Dateiname &&
                            v.DokumentId == dokument.Id &&
                            v.ObjectPath.Contains("/Versionen/"))
                .OrderByDescending(v => v.HochgeladenAm)
                .ToListAsync(); // <--- ajout async
        }



    }
}
