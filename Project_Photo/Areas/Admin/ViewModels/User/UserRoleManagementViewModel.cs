using System.ComponentModel.DataAnnotations;

namespace Project_Photo.Areas.Admin.ViewModels.User
{
    public class UserRoleManagementViewModel
    {
        public long UserId { get; set; }
        public string Account { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }

        // 當前角色列表
        public List<AssignedRoleInfo> AssignedRoles { get; set; } = new List<AssignedRoleInfo>();

        // 可用角色列表
        public List<AvailableRoleInfo> AvailableRoles { get; set; } = new List<AvailableRoleInfo>();
    }

    public class AssignedRoleInfo
    {
        public long UserRoleId { get; set; }
        public int RoleTypeId { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
        public int RoleLevel { get; set; }
        public string? SystemName { get; set; }
        public bool IsActive { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
    public class AvailableRoleInfo
    {
        public int RoleTypeId { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
        public int RoleLevel { get; set; }
        public string? SystemName { get; set; }
        public bool IsActive { get; set; }
    }
    public class AssignRoleViewModel
    {
        [Required(ErrorMessage = "用戶ID為必填")]
        public long UserId { get; set; }

        [Required(ErrorMessage = "角色類型ID為必填")]
        public int RoleTypeId { get; set; }

        [Display(Name = "過期時間")]
        public DateTime? ExpiredAt { get; set; }

        [Display(Name = "是否啟用")]
        public bool IsActive { get; set; } = true;
    }
}
