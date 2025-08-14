using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    public class DistributorSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DistributorId { get; set; }

        [ForeignKey("DistributorId")]
        public virtual Distributor Distributor { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime BillingDate { get; set; }

        public DateTime? PaidDate { get; set; }

        [Required]
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;

        [StringLength(100)]
        public string? PaymentIntentId { get; set; }

        [StringLength(500)]
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }

    public enum SubscriptionStatus
    {
        Pending = 0,
        Paid = 1,
        Failed = 2,
        Cancelled = 3
    }
}