using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    public class SampleOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DistributorId { get; set; }

        [ForeignKey("DistributorId")]
        public virtual Distributor Distributor { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceFee { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public SampleOrderType OrderType { get; set; }

        [Required]
        public SampleOrderStatus Status { get; set; } = SampleOrderStatus.Pending;

        [StringLength(100)]
        public string? PaymentIntentId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public DateTime? ShippedDate { get; set; }

        public DateTime? DeliveredDate { get; set; }

        [StringLength(100)]
        public string? TrackingNumber { get; set; }
    }

    public enum SampleOrderType
    {
        Monthly = 0,
        Additional = 1
    }

    public enum SampleOrderStatus
    {
        Pending = 0,
        Paid = 1,
        Processing = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5
    }
}