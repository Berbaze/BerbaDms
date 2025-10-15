using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DmsProjeckt.Data
{
    public class Kunden
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Vorname { get; set; } = string.Empty;

        public string Adresse { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ApplicationUserId { get; set; } = string.Empty;
        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser? ApplicationUser { get; set; }

        public ICollection<KundeBenutzer>? KundeBenutzer { get; set; }
        public ICollection<Dokumente>? Dokumente { get; set; }
        public string FirmenName { get; set; } = string.Empty;
    }
}
