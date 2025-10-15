using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DmsProjeckt.Data
{
    public class Kommentare
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid DokumentId { get; set; }

        [MaxLength(450)]
        public string? ApplicationUserId { get; set; }  // ✅ Ce champ sert pour la liaison FK vers AspNetUsers

        [Required]
        public string BenutzerId { get; set; } = string.Empty;  // ✅ Ce champ peut contenir le nom d’utilisateur affiché

        [Required]
        public string Text { get; set; } = string.Empty;

        [Required]
        public DateTime ErstelltAm { get; set; } = DateTime.UtcNow;
    }
}
