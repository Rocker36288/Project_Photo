namespace Project_Photo.Areas.Admin.ViewModels.Role
{
    public class RoleTypeDetailsViewModel
    {
        public int RoleTypeId { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
        public int RoleLevel { get; set; }
        public int? SystemId { get; set; }
        public string? SystemName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int UserCount { get; set; }
        public List<RoleUserInfo> Users { get; set; } = new List<RoleUserInfo>();
    }

    public class RoleUserInfo
    {
        public long UserRoleId { get; set; }
        public long UserId { get; set; }
        public string Account { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}
