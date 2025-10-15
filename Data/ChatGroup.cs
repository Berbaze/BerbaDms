namespace DmsProjeckt.Data
{
    public class ChatGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<ChatGroupMember> ChatGroupMembers { get; set; } = new List<ChatGroupMember>();
        public string AvatarUrl { get; set; } = string.Empty;
    }


}
