namespace DmsProjeckt.Data
{
    public class AuditLogAdmin
    {
        public int Id { get; set; }
        public string AdminId { get; set; }
        public string Action { get; set; }
        public string TargetUserId { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; }
    }
}
