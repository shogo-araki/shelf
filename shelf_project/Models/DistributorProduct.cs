using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    public class DistributorProduct
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

        public bool IsActive { get; set; } = true;

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public DateTime? RemovedAt { get; set; }
    }
}