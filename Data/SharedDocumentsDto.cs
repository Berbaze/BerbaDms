namespace DmsProjeckt.Data
{
    public class SharedDocumentDto
    {
        public string DokumentTitle { get; set; }
        public string SharedByUserName { get; set; }
        public DateTime SharedAt { get; set; }
        public Guid DokumentId { get; set; }
        public string ObjectPath { get; set; }
    }
}
