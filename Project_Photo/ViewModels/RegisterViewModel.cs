using System.ComponentModel.DataAnnotations;

namespace Project_Photo.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "請輸入帳號")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "帳號長度需介於 3-50 字元")]
        [Display(Name = "帳號")]
        public string Account { get; set; }

        [Required(ErrorMessage = "請輸入Email")]
        [EmailAddress(ErrorMessage = "Email格式不正確")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "手機號碼格式不正確")]
        [Display(Name = "手機號碼")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "密碼長度至少6個字元以上")]
        [DataType(DataType.Password)]
        [Display(Name = "密碼")]
        public string Password { get; set; }

        [Required(ErrorMessage = "請確認密碼")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "密碼與確認密碼不相符")]
        [Display(Name = "確認密碼")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "註冊來源")]
        public string RegistrationSource { get; set; } = "Web";

    }
}
