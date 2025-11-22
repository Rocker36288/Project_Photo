namespace Project_Photo.Areas.Admin.ViewModels.PermissionCategory
{
    public class PermissionCategoryDetailsViewModel
    {
        public int CategoryId { get; set; }

        public string CategoryCode { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public string? CategoryDescription { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int PermissionCount { get; set; }

        public List<CategoryPermissionInfo> Permissions { get; set; } = new List<CategoryPermissionInfo>();
    }

    public class CategoryPermissionInfo
    {
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string? PermissionDescription { get; set; }
        public string? SystemName { get; set; }
        public int? ParentPermissionId { get; set; }
        public string? ParentPermissionName { get; set; }
        public bool IsActive { get; set; }
    }
}

