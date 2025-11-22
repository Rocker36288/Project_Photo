namespace Project_Photo.Areas.Admin.ViewModels.Role
{
    public class RoleTypeDeleteViewModel
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
        public int UserCount { get; set; }
        public bool HasRelatedData => UserCount > 0;
        public string WarningMessage
        {
            get
            {
                if (!HasRelatedData)
                    return "此角色類型沒有關聯資料，可以安全刪除。";

                return $"警告：此角色類型關聯了 {UserCount} 個用戶，刪除後這些用戶將失去此角色！";
            }
        }

        public string RoleLevelDescription
        {
            get
            {
                return RoleLevel switch
                {
                    1 => "超級管理員",
                    2 => "系統管理員",
                    3 => "特殊角色",
                    4 => "一般用戶",
                    _ => "未知"
                };
            }
        }
    }
}
