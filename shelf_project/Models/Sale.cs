using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    public class Sale
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [Required]
        public int DistributorId { get; set; }

        [ForeignKey("DistributorId")]
        public virtual Distributor Distributor { get; set; } = null!;

        public int? QRCodeId { get; set; }

        [ForeignKey("QRCodeId")]
        public virtual QRCode? QRCode { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DistributorCommission { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformFee { get; set; }

        public DateTime SaleDate { get; set; } = DateTime.Now;

        public bool IsSettled { get; set; } = false;

        public DateTime? SettlementDate { get; set; }
    }
}