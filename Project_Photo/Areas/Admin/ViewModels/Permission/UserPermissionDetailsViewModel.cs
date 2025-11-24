namespace Project_Photo.Areas.Admin.ViewModels.Permission
{
    public class UserPermissionDetailsViewModel
    {
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string? PermissionDescription { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? SystemId { get; set; }
        public string? SystemName { get; set; }
        public int? ParentPermissionId { get; set; }
        public string? ParentPermissionName { get; set; }
        public bool IsActive { get; set; }
        public List<ChildPermissionInfo> ChildPermissions { get; set; } = new List<ChildPermissionInfo>();
        public List<AssignedRoleTypeInfo> AssignedRoleTypes { get; set; } = new List<AssignedRoleTypeInfo>();
        public int ChildPermissionCount { get; set; }
        public int AssignedRoleTypeCount { get; set; }

    }

    public class ChildPermissionInfo
    {
        public int PermissionId { get; set; }
        public string PermissionCode {  set; get; } = string.Empty;
        public string PermissionName { set; get; } = string.Empty;
        public string? PermissionDescription { set; get; }
        public bool IsActive { get; set; }

    }

    public class AssignedRoleTypeInfo
    {
        public int RoleTypeId { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
        public int RoleLevel { get; set; }
        public bool IsActive { get; set; }
        public string? SystemName { get; set; }
    }
}
