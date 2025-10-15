namespace DmsProjeckt.Data
{
    public class ChatGroupMember
    {
        public int ChatGroupId { get; set; }
        public ChatGroup ChatGroup { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
