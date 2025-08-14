using System.ComponentModel.DataAnnotations;

namespace shelf_project.ViewModels
{
    public class DistributorLoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "メールアドレス")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "パスワード")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "ログイン状態を保持する")]
        public bool RememberMe { get; set; }
    }
}