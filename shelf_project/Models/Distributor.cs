using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    public class Distributor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// 既存プロパティ（後方互換性のため保持）
        /// </summary>
        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        public string? LocationName { get; set; }

        /// <summary>
        /// 全ての代理店が独立した契約者となるため、常にtrueとして扱う
        /// </summary>
        public bool IsHeadquarters { get; set; } = true;

        /// <summary>
        /// 新設計プロパティ（チェーン店対応）
        /// </summary>
        public int? CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        /// <summary>
        /// 拠点タイプ
        /// </summary>
        public DistributorType DistributorType { get; set; } = DistributorType.Individual;

        /// <summary>
        /// チェーン店の場合の親拠点ID（本社など）
        /// </summary>
        public int? ParentDistributorId { get; set; }

        [ForeignKey("ParentDistributorId")]
        public virtual Distributor? ParentDistributor { get; set; }

        public int ShelfCount { get; set; } = 1;

        public int ProductSelectionCount { get; set; } = 5;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyFee { get; set; } = 3980m;

        public DateTime ContractStartDate { get; set; }

        public DateTime? ContractEndDate { get; set; }

        /// <summary>
        /// 解約申請日（解約手続き開始日）
        /// </summary>
        public DateTime? CancellationRequestDate { get; set; }

        /// <summary>
        /// 棚返却予定日
        /// </summary>
        public DateTime? ShelfReturnDueDate { get; set; }

        /// <summary>
        /// 棚返却完了日
        /// </summary>
        public DateTime? ShelfReturnedDate { get; set; }

        /// <summary>
        /// 棚返却状況
        /// </summary>
        public ShelfReturnStatus ShelfReturnStatus { get; set; } = ShelfReturnStatus.NotRequired;

        /// <summary>
        /// 契約ステータス
        /// </summary>
        public ContractStatus ContractStatus { get; set; } = ContractStatus.Active;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<DistributorSubscription>? Subscriptions { get; set; }

        /// <summary>
        /// 1拠点1QRコード設計 - 単一QRコードとの関係
        /// </summary>
        public virtual QRCode? QRCode { get; set; }

        public virtual ICollection<Sale>? Sales { get; set; }

        public virtual ICollection<DistributorProduct>? DistributorProducts { get; set; }

        /// <summary>
        /// 子拠点（本社の場合の支店など）
        /// </summary>
        public virtual ICollection<Distributor>? ChildDistributors { get; set; }

        /// <summary>
        /// 有効な商品選定数を計算（本社の場合は基本の選定数 × 拠点数）
        /// </summary>
        [NotMapped]
        public int EffectiveProductSelectionCount
        {
            get
            {
                if (Company != null && Company.CompanyType == CompanyType.Chain && DistributorType == DistributorType.HeadOffice)
                {
                    // 本社の場合：基本の選定数 × 拠点数
                    var locationCount = Company.Distributors?.Count(d => d.IsActive) ?? 1;
                    return locationCount * ProductSelectionCount;
                }
                else
                {
                    // 個人店または支店の場合：基本の選定数
                    return ProductSelectionCount;
                }
            }
        }

        /// <summary>
        /// 有効な棚数を計算（本社の場合は拠点数、それ以外は1）
        /// </summary>
        [NotMapped]
        public int EffectiveShelfCount
        {
            get
            {
                if (Company != null && Company.CompanyType == CompanyType.Chain && DistributorType == DistributorType.HeadOffice)
                {
                    // 本社の場合：拠点数
                    return Company.Distributors?.Count(d => d.IsActive) ?? 1;
                }
                else
                {
                    // 個人店または支店の場合：1台
                    return 1;
                }
            }
        }

        /// <summary>
        /// 解約可能かどうかを判定
        /// 契約開始から1年後から解約可能
        /// </summary>
        [NotMapped]
        public bool CanCancelContract
        {
            get
            {
                return ContractStartDate.AddYears(1) <= DateTime.Now && 
                       ContractStatus == ContractStatus.Active;
            }
        }

        /// <summary>
        /// 契約満了日を計算（1年後）
        /// </summary>
        [NotMapped]
        public DateTime ContractMaturityDate
        {
            get
            {
                return ContractStartDate.AddYears(1);
            }
        }
    }

    public enum DistributorType
    {
        /// <summary>
        /// 個人店（単独拠点）
        /// </summary>
        Individual = 0,

        /// <summary>
        /// チェーン店本社
        /// </summary>
        HeadOffice = 1,

        /// <summary>
        /// チェーン店支店・店舗
        /// </summary>
        Store = 2
    }

    /// <summary>
    /// 棚返却状況
    /// </summary>
    public enum ShelfReturnStatus
    {
        /// <summary>
        /// 返却不要（契約中）
        /// </summary>
        NotRequired = 0,

        /// <summary>
        /// 返却予定
        /// </summary>
        Scheduled = 1,

        /// <summary>
        /// 返却期限超過
        /// </summary>
        Overdue = 2,

        /// <summary>
        /// 返却完了
        /// </summary>
        Completed = 3
    }

    /// <summary>
    /// 契約ステータス
    /// </summary>
    public enum ContractStatus
    {
        /// <summary>
        /// 契約中（アクティブ）
        /// </summary>
        Active = 0,

        /// <summary>
        /// 解約申請中
        /// </summary>
        CancellationRequested = 1,

        /// <summary>
        /// 棚返却待ち
        /// </summary>
        PendingShelfReturn = 2,

        /// <summary>
        /// 解約完了
        /// </summary>
        Cancelled = 3,

        /// <summary>
        /// 一時停止
        /// </summary>
        Suspended = 4
    }
}