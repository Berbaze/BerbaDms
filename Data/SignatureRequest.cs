namespace DmsProjeckt.Data
{
    public class SignatureRequest
    {
        public int Id { get; set; }
        public Guid FileId { get; set; }            // Dokument-ID oder GUID als String
        public string RequestedUserId { get; set; } = "";  // User der unterschreiben soll
        public string RequestedByUserId { get; set; } = ""; // User der die Anfrage erstellt hat
        public DateTime RequestedAt { get; set; }
        public string Status { get; set; } = "Pending";    // Pending, Signed, Rejected
    }

}
