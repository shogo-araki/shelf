using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf_project.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ManufacturerId { get; set; }

        [ForeignKey("ManufacturerId")]
        public virtual Manufacturer Manufacturer { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal WholesalePrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RetailPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FreeShippingThreshold { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public bool RequiresRefrigeration { get; set; } = false;

        public bool RequiresFreezing { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0m;

        public int StockQuantity { get; set; } = 0;

        public int MinimumOrderQuantity { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<DistributorProduct>? DistributorProducts { get; set; }

        public virtual ICollection<OrderItem>? OrderItems { get; set; }

        public virtual ICollection<Review>? Reviews { get; set; }

        public virtual ICollection<SampleOrder>? SampleOrders { get; set; }
    }
}