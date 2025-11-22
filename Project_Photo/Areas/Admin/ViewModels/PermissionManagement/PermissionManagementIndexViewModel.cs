namespace Project_Photo.Areas.Admin.ViewModels.PermissionManagement
{
    public class PermissionManagementIndexViewModel
    {
        // 統計資訊
        public int TotalSystemModules { get; set; }
        public int ActiveSystemModules { get; set; }

        public int TotalRoleTypes { get; set; }
        public int ActiveRoleTypes { get; set; }

        public int TotalCategories { get; set; }
        public int ActiveCategories { get; set; }

        public int TotalPermissions { get; set; }
        public int ActivePermissions { get; set; }

        // 最近更新的權限 (最近7天)
        public List<RecentPermissionInfo> RecentPermissions { get; set; } = new List<RecentPermissionInfo>();

        // 系統模組權限統計
        public List<SystemPermissionStatInfo> SystemPermissionStats { get; set; } = new List<SystemPermissionStatInfo>();
    }

    public class RecentPermissionInfo
    {
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; }
        public string PermissionName { get; set; }
        public string SystemName { get; set; }
        public string CategoryName { get; set; }
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SystemPermissionStatInfo
    {
        public int SystemId { get; set; }
        public string SystemCode { get; set; }
        public string SystemName { get; set; }
        public int PermissionCount { get; set; }
        public int ActivePermissionCount { get; set; }
    }
}
