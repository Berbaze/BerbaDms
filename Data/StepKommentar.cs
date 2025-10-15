namespace DmsProjeckt.Data
{
    public class StepKommentar
    {
        public int Id { get; set; }
        public int StepId { get; set; }
        public Step Step { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string? UserName { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
