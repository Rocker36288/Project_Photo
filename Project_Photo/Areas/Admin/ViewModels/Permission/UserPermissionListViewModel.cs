namespace Project_Photo.Areas.Admin.ViewModels.Permission
{
    public class UserPermissionListViewModel
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
        public int ChildPermissionCount { get; set; }
        public int AssignedRoleTypeCount { get; set; }
    }
}
