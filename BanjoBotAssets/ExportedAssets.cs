using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets
{
    class ExportedAssets
    {
        public DateTime ExportedAt { get; set; } = DateTime.Now;

        public Dictionary<string, NamedItemData> NamedItems { get; } = new(StringComparer.OrdinalIgnoreCase);

        public ItemRatingTables ItemRatings { get; } = new();

        public Dictionary<string, DifficultyInfo> DifficultyInfo { get; } = new(StringComparer.OrdinalIgnoreCase);
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
        [DisallowNull]
        public AlterationSlot[]? AlterationSlots { get; set; }
    }

    class AlterationSlot
    {
        public int RequiredLevel { get; set; }
        [DisallowNull]
        public string[][]? Alterations { get; set; }
    }

    class QuestItemData : NamedItemData
    {
        [DisallowNull]
        public QuestObjective[]? Objectives { get; set; }
    }

    class QuestObjective
    {
        [DisallowNull]
        public string? BackendName { get; set; }
        [DisallowNull]
        public string? Description { get; set; }
        [DisallowNull]
        public string? HudShortDescription { get; set; }
        public int Count { get; set; }
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
        public Dictionary<string, ItemRatingTier> Tiers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
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
        private readonly ConcurrentDictionary<string, NamedItemData> namedItems = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, DifficultyInfo> difficultyInfo = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, int>> craftingRecipes = new(StringComparer.OrdinalIgnoreCase);

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

        public void CopyTo(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes)
        {
            foreach (var (k, v) in namedItems)
            {
                exportedAssets.NamedItems.TryAdd(k, v);
            }

            foreach (var (k, v) in difficultyInfo)
            {
                exportedAssets.DifficultyInfo.TryAdd(k, v);
            }

            if (defaultItemRatings != null)
                exportedAssets.ItemRatings.Default = defaultItemRatings;
            if (survivorItemRatings != null)
                exportedAssets.ItemRatings.Survivor = survivorItemRatings;
            if (leadSurvivorItemRatings != null)
                exportedAssets.ItemRatings.LeadSurvivor = leadSurvivorItemRatings;

            foreach (var (templateId, recipe) in craftingRecipes)
            {
                var ingredients = new Queue<KeyValuePair<string, int>>(recipe);

                var exportedRecipe = new ExportedRecipe { ItemName = templateId };

                if (ingredients.TryDequeue(out var pair))
                    (exportedRecipe.Ingredient1, exportedRecipe.Quantity1!) = pair;
                if (ingredients.TryDequeue(out pair))
                    (exportedRecipe.Ingredient2, exportedRecipe.Quantity2!) = pair;
                if (ingredients.TryDequeue(out pair))
                    (exportedRecipe.Ingredient3, exportedRecipe.Quantity3!) = pair;
                if (ingredients.TryDequeue(out pair))
                    (exportedRecipe.Ingredient4, exportedRecipe.Quantity4!) = pair;
                if (ingredients.TryDequeue(out pair))
                    (exportedRecipe.Ingredient5, exportedRecipe.Quantity5!) = pair;

                exportedRecipes.Add(exportedRecipe);
            }
        }

        public void AddCraftingRecipe(string name, IReadOnlyDictionary<string, int> ingredients)
        {
            craftingRecipes.TryAdd(name, ingredients);
        }
    }

    enum RecipeMaterial
    {
        Ore,
        Crystal,
    }

    class ExportedRecipe
    {
        [DisallowNull]
        public string? ItemName { get; set; }
        /// <summary>
        /// "Ranged", "Melee", or "Trap"
        /// </summary>
        /// <remarks>
        /// Localized using Resources.Field_Recipe_*
        /// </remarks>
        [DisallowNull]
        public string? Type { get; set; }
        /// <summary>
        /// "Assault", "Axe", "Ceiling", "Club", "Explosive", "Floor", "Hardware",
        /// "Pistol", "Scythe", "Shotgun", "SMG", "Sniper", "Spear", "Sword", or "Wall"
        /// </summary>
        /// <remarks>
        /// Localized using Resources.Field_Schematic_*
        /// </remarks>
        [DisallowNull]
        public string? Subtype { get; set; }
        public int Tier { get; set; }
        /// <summary>
        /// "Ore", "Crystal", or ""
        /// </summary>
        public string? Material { get; set; }
        /// <summary>
        /// "Common", "Uncommon", "Rare", "Epic", "Legendary", or "Mythic"
        /// </summary>
        /// <remarks>
        /// Localized using ELanguage (?)
        /// </remarks>
        [DisallowNull]
        public string? Rarity { get; set; }
        public string? Ingredient1 { get; set; }
        public object? Quantity1 { get; set; }
        public string? Ingredient2 { get; set; }
        public object? Quantity2 { get; set; }
        public string? Ingredient3 { get; set; }
        public object? Quantity3 { get; set; }
        public string? Ingredient4 { get; set; }
        public object? Quantity4 { get; set; }
        public string? Ingredient5 { get; set; }
        public object? Quantity5 { get; set; }
    }
}
