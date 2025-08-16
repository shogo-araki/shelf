using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? HeadquartersAddress { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        /// <summary>
        /// 会社タイプ（Individual: 個人店, Chain: チェーン店）
        /// </summary>
        public CompanyType CompanyType { get; set; } = CompanyType.Individual;

        /// <summary>
        /// 本社コード（拠点が新規契約時に入力するコード）
        /// </summary>
        [StringLength(20)]
        public string? HeadOfficeCode { get; set; }

        /// <summary>
        /// 代表者ユーザーID（管理者権限を持つユーザー）
        /// </summary>
        [Required]
        public string OwnerUserId { get; set; } = string.Empty;

        [ForeignKey("OwnerUserId")]
        public virtual ApplicationUser OwnerUser { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// この会社に属する全代理店（拠点）
        /// </summary>
        public virtual ICollection<Distributor>? Distributors { get; set; }
    }

    public enum CompanyType
    {
        /// <summary>
        /// 個人店（1拠点のみ）
        /// </summary>
        Individual = 0,

        /// <summary>
        /// チェーン店（複数拠点）
        /// </summary>
        Chain = 1
    }
}