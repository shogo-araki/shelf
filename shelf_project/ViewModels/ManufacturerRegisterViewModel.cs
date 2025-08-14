using System.ComponentModel.DataAnnotations;

namespace shelf_project.ViewModels
{
    public class ManufacturerRegisterViewModel
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
        [Display(Name = "会社名")]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "住所")]
        public string? Address { get; set; }

        [Display(Name = "電話番号")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "担当者名")]
        public string? ContactPerson { get; set; }
    }
}