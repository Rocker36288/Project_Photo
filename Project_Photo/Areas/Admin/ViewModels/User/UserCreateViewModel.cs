using System.ComponentModel.DataAnnotations;

namespace Project_Photo.Areas.Admin.ViewModels.User
{
    public class UserCreateViewModel
    {
        // ===== User 資料表欄位 =====
        [Required(ErrorMessage = "帳號為必填欄位")]
        [StringLength(50, ErrorMessage = "帳號長度不可超過 50 個字元")]
        [Display(Name = "帳號")]
        public string Account { get; set; }

        [Required(ErrorMessage = "Email 為必填欄位")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        [StringLength(100, ErrorMessage = "Email 長度不可超過 100 個字元")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(20, ErrorMessage = "手機號碼長度不可超過 20 個字元")]
        [Display(Name = "手機號碼")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "密碼為必填欄位")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "密碼長度需為 6-255 個字元")]
        [DataType(DataType.Password)]
        [Display(Name = "密碼")]
        public string Password { get; set; }

        [Required(ErrorMessage = "確認密碼為必填欄位")]
        [Compare("Password", ErrorMessage = "密碼與確認密碼不符")]
        [DataType(DataType.Password)]
        [Display(Name = "確認密碼")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "帳號類型為必填欄位")]
        [StringLength(20, ErrorMessage = "帳號類型長度不可超過 20 個字元")]
        [Display(Name = "帳號類型")]
        public string AccountType { get; set; } = "Email";

        [Required(ErrorMessage = "帳號狀態為必填欄位")]
        [StringLength(20, ErrorMessage = "帳號狀態長度不可超過 20 個字元")]
        [Display(Name = "帳號狀態")]
        public string AccountStatus { get; set; } = "Active";

        [Required(ErrorMessage = "刪除標記為必填欄位")]
        [Display(Name = "是否刪除")]
        public bool IsDeleted { get; set; } = false;

        [StringLength(50, ErrorMessage = "註冊來源長度不可超過 50 個字元")]
        [Display(Name = "註冊來源")]
        public string? RegistrationSource { get; set; }

        // ===== UserProfile 資料表欄位 =====
        [StringLength(100, ErrorMessage = "顯示名稱長度不可超過 100 個字元")]
        [Display(Name = "顯示名稱")]
        public string? DisplayName { get; set; }

        [StringLength(500, ErrorMessage = "頭像URL長度不可超過 500 個字元")]
        [Display(Name = "頭像URL")]
        public string? Avatar { get; set; }

        [StringLength(500, ErrorMessage = "封面圖URL長度不可超過 500 個字元")]
        [Display(Name = "封面圖URL")]
        public string? CoverImage { get; set; }

        [StringLength(int.MaxValue, ErrorMessage = "個人簡介長度過長")]
        [Display(Name = "個人簡介")]
        public string? Bio { get; set; }

        [StringLength(255, ErrorMessage = "個人網站長度不可超過 255 個字元")]
        [Display(Name = "個人網站")]
        public string? Website { get; set; }

        [StringLength(100, ErrorMessage = "所在地長度不可超過 100 個字元")]
        [Display(Name = "所在地")]
        public string? Location { get; set; }

        // ===== UserPrivateInfo 資料表欄位 =====
        [StringLength(100, ErrorMessage = "真實姓名長度不可超過 100 個字元")]
        [Display(Name = "真實姓名")]
        public string? RealName { get; set; }

        [StringLength(20, ErrorMessage = "性別長度不可超過 20 個字元")]
        [Display(Name = "性別")]
        public string? Gender { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "生日日期")]
        public DateOnly? BirthDate { get; set; }

        [StringLength(200, ErrorMessage = "完整地址長度不可超過 200 個字元")]
        [Display(Name = "完整地址")]
        public string? FullAddress { get; set; }

        [StringLength(100, ErrorMessage = "城市長度不可超過 100 個字元")]
        [Display(Name = "城市")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "國家長度不可超過 100 個字元")]
        [Display(Name = "國家")]
        public string? Country { get; set; }

        [StringLength(20, ErrorMessage = "郵遞區號長度不可超過 20 個字元")]
        [Display(Name = "郵遞區號")]
        public string? PostalCode { get; set; }

        [StringLength(50, ErrorMessage = "身分證字號長度不可超過 50 個字元")]
        [Display(Name = "身分證字號")]
        public string? IdNumber { get; set; }
    }
}