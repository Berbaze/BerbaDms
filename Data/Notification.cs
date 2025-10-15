namespace DmsProjeckt.Data
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int NotificationTypeId { get; set; }
        public NotificationType NotificationType { get; set; }
        public ICollection<UserNotification> UserNotifications { get; set; }
        public string? ActionLink { get; set; }
    }

}
