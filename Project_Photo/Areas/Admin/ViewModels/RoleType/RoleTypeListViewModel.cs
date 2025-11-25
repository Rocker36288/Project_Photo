namespace Project_Photo.Areas.Admin.ViewModels.Role
{
    public class RoleTypeListViewModel
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
    }
}
