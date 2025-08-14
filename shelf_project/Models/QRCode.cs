using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    public class QRCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DistributorId { get; set; }

        [ForeignKey("DistributorId")]
        public virtual Distributor Distributor { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [StringLength(500)]
        public string QRCodeImageUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? DeactivatedAt { get; set; }

        public virtual ICollection<Sale>? Sales { get; set; }
    }
}