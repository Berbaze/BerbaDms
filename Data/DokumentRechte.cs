using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DmsProjeckt.Data
{
    public class DokumentRechte
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Id")]
        public Guid DokumentId { get; set; }
        [ForeignKey("Id")]
        public string UserId { get; set; } = string.Empty;
        
        public enum Rechte
        {
            Lesen,
            Schreiben,
            Vollzugriff
        }
    }
}
