using DmsProjeckt.Data;
using Microsoft.EntityFrameworkCore;
namespace DmsProjeckt.Service
{
    public class LocalIndexService
    {
        private readonly ApplicationDbContext _context;

        public LocalIndexService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SaveIndexAsync(Dokumente dokument)
        {
            // 🧹 Alte Indexversion löschen, falls vorhanden
            var existing = await _context.DokumentIndex
                .FirstOrDefaultAsync(d => d.DokumentId == dokument.Id);

            if (existing != null)
            {
                _context.DokumentIndex.Remove(existing);
                await _context.SaveChangesAsync();
            }

            // 🧩 Hole verknüpfte Metadaten
            var meta = dokument.MetadatenObjekt;

            if (meta == null && dokument.MetadatenId != null)
            {
                meta = await _context.Metadaten.FirstOrDefaultAsync(m => m.Id == dokument.MetadatenId);
            }

            // 📦 Neues Indexobjekt erstellen
            var index = new DokumentIndex
            {
                DokumentId = dokument.Id,

                // 🔹 Allgemeine Felder
                Titel = meta?.Titel ?? dokument.Titel ?? "",
                Beschreibung = meta?.Beschreibung ?? dokument.Beschreibung ?? "",
                Kategorie = meta?.Kategorie ?? dokument.Kategorie ?? "",
                ErkannteKategorie = dokument.ErkannteKategorie,

                // 🧾 Rechnungsdaten
                Rechnungsnummer = meta?.Rechnungsnummer,
                Kundennummer = meta?.Kundennummer,
                Rechnungsbetrag = meta?.Rechnungsbetrag,
                Nettobetrag = meta?.Nettobetrag,
                Gesamtbetrag = meta?.Gesamtpreis,
                Steuerbetrag = meta?.Steuerbetrag,
                Rechnungsdatum = meta?.Rechnungsdatum,
                Lieferdatum = meta?.Lieferdatum,
                Faelligkeitsdatum = meta?.Faelligkeitsdatum,
                Zahlungsbedingungen = meta?.Zahlungsbedingungen,
                lieferart = meta?.Lieferart,
                ArtikelAnzahl = meta?.ArtikelAnzahl,

                // 👤 Kontakte & Kommunikation
                Email = meta?.Email,
                Telefon = meta?.Telefon,
                Telefax = meta?.Telefax,

                // 🏦 Finanzdaten
                IBAN = meta?.IBAN,
                BIC = meta?.BIC,
                Bankverbindung = meta?.Bankverbindung,
                SteuerNr = meta?.SteuerNr,
                UIDNummer = meta?.UIDNummer,

                // 🏠 Adressen
                Adresse = meta?.Adresse,
                AbsenderAdresse = meta?.AbsenderAdresse,
                AnsprechPartner = meta?.AnsprechPartner,

                // 🕒 Zeitraum / PDF / Sonstiges
                Zeitraum = meta?.Zeitraum,
                Website = meta?.Website,
                Autor = meta?.PdfAutor ?? "",
                Betreff = meta?.PdfBetreff ?? "",
                Schluesselwoerter = meta?.PdfSchluesselwoerter ?? "",
                OCRText = meta?.OCRText ?? "",

                // 👥 Kunde & Beziehungen
                Kundenname = dokument.Kunde?.Name,

                // 🏷️ Tags & Benutzerdefinierte Metadaten
                Tags = string.Join(", ", dokument.DokumentTags?.Select(t => t.Tag?.Name) ?? []),
                Metadaten = string.Join(" | ", dokument.BenutzerMetadaten?.Select(m => $"{m.Key}: {m.Value}") ?? [])
            };

            // 💾 In Datenbank speichern
            _context.DokumentIndex.Add(index);
            await _context.SaveChangesAsync();
        }

    }
}
