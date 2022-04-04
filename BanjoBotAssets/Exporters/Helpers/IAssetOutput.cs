using BanjoBotAssets.Artifacts.Models;

namespace BanjoBotAssets.Exporters.Helpers
{
    internal interface IAssetOutput
    {
        void AddNamedItem(string name, NamedItemData itemData);

        /// <summary>
        /// Adds an image for a named item.
        /// </summary>
        /// <param name="name">The item's name, as provided to <see cref="AddNamedItem(string, NamedItemData)"/>.
        /// The named item does not have to be added before calling this method.</param>
        /// <param name="type">The type of image being added.</param>
        /// <param name="assetPath">The path of the image asset.</param>
        void AddImageForNamedItem(string name, ImageType type, string assetPath);

        void AddDifficultyInfo(string name, DifficultyInfo difficultyInfo);

        void AddDefaultItemRatings(ItemRatingTable itemRatings);
        void AddSurvivorItemRatings(ItemRatingTable itemRatings);
        void AddLeadSurvivorItemRatings(ItemRatingTable itemRatings);

        void AddCraftingRecipe(string name, IReadOnlyDictionary<string, int> ingredients);
        void AddDisplayNameCorrection(string schematicTemplateId, string v);

        void AddMainQuestLine(string name, string[][] questPages);
        void AddEventQuestLine(string name, string[][] questPages);

        void AddVenturesLevelReward(string eventTag, int level, int totalRequiredXP, bool isMajorReward, List<QuestReward> convertedRewards);
        void AddVenturesPastLevelReward(string eventTag, int pastLevel, QuestReward convertedReward);
        void AddVenturesPastLevelXPRequirement(string eventTag, int xpAmount);

        void CopyTo(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken);

        void ApplyDisplayNameCorrections(ExportedAssets exportedAssets);
    }
}
