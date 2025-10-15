using System.ComponentModel.DataAnnotations;

namespace DmsProjeckt.Data
{
    public class Tags
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;    

        public ICollection<DokumentTags>? DokumentTags { get; set; }
    }
}
