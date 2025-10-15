namespace DmsProjeckt.Data
{
    public class AuditLogDto
    {
        public string Aktion { get; set; } = string.Empty;
        public string BenutzerId { get; set; } = string.Empty;
        public string BenutzerName { get; set; } = string.Empty;
        public string BenutzerEmail { get; set; } = string.Empty;
        public DateTime Zeitstempel { get; set; }
    }
}
