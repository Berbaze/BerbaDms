namespace DmsProjeckt.Data
{
    public class DuplicateUpload
    {
        public int Id { get; set; }

        public Guid DokumentId { get; set; }
        public Dokumente Dokument { get; set; }

        // ✅ Ton UserId doit être string (comme IdentityUser.Id)
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string FileName { get; set; }
    }


}
