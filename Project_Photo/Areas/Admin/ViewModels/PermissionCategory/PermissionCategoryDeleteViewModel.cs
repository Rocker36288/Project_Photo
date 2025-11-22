namespace Project_Photo.Areas.Admin.ViewModels.PermissionCategory
{
    public class PermissionCategoryDeleteViewModel
    {
        public int CategoryId { get; set; }

        public string CategoryCode { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public string? CategoryDescription { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public int PermissionCount { get; set; }

        public bool HasRelatedData => PermissionCount > 0;

        public string WarningMessage
        {
            get
            {
                if (!HasRelatedData)
                    return "此權限分類沒有關聯資料,可以安全刪除。";

                return $"警告:此權限分類關聯了 {PermissionCount} 個權限,刪除後這些權限將失去分類歸屬!";
            }
        }
    }
}
