using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    public class Settlement
    {
        [Key]
        public int Id { get; set; }

        public int? ManufacturerId { get; set; }

        [ForeignKey("ManufacturerId")]
        public virtual Manufacturer? Manufacturer { get; set; }

        public int? DistributorId { get; set; }

        [ForeignKey("DistributorId")]
        public virtual Distributor? Distributor { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public SettlementType Type { get; set; }

        [Required]
        public DateTime PeriodStart { get; set; }

        [Required]
        public DateTime PeriodEnd { get; set; }

        [Required]
        public SettlementStatus Status { get; set; } = SettlementStatus.Pending;

        public DateTime? ProcessedDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum SettlementType
    {
        ManufacturerSales = 0,
        DistributorCommission = 1
    }

    public enum SettlementStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3
    }
}