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

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public int ShelfCount { get; set; } = 1;

        public int ProductSelectionCount { get; set; } = 5;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyFee { get; set; } = 3980m;

        public DateTime ContractStartDate { get; set; }

        public DateTime? ContractEndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<DistributorSubscription>? Subscriptions { get; set; }

        public virtual ICollection<QRCode>? QRCodes { get; set; }

        public virtual ICollection<Sale>? Sales { get; set; }

        public virtual ICollection<DistributorProduct>? DistributorProducts { get; set; }
    }
}