using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DmsProjeckt.Data
{
    public class Notiz
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? Titel { get; set; } = string.Empty;
        public string? Inhalt { get; set; } = string.Empty;
        public DateTime LetzteBearbeitung { get; set; }
        public ApplicationUser? User { get; set; }
    }
}