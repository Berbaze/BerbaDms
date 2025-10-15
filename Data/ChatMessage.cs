namespace DmsProjeckt.Data
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string? ReceiverId { get; set; }   // Für private Chats
        public int? GroupId { get; set; }         // Für Gruppenchats
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
    }
}
