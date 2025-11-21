using System.ComponentModel.DataAnnotations;

namespace Project_Photo.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "請輸入帳號或Email")]
        [Display(Name = "帳號或Email")]
        public string AccountOrEmail { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)]
        [Display(Name = "密碼")]
        public string Password { get; set; }

        [Display(Name = "記住我")]
        public bool RememberMe { get; set; }
    }
}
