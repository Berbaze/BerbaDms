using System.ComponentModel.DataAnnotations;
namespace DmsProjeckt.Data
{
    public class UserDashboardItem
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public int DashboardItemId { get; set; }
        public DashboardItem? DashboardItem { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Locked { get; set; }
        public bool Favorit {  get; set; }
    }
}