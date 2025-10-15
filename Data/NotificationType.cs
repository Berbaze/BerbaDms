namespace DmsProjeckt.Data
{
    public class NotificationType
    {
        public int Id { get; set; }
        public string Name { get; set; } // z.B. "Aufgabe"
        public string Description { get; set; }
        public ICollection<Notification> Notifications { get; set; }
        public ICollection<UserNotificationSetting> UserNotificationSettings { get; set; }
    }
}
