using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        public int? DistributorId { get; set; }

        [ForeignKey("DistributorId")]
        public virtual Distributor? Distributor { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaymentFee { get; set; } = 0m;

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [StringLength(100)]
        public string? PaymentIntentId { get; set; }

        [StringLength(200)]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(100)]
        public string ShippingName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? ShippingPhone { get; set; }

        [StringLength(100)]
        public string? TrackingNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ShippedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    public enum OrderStatus
    {
        Pending = 0,
        Paid = 1,
        Processing = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5,
        Refunded = 6
    }
}