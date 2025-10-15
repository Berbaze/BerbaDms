namespace DmsProjeckt.Data
{
    public class DokumentSignatur
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DokumentId { get; set; }
        public int PageNumber { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public string ImageBase64 { get; set; } // optional, falls Signatur individuell ist
    }

}
