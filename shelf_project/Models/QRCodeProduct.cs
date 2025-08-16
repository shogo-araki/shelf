using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    public class QRCodeProduct
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QRCodeId { get; set; }

        [ForeignKey("QRCodeId")]
        public virtual QRCode QRCode { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public DateTime? RemovedAt { get; set; }

        public string? Notes { get; set; }
    }
}