namespace Project_Photo.Areas.Admin.ViewModels.SystemModule
{
    public class SystemModuleDetailsViewModel
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

        public List<RoleTypeInfo> RoleTypes { get; set; } = new List<RoleTypeInfo>();

        public List<PermissionInfo> Permissions { get; set; } = new List<PermissionInfo>();
    }

    public class RoleTypeInfo
    {
        public int RoleTypeId { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
        public int RoleLevel { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PermissionInfo
    {
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string? PermissionDescription { get; set; }
        public string? CategoryName { get; set; }
        public int? ParentPermissionId { get; set; }
        public string? ParentPermissionName { get; set; }
        public bool IsActive { get; set; }
    }
}
