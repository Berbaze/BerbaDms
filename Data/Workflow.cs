using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DmsProjeckt.Data
{
    public class Workflow
    {
        [Key]
        public int Id { get; set; }


        [Required]
        [StringLength(100)]
        [Display(Name = "Titel")]
        public string Title { get; set; }

        [Display(Name = "Beschreibung")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [Display(Name = "Erstellt am")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Letzte Änderung")]
        [DataType(DataType.DateTime)]
        public DateTime? LastModified { get; set; }

        public List<Step> Steps { get; set; } = new();

        [ForeignKey("UserId")]
        public ApplicationUser? CreatedByUser { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ICollection<Dokumente> Dokumente { get; set; } = new List<Dokumente>();
    }


}
