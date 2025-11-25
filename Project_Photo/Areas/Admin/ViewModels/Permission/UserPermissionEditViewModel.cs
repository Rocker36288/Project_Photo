using System.ComponentModel.DataAnnotations;

namespace Project_Photo.Areas.Admin.ViewModels.Permission
{
    public class UserPermissionEditViewModel
    {
        [Required]
        public int PermissionId { get; set; }

        [Required(ErrorMessage = "權限代碼為必填欄位")]
        [StringLength(100, ErrorMessage = "權限代碼長度不可超過 100 個字元")]
        [Display(Name = "權限代碼")]
        public string PermissionCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "權限名稱為必填欄位")]
        [StringLength(100, ErrorMessage = "權限名稱長度不可超過 100 個字元")]
        [Display(Name = "權限名稱")]
        public string PermissionName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "權限說明長度不可超過 200 個字元")]
        [Display(Name = "權限說明")]
        public string? PermissionDescription { get; set; }

        [Display(Name = "權限分類")]
        public int? CategoryId { get; set; }

        [Display(Name = "所屬系統")]
        public int? SystemId { get; set; }

        [Display(Name = "父權限")]
        public int? ParentPermissionId { get; set; }

        [Required]
        [Display(Name = "狀態")]
        public bool IsActive { get; set; }

        // 額外資訊（用於警告）
        public int ChildPermissionCount { get; set; }

        public int AssignedRoleTypeCount { get; set; }
    }
}
