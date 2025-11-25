namespace Project_Photo.Areas.Admin.ViewModels.PermissionCategory
{
    public class PermissionCategoryListViewModel
    {
        public int CategoryId { get; set; }

        public string CategoryCode { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public string? CategoryDescription { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int PermissionCount { get; set; }
    }
}
