namespace DmsProjeckt.Data
{
    public class SearchHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; }    // Wer hat gesucht?
        public string? SearchTerm { get; set; }
        public DateTime SearchedAt { get; set; }

        public Guid? DokumentId { get; set; }
        public Dokumente? Dokument { get; set; }
    }
}
