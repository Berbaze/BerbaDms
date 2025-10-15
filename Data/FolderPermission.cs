namespace DmsProjeckt.Data
{
    public class FolderPermission
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string FolderPath { get; set; }   // z. B. dokumente/SoftIT/HR/Rechnungen
        public string GrantedByAdminId { get; set; }
        public ApplicationUser GrantedByAdmin { get; set; }
        public DateTime GrantedAt { get; set; }
    }

}
