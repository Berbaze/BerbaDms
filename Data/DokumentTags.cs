using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DmsProjeckt.Data
{
    public class DokumentTags
    {
        [Key]
        public Guid DokumentId { get; set; }
        [Key]
        public int TagId { get; set; }
        [ForeignKey("Id")]
        public Dokumente Dokument { get; set; } = null!;
        [ForeignKey("Id")]
        public Tags Tag { get; set; } = null!;
    }
}
