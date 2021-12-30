using Newtonsoft.Json;
using System.Collections.Concurrent;
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

    class SchematicItemData : NamedItemData
    {
        public string? EvoType { get; set; }
    }

    class ItemRatingTables
    {
        [DisallowNull]
        public ItemRatingTable? Survivor { get; set; }
        [DisallowNull]
        public ItemRatingTable? LeadSurvivor { get; set; }
        [DisallowNull]
        public ItemRatingTable? Default { get; set; }

        public void Update(ItemRatingTables other)
        {
            Survivor ??= other.Survivor;
            LeadSurvivor ??= other.LeadSurvivor;
            Default ??= other.Default;
        }
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

    internal class AssetOutput : IAssetOutput
    {
        private ItemRatingTable? defaultItemRatings, survivorItemRatings, leadSurvivorItemRatings;
        private readonly ConcurrentDictionary<string, NamedItemData> namedItems = new();
        private readonly ConcurrentDictionary<string, DifficultyInfo> difficultyInfo = new();

        public void AddDefaultItemRatings(ItemRatingTable itemRatings)
        {
            defaultItemRatings = itemRatings;
        }

        public void AddDifficultyInfo(string name, DifficultyInfo difficultyInfo)
        {
            this.difficultyInfo.TryAdd(name, difficultyInfo);
        }

        public void AddLeadSurvivorItemRatings(ItemRatingTable itemRatings)
        {
            leadSurvivorItemRatings = itemRatings;
        }

        public void AddNamedItem(string name, NamedItemData itemData)
        {
            namedItems.TryAdd(name, itemData);
        }

        public void AddSurvivorItemRatings(ItemRatingTable itemRatings)
        {
            survivorItemRatings = itemRatings;
        }

        public void CopyTo(ExportedAssets export)
        {
            foreach (var (k, v) in namedItems)
            {
                export.NamedItems.TryAdd(k, v);
            }

            foreach (var (k, v) in difficultyInfo)
            {
                export.DifficultyInfo.TryAdd(k, v);
            }

            if (defaultItemRatings != null)
                export.ItemRatings.Default = defaultItemRatings;
            if (survivorItemRatings != null)
                export.ItemRatings.Survivor = survivorItemRatings;
            if (leadSurvivorItemRatings != null)
                export.ItemRatings.LeadSurvivor = leadSurvivorItemRatings;
        }
    }
}
