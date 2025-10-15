using System;
namespace DmsProjeckt.Data
{
    public class UserNotification
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int NotificationId { get; set; }
        public Notification Notification { get; set; }
        public bool IsRead { get; set; }
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SendAt { get; set; }
    }

}
