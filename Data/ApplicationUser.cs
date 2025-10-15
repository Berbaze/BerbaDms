using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
namespace DmsProjeckt.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? Vorname { get; set; }
        public string? Nachname { get; set; }
        public DateTime? Geburtsdatum { get; set; }

        // ✅ Navigation correcte (pas de doublon)
        public ICollection<KundeBenutzer>? KundenVerbindungen { get; set; }

        // Autres relations...
        public ICollection<Dokumente>? Dokumente { get; set; }
        public ICollection<DokumentTags>? DokumentTags { get; set; }
        public ICollection<DokumentRechte>? DokumentRechte { get; set; }
        public ICollection<Kunden>? Kunden { get; set; }
        public ICollection<AuditLog>? AuditLogs { get; set; }
        public ICollection<Kommentare>? Kommentare { get; set; }
        public ICollection<DokumentVersionen>? DokumentVersionen { get; set; }
        public ICollection<Tags>? Tags { get; set; }
        public string? CreatedByAdminId { get; set; }
        public string? FirmenName { get; set; }
        public ICollection<ChatGroupMember>? ChatGroupMembers { get; set; }
        public int? AdminId { get; set; }
        public string ProfilbildUrl { get; set; } = string.Empty;
        public int? AbteilungId { get; set; }
        public Abteilung? Abteilung { get; set; }
        [NotMapped]
        public string FullName => $"{Vorname} {Nachname}".Trim();

        public string? SignaturePath { get; set; }
    }
}

