using DmsProjeckt.Data;

namespace DmsProjeckt.Data
{
    public class RecentHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public Guid DokumentId { get; set; }
        public DateTime OpenedAt { get; set; }

        public ApplicationUser User { get; set; }
        public Dokumente Dokument { get; set; }
    }
}
