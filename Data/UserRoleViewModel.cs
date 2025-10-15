namespace DmsProjeckt.Data
{
    public class UserRoleViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string CurrentRole { get; set; }
        public string SelectedRole { get; set; }
        public List<string> AvailableRoles { get; set; }
        public string DepartmentName { get; set; }
        public string Email { get; set; }
        public string Vorname { get; set; }
        public string Nachname { get; set; }
    }
}
