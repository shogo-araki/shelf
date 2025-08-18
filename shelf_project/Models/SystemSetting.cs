using System.ComponentModel.DataAnnotations;

namespace shelf_project.Models
{
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    // システム設定のカテゴリ定義
    public static class SystemSettingCategories
    {
        public const string Shelf = "SHELF";
        public const string Pricing = "PRICING";
        public const string Contract = "CONTRACT";
        public const string System = "SYSTEM";
    }

    // システム設定のキー定義
    public static class SystemSettingKeys
    {
        // 棚関連
        public const string DefaultShelfCount = "DEFAULT_SHELF_COUNT";
        public const string DefaultProductSelectionCount = "DEFAULT_PRODUCT_SELECTION_COUNT";
        
        // 価格関連
        public const string MonthlyFeeIndividual = "MONTHLY_FEE_INDIVIDUAL";
        public const string MonthlyFeeChainStore = "MONTHLY_FEE_CHAIN_STORE";
        public const string MonthlyFeeHeadOffice = "MONTHLY_FEE_HEAD_OFFICE";
        
        // 契約関連
        public const string DefaultContractDurationMonths = "DEFAULT_CONTRACT_DURATION_MONTHS";
        
        // システム関連
        public const string SystemName = "SYSTEM_NAME";
        public const string SystemVersion = "SYSTEM_VERSION";
    }
}