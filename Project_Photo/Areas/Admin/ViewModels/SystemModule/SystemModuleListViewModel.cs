namespace Project_Photo.Areas.Admin.ViewModels.SystemModule
{
    public class SystemModuleListViewModel
    {
        public int SystemId { get; set; }

        public string SystemCode { get; set; } = string.Empty;

        public string SystemName { get; set; } = string.Empty;

        public string? SystemDescription { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int RoleTypeCount { get; set; }

        public int PermissionCount { get; set; }

        public int ActiveUserCount { get; set; }
    }
}
