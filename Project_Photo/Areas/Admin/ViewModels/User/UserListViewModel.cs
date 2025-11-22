namespace Project_Photo.Areas.Admin.ViewModels.User
{
    public class UserListViewModel
    {
        public long UserId { get; set; }
        public string Account { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string? DisplayName { get; set; }
        public string AccountType { get; set; }
        public string AccountStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();

    }
}
