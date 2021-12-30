using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets
{
    class ExportedAssets
    {
        public DateTime ExportedAt { get; set; } = DateTime.Now;

        public Dictionary<string, NamedItemData> NamedItems = new();

        public ItemRatingTables ItemRatings = new();

        public Dictionary<string, DifficultyInfo> DifficultyInfo = new();
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    class NamedItemData
    {
        [DisallowNull]
        public string? Name { get; set; }
        [DisallowNull]
        public string? Type { get; set; }
        [DisallowNull]
        public string? AssetPath { get; set; }
        [DisallowNull]
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? SubType { get; set; }
        public string? Rarity { get; set; }
        public int? Tier { get; set; }
    }

    class HeroItemData : NamedItemData
    {
        [DisallowNull]
        public string? HeroPerk { get; set; }
        [DisallowNull]
        public string? HeroPerkDescription { get; set; }
        [DisallowNull]
        public string? CommanderPerk { get; set; }
        [DisallowNull]
        public string? CommanderPerkDescription { get; set; }
    }

    class ItemRatingTables
    {
        [DisallowNull]
        public ItemRatingTable? Survivor { get; set; }
        [DisallowNull]
        public ItemRatingTable? LeadSurvivor { get; set; }
        [DisallowNull]
        public ItemRatingTable? Default { get; set; }
    }

    class ItemRatingTable
    {
        // key: $"{rarity}_T{tier:00}", e.g. "SR_T05"
        public Dictionary<string, ItemRatingTier> Tiers { get; set; } = new();
    }

    class ItemRatingTier
    {
        public int FirstLevel { get; set; }
        [DisallowNull]
        public float[]? Ratings { get; set; }
    }

    class DifficultyInfo
    {
        public int RequiredRating { get; set; }
        public int MaximumRating { get; set; }
        public int RecommendedRating { get; set; }
        [DisallowNull]
        public string? DisplayName { get; set; }
    }
}
