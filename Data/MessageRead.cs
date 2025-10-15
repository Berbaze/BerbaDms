namespace DmsProjeckt.Data
{
    public class MessageRead
    {
        public int MessageId { get; set; }
        public ChatMessage Message { get; set; }

        public string UserId { get; set; }
        public DateTime ReadAt { get; set; }
    }
}
