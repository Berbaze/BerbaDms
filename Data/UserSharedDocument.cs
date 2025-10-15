using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DmsProjeckt.Data
{
    public class UserSharedDocument
    {
        public Guid Id { get; set; }
        public Guid DokumentId { get; set; } // oder Guid, je nach Modell
        public string SharedByUserId { get; set; }
        public string SharedToUserId { get; set; }
        public DateTime SharedAt { get; set; }

        // Navigation Properties (optional, für Includes)
        public ApplicationUser SharedByUser { get; set; }
        public ApplicationUser SharedToUser { get; set; }
        public Dokumente Dokument { get; set; }
    }

}
