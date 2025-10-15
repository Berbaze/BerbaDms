using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DmsProjeckt.Data
{
    public class KundeBenutzer
    {
        public int KundenId { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(KundenId))]
        public Kunden? Kunden { get; set; }

        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser? ApplicationUser { get; set; }
    }
}
