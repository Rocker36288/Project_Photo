namespace Project_Photo.Areas.Admin.ViewModels.Permission
{
    public class UserPermissionDeleteViewModel
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
        public bool HasRelatedData => ChildPermissionCount > 0 || AssignedRoleTypeCount > 0;

        public string WarningMessage
        {
            get
            {
                if (!HasRelatedData)
                    return "此權限沒有關聯資料，可以安全刪除。";

                var messages = new List<string>();

                if (ChildPermissionCount > 0)
                    messages.Add($"{ChildPermissionCount} 個子權限");

                if (AssignedRoleTypeCount > 0)
                    messages.Add($"{AssignedRoleTypeCount} 個角色");

                return $"警告：此權限關聯了 {string.Join("、", messages)}，刪除後這些關聯將會受到影響！";
            }
        }
    }
}
