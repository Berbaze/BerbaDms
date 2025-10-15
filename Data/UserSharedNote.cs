namespace DmsProjeckt.Data
{
    public class UserSharedNote
    {
        public Guid Id { get; set; }
        public int NotizId { get; set; } // oder Guid, je nach Modell
        public string SharedByUserId { get; set; }
        public string SharedToUserId { get; set; }
        public DateTime SharedAt { get; set; }

        // Navigation Properties (optional, für Includes)
        public ApplicationUser SharedByUser { get; set; }
        public ApplicationUser SharedToUser { get; set; }
        public Notiz Notiz { get; set; }
    }
}
