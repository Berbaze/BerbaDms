namespace DmsProjeckt.Data
{
    public class DashboardItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Icon { get; set; }
        public string? CssClass { get; set; }
        public string? ActionLink { get; set; }
        public string Nail { get; set; } = string.Empty;
        public string? Beschreibung { get; set; }
        public ICollection<UserDashboardItem>? UserDashboardItems { get; set; }
    }
}
