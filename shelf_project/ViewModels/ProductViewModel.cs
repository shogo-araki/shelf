using System.ComponentModel.DataAnnotations;

namespace shelf_project.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "商品名")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "商品説明")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "卸価格")]
        [Range(0, 999999.99)]
        public decimal WholesalePrice { get; set; }

        [Required]
        [Display(Name = "販売価格")]
        [Range(0, 999999.99)]
        public decimal RetailPrice { get; set; }

        [Display(Name = "送料無料条件")]
        [Range(0, 999999.99)]
        public decimal? FreeShippingThreshold { get; set; }

        [Display(Name = "カテゴリ")]
        [StringLength(100)]
        public string? Category { get; set; }

        [Display(Name = "商品画像URL")]
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Display(Name = "冷蔵配送が必要")]
        public bool RequiresRefrigeration { get; set; }

        [Display(Name = "冷凍配送が必要")]
        public bool RequiresFreezing { get; set; }

        [Display(Name = "送料")]
        [Range(0, 9999.99)]
        public decimal ShippingFee { get; set; } = 0m;

        [Required]
        [Display(Name = "在庫数量")]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; } = 0;

        [Display(Name = "最小注文数量")]
        [Range(1, int.MaxValue)]
        public int MinimumOrderQuantity { get; set; } = 1;

        [Display(Name = "販売中")]
        public bool IsActive { get; set; } = true;
    }
}