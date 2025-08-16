using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    public class Manufacturer
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

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(2000)]
        public string? CompanyDescription { get; set; }

        [StringLength(100)]
        public string? Industry { get; set; }

        [StringLength(500)]
        public string? Website { get; set; }

        public int EstablishedYear { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<Product>? Products { get; set; }

        public virtual ICollection<Settlement>? Settlements { get; set; }
    }
}