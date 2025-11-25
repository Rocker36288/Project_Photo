namespace Project_Photo.Areas.Admin.ViewModels.SystemModule
{
    public class SystemModuleDeleteViewModel
    {
        public int SystemId { get; set; }

        public string SystemCode { get; set; } = string.Empty;

        public string SystemName { get; set; } = string.Empty;

        public string? SystemDescription { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public int RoleTypeCount { get; set; }

        public int PermissionCount { get; set; }

        public int AffectedUserCount { get; set; }

        public bool HasRelatedData => RoleTypeCount > 0 || PermissionCount > 0;

        public string WarningMessage
        {
            get
            {
                if (!HasRelatedData)
                    return "此系統模組沒有關聯資料,可以安全刪除。";

                var messages = new System.Collections.Generic.List<string>();
                if (RoleTypeCount > 0)
                    messages.Add($"{RoleTypeCount} 個角色類型");
                if (PermissionCount > 0)
                    messages.Add($"{PermissionCount} 個權限");
                if (AffectedUserCount > 0)
                    messages.Add($"影響 {AffectedUserCount} 個用戶");

                return $"警告:此系統模組關聯了 {string.Join("、", messages)},刪除後這些資料將無法正常運作!";
            }
        }
    }
}
