namespace DmsProjeckt.Data
{
    public class UserNotificationSetting
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int NotificationTypeId { get; set; }
        public NotificationType NotificationType { get; set; }
        public bool Enabled { get; set; } = true;
        public int? AdvanceMinutes { get; set; }
    }

}
