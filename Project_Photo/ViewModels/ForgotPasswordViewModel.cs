using System.ComponentModel.DataAnnotations;

namespace Project_Photo.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "請輸入Email")]
        [EmailAddress(ErrorMessage = "Email格式不正確")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string Email { get; set; }

        [Required(ErrorMessage = "請輸入新密碼")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼長度必須至少6個字元")]
        [DataType(DataType.Password)]
        [Display(Name = "新密碼")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "請確認密碼")]
        [DataType(DataType.Password)]
        [Display(Name = "確認密碼")]
        [Compare("NewPassword", ErrorMessage = "密碼與確認密碼不相符")]
        public string ConfirmPassword { get; set; }
    }
}