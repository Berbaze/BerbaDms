namespace DmsProjeckt.Data
{
    public class UserFavoritDokument
    {
        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        public Guid DokumentId { get; set; }
        public Dokumente Dokument { get; set; }

        public DateTime AngelegtAm { get; set; } = DateTime.Now;
    }
}
