using System.ComponentModel.DataAnnotations;
using shelf_project.Models;

namespace shelf_project.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "メールアドレス")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "{0}は{2}文字以上{1}文字以下で入力してください。", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "パスワード")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "パスワード確認")]
        [Compare("Password", ErrorMessage = "パスワードと確認用パスワードが一致しません。")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [Display(Name = "名")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "姓")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "ユーザータイプ")]
        public UserRole Role { get; set; }

        [Display(Name = "会社名")]
        public string? CompanyName { get; set; }
    }
}