namespace DmsProjeckt.Data
{
    public class UserWidgetDto
    {
        public string id { get; set; } = string.Empty;
        public int x { get; set; }
        public int y { get; set; }
        public int w { get; set; }
        public int h { get; set; }
        public bool locked { get; set; }
        public bool favorit { get; set; }
    }
}
