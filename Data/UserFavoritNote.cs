using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DmsProjeckt.Data
{
    public class UserFavoritNote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NotizId { get; set; }

        [ForeignKey(nameof(NotizId))]
        public Notiz Notiz { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }

        public DateTime HinzugefuegtAm { get; set; } = DateTime.UtcNow;
    }
}
