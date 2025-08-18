using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    /// <summary>
    /// 製造者（メーカー）モデル - 商品の製造・提供を行う事業者の情報管理
    /// 代理店への商品提供と売上管理を担当
    /// </summary>
    public class Manufacturer
    {
        /// <summary>
        /// 製造者の一意識別子
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 関連するApplicationUserのID
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 関連するApplicationUserオブジェクト
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// 製造者の会社名
        /// </summary>
        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// 会社の住所
        /// </summary>
        [StringLength(500)]
        public string? Address { get; set; }

        /// <summary>
        /// 会社の電話番号
        /// </summary>
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 会社の詳細説明・企業紹介
        /// </summary>
        [StringLength(2000)]
        public string? CompanyDescription { get; set; }

        /// <summary>
        /// 所属業界・事業分野
        /// </summary>
        [StringLength(100)]
        public string? Industry { get; set; }

        /// <summary>
        /// 会社のウェブサイトURL
        /// </summary>
        [StringLength(500)]
        public string? Website { get; set; }

        /// <summary>
        /// 会社設立年
        /// </summary>
        public int EstablishedYear { get; set; }

        /// <summary>
        /// アクティブ状態（true: 有効、false: 無効）
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// レコード作成日時
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// レコード最終更新日時
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// この製造者が提供する商品のコレクション
        /// </summary>
        /// <remarks>
        /// 1つの製造者は複数の商品を提供できる（1対多の関係）
        /// </remarks>
        public virtual ICollection<Product>? Products { get; set; }

        /// <summary>
        /// この製造者に関連する売上精算情報のコレクション
        /// </summary>
        /// <remarks>
        /// 代理店での販売に基づく定期的な精算データを管理
        /// </remarks>
        public virtual ICollection<Settlement>? Settlements { get; set; }
    }
}