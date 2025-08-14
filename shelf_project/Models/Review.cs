using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ApprovedAt { get; set; }
    }
}